using CommunityToolkit.Mvvm.Messaging;
using ControllerEngineeringStudio.UI.Navigation;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// ドッキング管理
    /// </summary>
    internal class DockingManager : IDockingManager, IAsyncDisposable
    {
        #region Fields
        /// <summary>ViewFactory</summary>
        private readonly IViewFactory _viewFactory;
        /// <summary>ドッキングIdとViewModelタイプの関連付け</summary>
        private readonly Dictionary<DockingId, Type> _mappings;
        /// <summary>ロガー</summary>
        private readonly ILogger<DockingManager> _logger;
        /// <summary>メッセンジャー</summary>
        private readonly IMessenger _messenger;

        /// <summary>ドッキングウィンドウViewModelのCollection</summary>
        private readonly ObservableCollection<DockingViewModelBase> _items = new();

        /// <summary>DockingParameterごとにOpen/Closeの排他をとる</summary>
        private readonly ConcurrentDictionary<DockingParameter, SemaphoreSlim> _semaphoreDic = new();
        #endregion

        /// <inheritdoc/>
        public ReadOnlyObservableCollection<DockingViewModelBase> Items { get; }

        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler? ItemsChanged;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="viewFactory">ViewFactory</param>
        /// <param name="mappings">DockingMappingのCollection</param>
        /// <param name="logger">ロガー</param>
        /// <param name="messenger">メッセンジャー</param>
        public DockingManager(IViewFactory viewFactory, IEnumerable<IDockingMapping> mappings,
            ILogger<DockingManager> logger, IMessenger messenger)
        {
            _viewFactory = viewFactory;
            _mappings = mappings.ToDictionary(x => x.DockingId, x => x.ViewModelType);
            _logger = logger;
            _messenger = messenger;

            Items = new ReadOnlyObservableCollection<DockingViewModelBase>(_items);
            _items.CollectionChanged += (s, e) =>
            {
                ItemsChanged?.Invoke(s, e);
            };
        }

        /// <inheritdoc/>
        public async Task<bool> OpenAsync(DockingParameter parameter)
        {
            _logger.LogInformation("Parameter:{Parameter}", parameter);
            var semaphore = _semaphoreDic.GetOrAdd(parameter, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();

            try
            {
                if (!_mappings.TryGetValue(parameter.DockingId, out var type))
                {
                    // ドッキングIdに対応するViewModelが登録されていない
                    throw new InvalidOperationException($"ViewModelが登録されていない。DockingId={parameter.DockingId}");
                }
                // ドッキングIdとキーから追加済みのドッキングウィンドウを探す
                var vm = Find(parameter);
                if (vm == null)
                {
                    // 存在しないので、追加する
                    vm = _viewFactory.CreateViewModel(type) as DockingViewModelBase;
                    if (vm == null)
                    {
                        throw new InvalidOperationException("ViewModel取得失敗");
                    }

                    try
                    {
                        if (!await vm.InitializeAsync(parameter))
                        {
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "想定外のエラー");
                        return false;
                    }
                    vm.PropertyChanged += OnPropertyChanged;
                    _items.Add(vm);
                }

                // 表示、アクティブ化
                vm.IsActive = true;
                vm.IsHidden = false;

                return true;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task CloseAsync(DockingParameter parameter)
        {
            _logger.LogInformation("Parameter:{Parameter}", parameter);
            var semaphore = _semaphoreDic.GetOrAdd(parameter, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();

            try
            {
                // ドッキングIdとキーから追加済みのドッキングウィンドウを探す
                var vm = Find(parameter);
                if (vm == null)
                {
                    // 存在しないので、無視する
                    return;
                }

                if (vm is DocumentViewModelBase)
                {
                    // ドキュメントの場合、ViewModelごと破棄
                    await RemoveAsync(vm);
                }
                else
                {
                    // 非表示、非アクティブ化
                    vm.IsActive = false;
                    vm.IsHidden = true;
                }
            }
            finally
            {

                semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public bool IsOpened(DockingParameter parameter)
        {
            return _items.Any(vm => vm.DockingId == parameter.DockingId && vm.Key == parameter.Key);
        }

        /// <summary>
        /// ドッキングウィンドウViewModelのプロパティ変更
        /// </summary>
        /// <param name="sender">ドッキングウィンドウのViewModel</param>
        /// <param name="e">プロパティ変更イベント引数</param>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DockingViewModelBase vm)
            {
                // ドッキングウィンドウのViewModelでない
                return;
            }

            if (e.PropertyName == nameof(DockingViewModelBase.IsHidden))
            {
                // 表示/非表示状態が変わったら、メッセージで通知する
                var message = new DockingStateChangedMessage(vm.DockingId, vm.Key, vm.IsHidden);
                _logger.LogDebug("Message:{Message}", message);
                _messenger.Send(message);
            }
        }

        /// <summary>
        /// ViewModelの削除
        /// </summary>
        /// <param name="vm">ViewModel</param>
        private async Task RemoveAsync(DockingViewModelBase vm)
        {
            vm.PropertyChanged -= OnPropertyChanged;
            if (vm is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
            _items.Remove(vm);
        }

        /// <summary>
        /// 指定したドッキング画面のViewModelを返す
        /// </summary>
        /// <param name="parameter">ドッキング画面用パラメータ</param>
        /// <returns>ドッキング画面のViewModel。存在しない場合、null</returns>
        private DockingViewModelBase? Find(DockingParameter parameter)
        {
            return _items.FirstOrDefault(item => item.DockingId == parameter.DockingId && item.Key == parameter.Key);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            var tasks = new List<Task>();
            foreach (var item in _items.ToList())
            {
                item.PropertyChanged -= OnPropertyChanged;
                if (item is IAsyncDisposable disposable)
                {
                    tasks.Add(disposable.DisposeAsync().AsTask());
                }
            }
            await Task.WhenAll(tasks);
            _items.Clear();
        }
    }
}

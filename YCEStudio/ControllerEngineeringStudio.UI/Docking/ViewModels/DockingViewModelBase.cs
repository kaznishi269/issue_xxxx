using CommunityToolkit.Mvvm.ComponentModel;

namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// ドッキングウィンドウViewModel
    /// </summary>
    public abstract partial class DockingViewModelBase : ObservableValidator, IAsyncDisposable
    {
        /// <summary>ヘッダー初期値</summary>
        protected string _defaultHeader = string.Empty;

        #region Properties
        /// <summary>ヘッダー</summary>
        [ObservableProperty]
        private string _header = string.Empty;

        /// <summary>アクティブか？</summary>
        [ObservableProperty]
        private bool _isActive = false;

        /// <summary>非表示か？</summary>
        [ObservableProperty]
        private bool _isHidden = true;

        /// <summary>
        /// ドッキングId
        /// </summary>
        public abstract DockingId DockingId { get; }

        /// <summary>
        /// キー(同じドッキングIdで複数インスタンスになる場合に使用する)
        /// </summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>
        /// DockingParameter
        /// </summary>
        public DockingParameter Parameter => new DockingParameter(DockingId, Key);
        #endregion

        /// <inheritdoc/>
        public virtual ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="parameter">Open用パラメータ</param>
        /// <returns>true:Open成功、false:Open失敗</returns>
        public virtual Task<bool> InitializeAsync(DockingParameter parameter)
        {
            Key = parameter.Key;

            return Task.FromResult(true);
        }
    }
}

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ControllerEngineeringStudio.App.Interfaces;
using ControllerEngineeringStudio.App.Resources;
using ControllerEngineeringStudio.App.ViewModels;
using ControllerEngineeringStudio.SampleDocument.ViewModels;
using ControllerEngineeringStudio.UI.Dialog;
using ControllerEngineeringStudio.UI.Docking;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ControllerEngineeringStudio.App.Services
{
    /// <summary>
    /// メニューサービス
    /// </summary>
    public partial class MenuService : IMenuService
    {
        #region Fields
        /// <summary>ドッキングマネージャー</summary>
        private readonly IDockingManager _dockingManager;

        /// <summary>ダイアログサービス</summary>
        private readonly IDialogService _dialogService;

        /// <summary>ロガー</summary>
        private readonly ILogger<MenuService> _logger;

        /// <summary>メッセンジャー</summary>
        private readonly IMessenger _messenger;

        /// <summary>メニューViewModelのCollection</summary>
        private readonly ObservableCollection<MenuItemViewModel> _items = new();
        #endregion

        /// <inheritdoc/>
        public ReadOnlyObservableCollection<MenuItemViewModel> Items { get; }

        #region Commands
        /// <summary>
        /// SampleDocument新規作成コマンド
        /// </summary>
        /// <param name="vm">メニューアイテムViewModel</param>
        [RelayCommand]
        private async Task NewDocument(MenuItemViewModel vm)
        {
            var result = _dialogService.ShowDialog<NewSampleDocumentDialogViewModel, string>("SampleDocument");
            if (!string.IsNullOrEmpty(result))
            {
                await _dockingManager.OpenAsync(new DockingParameter(DockingId.SampleDocument, result));
            }
        }

        /// <summary>
        /// Viewメニューコマンド
        /// </summary>
        /// <param name="vm">メニューアイテムViewModel</param>
        [RelayCommand]
        private async Task ViewMenu(ViewMenuItemViewModel vm)
        {
            if (vm.IsChecked)
            {
                // チェックONのとき、Open
                await _dockingManager.OpenAsync(vm.Parameter);
            }
            else
            {
                // チェックOFFのとき、Close
                await _dockingManager.CloseAsync(vm.Parameter);
            }
        }
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="dockingManager">ドッキングマネージャー</param>
        /// <param name="dialogService">ダイアログサービス</param>
        /// <param name="logger">ロガー</param>
        /// <param name="messenger">メッセンジャー</param>
        public MenuService(IDockingManager dockingManager, IDialogService dialogService,
            ILogger<MenuService> logger, IMessenger messenger)
        {
            _dockingManager = dockingManager;
            _dialogService = dialogService;
            _logger = logger;
            _messenger = messenger;

            Items = new ReadOnlyObservableCollection<MenuItemViewModel>(_items);
            CreateMenuItems();
        }

        /// <summary>
        /// メニューアイテム生成
        /// </summary>
        private void CreateMenuItems()
        {
            _items.Clear();

            CreateFileMenuItems();
            CreateViewMenuItems();
        }

        /// <summary>
        /// Fileメニュー作成
        /// </summary>
        private void CreateFileMenuItems()
        {
            #region 後で消す
            var fileGroup = new MenuItemViewModel(_messenger) { Header = Strings.FileMenu };
            fileGroup.SubItems.Add(new MenuItemViewModel(_messenger)
            {
                Header = "_New SampleDocument",
                ExecuteCommand = NewDocumentCommand,
            });
            fileGroup.SubItems.Add(new MenuItemViewModel(_messenger)
            {
                IsSeparator = true,
            });
            fileGroup.SubItems.Add(new MenuItemViewModel(_messenger)
            {
                Header = "Dummy",
            });
            _items.Add(fileGroup);
            #endregion
        }

        /// <summary>
        /// Viewメニュー作成
        /// </summary>
        private void CreateViewMenuItems()
        {
            var viewGroup = new MenuItemViewModel(_messenger) { Header = Strings.ViewMenu };
            #region 後で消す
            var sampleGroup = new MenuItemViewModel(_messenger) { Header = "SampleTool" };
            for (int i = 1; i < 5; i++)
            {
                var key = $"SampleTool{i}";
                sampleGroup.SubItems.Add(new ViewMenuItemViewModel(_messenger)
                {
                    Header = key,
                    Parameter = new DockingParameter(DockingId.SampleTool, key),
                    ExecuteCommand = ViewMenuCommand,
                    Description = string.Format(Strings.ViewMenuDescription, key),
                });
            }
            viewGroup.SubItems.Add(sampleGroup);
            #endregion

            var aiChat = new ViewMenuItemViewModel(_messenger)
            {
                Header = "Yaskawa Copilot",
                Parameter = new DockingParameter(DockingId.AiChatTool, "Yaskawa Copilot"),
                ExecuteCommand = ViewMenuCommand,
                Description = string.Format(Strings.ViewMenuDescription, "Yaskawa Copilot"),
            };
            viewGroup.SubItems.Add(aiChat);
            _items.Add(viewGroup);
        }
    }
}

using ControllerEngineeringStudio.UI.Navigation;
using System.Text.Json;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Docking;

namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// RadDockingのカスタムDockingPanesFactory
    /// </summary>
    public sealed class CustomDockingPanesFactory : DockingPanesFactory
    {
        /// <summary>ViewFactory</summary>
        private readonly IViewFactory _viewFactory;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="viewFactory">ViewFactory</param>
        public CustomDockingPanesFactory(IViewFactory viewFactory)
        {
            _viewFactory = viewFactory;
        }

        /// <summary>
        /// ViewModelからViewの生成
        /// </summary>
        /// <param name="item">ViewMolde</param>
        /// <returns>RadPane</returns>
        /// <exception cref="InvalidOperationException">システム不正</exception>
        protected override RadPane CreatePaneForItem(object item)
        {
            if (item is not DockingViewModelBase)
            {
                // ドッキングウィンドウのViewModelでない
                throw new InvalidOperationException($"itemがDockingViewModelBaseでない");
            }
            var vm = (DockingViewModelBase)item;
            var view = _viewFactory.CreateView(item);
            if (vm is DocumentViewModelBase)
            {
                // ドキュメントの場合
                return new RadDocumentPane
                {
                    Content = view,
                    DataContext = item
                };
            }
            else
            {
                // ツールの場合
                return new RadPane
                {
                    Content = view,
                    DataContext = item
                };
            }
        }

        /// <summary>
        /// RadDockingへRadPaneを追加
        /// </summary>
        /// <param name="radDocking">RadDocking</param>
        /// <param name="pane">RadPane</param>
        /// <exception cref="NotSupportedException"></exception>
        protected override void AddPane(RadDocking radDocking, RadPane pane)
        {
            if (pane.DataContext is not DockingViewModelBase vm)
            {
                base.AddPane(radDocking, pane);
                return;
            }

            RadDocking.SetSerializationTag(pane, JsonSerializer.Serialize(vm.Parameter));
            RadPaneGroup? group = null;
            if (pane is RadDocumentPane)
            {
                // ドキュメントの場合
                group = FindRadPaneGroup(radDocking, DockingGroupNames.Document);
            }
            else
            {
                // ツールの場合
                if (vm is not ToolViewModelBase toolViewModel)
                {
                    throw new InvalidOperationException("ToolのViewModelが指定されていない");
                }
                switch (toolViewModel.InitialPosition)
                {
                    case DockState.DockedRight:
                        group = FindRadPaneGroup(radDocking, DockingGroupNames.Right);
                        break;
                    case DockState.DockedBottom:
                        group = FindRadPaneGroup(radDocking, DockingGroupNames.Bottom);
                        break;
                    case DockState.DockedLeft:
                        group = FindRadPaneGroup(radDocking, DockingGroupNames.Left);
                        break;
                    //case DockState.FloatingDockable:
                    //    var fdSplitContainer = radDocking.GeneratedItemsFactory.CreateSplitContainer();
                    //    group = radDocking.GeneratedItemsFactory.CreatePaneGroup();
                    //    fdSplitContainer.Items.Add(group);
                    //    group.Items.Add(pane);
                    //    radDocking.Items.Add(fdSplitContainer);
                    //    pane.MakeFloatingDockable();
                    //    return;
                    //case DockState.FloatingOnly:
                    //    var foSplitContainer = radDocking.GeneratedItemsFactory.CreateSplitContainer();
                    //    group = radDocking.GeneratedItemsFactory.CreatePaneGroup();
                    //    foSplitContainer.Items.Add(group);
                    //    group.Items.Add(pane);
                    //    radDocking.Items.Add(foSplitContainer);
                    //    pane.MakeFloatingOnly();
                    //    return;
                    case DockState.DockedTop:
                        throw new NotSupportedException();
                    default:
                        throw new NotSupportedException();
                }
            }

            if (group == null)
            {
                throw new InvalidOperationException("あり得ない");
            }
            else
            {
                group.Items.Add(pane);
            }
        }

        /// <summary>
        /// RadPaneGroupの検索
        /// </summary>
        /// <param name="radDocking">検索対象のRadDocking</param>
        /// <param name="serializationTag">シリアライズタグ</param>
        /// <returns>RadPaneGroup</returns>
        private RadPaneGroup? FindRadPaneGroup(RadDocking radDocking, string serializationTag)
        {
            return radDocking.SplitItems
                    .Select(x => x.Control)
                    .OfType<RadPaneGroup>()
                    .FirstOrDefault(x => RadDocking.GetSerializationTag(x)?.ToString() == serializationTag);
        }
    }
}
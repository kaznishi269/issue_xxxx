using Telerik.Windows.Controls.Docking;

namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// ツールウィンドウViewModel
    /// </summary>
    public abstract partial class ToolViewModelBase : DockingViewModelBase
    {
        /// <summary>
        /// 初期位置
        /// </summary>
        public virtual DockState InitialPosition => DockState.DockedLeft;
    }
}

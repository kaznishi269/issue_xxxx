using Telerik.Windows.Controls;

namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// レイアウトサービス
    /// </summary>
    public interface ILayoutService
    {
        /// <summary>
        /// レイアウト保存
        /// </summary>
        /// <param name="radDocking">RadDocking</param>
        void SaveLayout(RadDocking radDocking);

        /// <summary>
        /// レイアウト復元
        /// </summary>
        /// <param name="radDocking">RadDocking</param>
        Task LoadLayoutAsync(RadDocking radDocking);

        /// <summary>
        /// RadDockingのレイアウト保存中イベント
        /// </summary>
        /// <param name="sender">RadDocking</param>
        /// <param name="e">イベント引数</param>
        void OnRadDockingElementLayoutSaving(object sender, LayoutSerializationSavingEventArgs e);

        /// <summary>
        /// RadDockingのレイアウト復元中イベント
        /// </summary>
        /// <param name="sender">RadDocking</param>
        /// <param name="e">イベント引数</param>
        void OnRadDockingElementLoading(object sender, LayoutSerializationLoadingEventArgs e);
    }
}

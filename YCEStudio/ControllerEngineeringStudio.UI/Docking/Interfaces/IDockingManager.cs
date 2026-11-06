using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// ドッキング管理インターフェース
    /// </summary>
    public interface IDockingManager
    {
        /// <summary>
        /// ドッキングウィンドウViewModelのCollection
        /// </summary>
        ReadOnlyObservableCollection<DockingViewModelBase> Items { get; }

        /// <summary>
        /// CollectionChangedイベント
        /// </summary>
        event NotifyCollectionChangedEventHandler? ItemsChanged;

        /// <summary>
        /// ドッキング画面表示
        /// </summary>
        /// <param name="parameter">ドッキング画面用パラメータ</param>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> OpenAsync(DockingParameter parameter);

        /// <summary>
        /// ドッキング画面閉じる、非表示
        /// </summary>
        /// <param name="parameter">ドッキング画面用パラメータ</param>
        Task CloseAsync(DockingParameter parameter);

        /// <summary>
        /// ドッキング画面が開かれているか？
        /// </summary>
        /// <param name="parameter">ドッキング画面用パラメータ</param>
        /// <returns>true:開かれている、false:開かれていない</returns>
        bool IsOpened(DockingParameter parameter);
    }
}

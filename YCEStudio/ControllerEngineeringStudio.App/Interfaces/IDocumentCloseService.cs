using ControllerEngineeringStudio.UI.Docking;

namespace ControllerEngineeringStudio.App.Interfaces
{
    /// <summary>
    /// ドキュメントクローズサービス
    /// </summary>
    public interface IDocumentCloseService
    {
        /// <summary>
        /// 指定したドキュメントの保存
        /// </summary>
        /// <param name="viewModels">DocumentViewModel</param>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> SaveDocumentsAsync(IEnumerable<DocumentViewModelBase> viewModels);

        /// <summary>
        /// 指定したドキュメントの保存/クローズ
        /// </summary>
        /// <param name="viewModels">DocumentViewModel</param>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> SaveAndCloseDocumentsAsync(IEnumerable<DocumentViewModelBase> viewModels);
    }
}

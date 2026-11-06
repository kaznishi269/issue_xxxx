using ControllerEngineeringStudio.App.Constants;
using ControllerEngineeringStudio.App.Interfaces;
using ControllerEngineeringStudio.App.Parameters;
using ControllerEngineeringStudio.App.ViewModels;
using ControllerEngineeringStudio.UI.Dialog;
using ControllerEngineeringStudio.UI.Docking;
using Microsoft.Extensions.Logging;

namespace ControllerEngineeringStudio.App.Services
{
    /// <summary>
    /// ドキュメントクローズサービス
    /// </summary>
    internal class DocumentCloseService : IDocumentCloseService
    {
        /// <summary>
        /// クローズモード
        /// </summary>
        enum CloseMode
        {
            SaveOnly,       // 保存のみ
            SaveAndClose    // 保存+閉じる
        }

        #region Fields
        /// <summary>ドッキングマネージャー</summary>
        private readonly IDockingManager _dockingManager;

        /// <summary>ダイアログサービス</summary>
        private readonly IDialogService _dialogService;

        /// <summary>ロガー</summary>
        private readonly ILogger<DocumentCloseService> _logger;
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="dockingManager">ドッキングマネージャー</param>
        /// <param name="dialogService">ダイアログサービス</param>
        /// <param name="logger">ロガー</param>
        public DocumentCloseService(IDockingManager dockingManager, IDialogService dialogService, ILogger<DocumentCloseService> logger)
        {
            _dockingManager = dockingManager;
            _dialogService = dialogService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> SaveDocumentsAsync(IEnumerable<DocumentViewModelBase> viewModels)
        {
            return await SaveAndCloseDocumentsAsync(viewModels, CloseMode.SaveOnly);
        }

        /// <inheritdoc/>
        public async Task<bool> SaveAndCloseDocumentsAsync(IEnumerable<DocumentViewModelBase> viewModels)
        {
            return await SaveAndCloseDocumentsAsync(viewModels, CloseMode.SaveAndClose);
        }

        /// <summary>
        /// ドキュメントの保存/クローズ
        /// </summary>
        /// <param name="viewModels"></param>
        /// <param name="mode">クローズモード</param>
        /// <returns>true:成功、false:失敗/キャンセル</returns>
        private async Task<bool> SaveAndCloseDocumentsAsync(IEnumerable<DocumentViewModelBase> viewModels, CloseMode mode)
        {
            var documents = viewModels.ToList();
            _logger.LogDebug("ViewModels:{ViewModels}",
                string.Join(",", documents.Select(x => $"{x.DockingId}:{x.Key}")));
            if (!documents.Any())
            {
                // 対象なし
                return true;
            }

            try
            {
                var dirtyDocuments = documents.Where(x => x.IsDirty).ToList();
                if (dirtyDocuments.Any())
                {
                    // 編集中ドキュメントがある場合、確認
                    var confirmResult = _dialogService.ShowDialog<ConfirmSaveWindowViewModel, ConfirmSaveResult>(
                        new ConfirmSaveWindowParameter(dirtyDocuments.Select(x => x.Header).ToList()));
                    if (confirmResult == ConfirmSaveResult.Save)
                    {
                        // 保存
                        foreach (var document in dirtyDocuments)
                        {
                            if (!await document.SaveAsync())
                            {
                                // 保存失敗
                                return false;
                            }
                        }
                    }
                    else if (confirmResult == ConfirmSaveResult.Cancel)
                    {
                        // キャンセル
                        return false;
                    }
                }

                if (mode == CloseMode.SaveAndClose)
                {
                    // 閉じる
                    foreach (var item in documents)
                    {
                        await _dockingManager.CloseAsync(item.Parameter);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "想定外のエラー");
                return false;
            }

            return true;
        }
    }
}

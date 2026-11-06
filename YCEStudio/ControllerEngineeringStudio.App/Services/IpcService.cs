using ControllerEngineeringStudio.App.Interfaces;
using ControllerEngineeringStudio.Common.InterProcessCommunication;
using ControllerEngineeringStudio.Common.Win32;
using ControllerEngineeringStudio.UI.Docking;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ControllerEngineeringStudio.App.Services
{
    /// <summary>
    /// プロセス間通信サービス
    /// </summary>
    internal class IpcService : IIpcService
    {
        #region Fields
        /// <summary>ドキュメントクローズサービス</summary>
        private readonly IDocumentCloseService _documentCloseService;

        /// <summary>ドッキングマネージャー</summary>
        private readonly IDockingManager _dockingManager;

        /// <summary>ロガー</summary>
        private readonly ILogger<IpcService> _logger;

        /// <summary>ウィンドウハンドル</summary>
        private HwndSource? _wndSource;
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="documentCloseService">ドキュメントクローズサービス</param>
        /// <param name="dockingManager">ドッキングマネージャー</param>
        /// <param name="logger">ロガー</param>
        public IpcService(IDocumentCloseService documentCloseService, IDockingManager dockingManager, ILogger<IpcService> logger)
        {
            _documentCloseService = documentCloseService;
            _dockingManager = dockingManager;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Attach(Window window)
        {
            _wndSource = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            _wndSource?.AddHook(WndProc);
        }

        /// <inheritdoc/>
        public void Detach()
        {
            _wndSource?.RemoveHook(WndProc);
            _wndSource = null;
        }

        /// <summary>
        /// メッセージ受信
        /// </summary>
        /// <param name="hwnd">受信先ウィンドウ</param>
        /// <param name="msgId">メッセージID</param>
        /// <param name="wParam">メッセージ情報</param>
        /// <param name="lParam">メッセージ情報</param>
        /// <param name="isHandled">処理済みか？</param>
        /// <returns>メッセージ応答</returns>
        private IntPtr WndProc(IntPtr hwnd, int msgId, IntPtr wParam, IntPtr lParam, ref bool isHandled)
        {
            if (msgId == Win32API.WM_COPYDATA)
            {
                // メッセージ受信
                return HandleCopyMessage(lParam, ref isHandled);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// WM_COPYDATAの処理
        /// </summary>
        /// <param name="lParam">引数</param>
        /// <param name="isHandled">ハンドルしたか？</param>
        /// <returns>処理結果</returns>
        private IntPtr HandleCopyMessage(IntPtr lParam, ref bool isHandled)
        {
            try
            {
                if (lParam == IntPtr.Zero)
                {
                    // 引数がおかしい
                    _logger.LogDebug("lParamが不正");
                    return IntPtr.Zero;
                }

                // COPYDATASTRUCTには、メッセージIDとプロセスIDのみを持つ
                // データのやり取りはマッピングファイルで行う
                COPYDATASTRUCT copyData = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                if (copyData.lpData == IntPtr.Zero
                    || copyData.cbData < sizeof(uint))
                {
                    // 引数がおかしい
                    _logger.LogDebug("CopyDataStructが不正 lpData:{lpData}, cbData:{cbData}",
                        copyData.lpData,
                        copyData.cbData);
                    return IntPtr.Zero;
                }

                var processId = (uint)Marshal.ReadInt32(copyData.lpData);
                switch ((int)copyData.dwData)
                {
                    case (int)CopyDataMessageId.IsReady:    // 準備完了
                        isHandled = true;
                        return new IntPtr(1);
                    case (int)CopyDataMessageId.CanClose:   // 閉じても良いか確認
                        isHandled = true;
                        _ = HandleCanCloseAsync(processId);
                        return IntPtr.Zero;
                    case (int)CopyDataMessageId.OpenDocument:   // ドキュメントオープン
                        isHandled = true;
                        _ = HandleOpenDocumentAsync(processId);
                        return IntPtr.Zero;
                    case (int)CopyDataMessageId.IsOpenedDocument:   // ドキュメントオープン中確認
                        isHandled = true;
                        _ = HandleIsOpenedDocumentAsync(processId);
                        return IntPtr.Zero;
                    default:
                        _logger.LogDebug("未対応のメッセージ MessageId:{MessageId}", (int)copyData.dwData);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "予期しない例外");
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// アプリケーションを閉じることが可能か？
        /// </summary>
        /// <param name="processId">プロセスId</param>
        /// <returns>Task</returns>
        private Task HandleCanCloseAsync(uint processId)
        {
            // ドキュメントを保存し、全て閉じる
            var messageId = (int)CopyDataMessageId.CanClose;
            return ExecuteIpcAsync(processId, messageId, async () =>
            {
                var data = CopyDataMessage.ReadMappedFile<CanCloseMessageData>(processId, messageId);
                var result = await _documentCloseService.SaveAndCloseDocumentsAsync(_dockingManager.Items.OfType<DocumentViewModelBase>());
                data.canClose = CopyDataMessage.ConvertToBool(result);
                CopyDataMessage.WriteMappedFile<CanCloseMessageData>(processId, messageId, data);
            });
        }

        /// <summary>
        /// ドキュメントオープン
        /// </summary>
        /// <param name="processId">送信元のプロセスID</param>
        /// <returns>Task</returns>
        private Task HandleOpenDocumentAsync(uint processId)
        {
            var messageId = (int)CopyDataMessageId.OpenDocument;
            return ExecuteIpcAsync(processId, messageId, async () =>
            {
                var data = CopyDataMessage.ReadMappedFile<OpenDocumentMessageData>(processId, messageId);
                var result = await _dockingManager.OpenAsync(new DockingParameter(DockingId.SampleDocument, data.fileName));
                data.success = CopyDataMessage.ConvertToBool(result);
                CopyDataMessage.WriteMappedFile<OpenDocumentMessageData>(processId, messageId, data);
            });
        }

        /// <summary>
        /// ドキュメントがオープンされているか
        /// </summary>
        /// <param name="processId">送信元のプロセスID</param>
        /// <returns>Task</returns>
        private Task HandleIsOpenedDocumentAsync(uint processId)
        {
            var messageId = (int)CopyDataMessageId.IsOpenedDocument;
            return ExecuteIpcAsync(processId, messageId, () =>
            {
                var data = CopyDataMessage.ReadMappedFile<IsOpenedDocumentMessageData>(processId, messageId);
                data.opened = CopyDataMessage.ConvertToBool(_dockingManager.IsOpened(new DockingParameter(DockingId.SampleDocument, data.fileName)));
                CopyDataMessage.WriteMappedFile<IsOpenedDocumentMessageData>(processId, messageId, data);
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// IPCの処理実行
        /// </summary>
        /// <param name="processId">プロセスId</param>
        /// <param name="messageId">メッセージId</param>
        /// <param name="action">処理タスク</param>
        /// <returns>Task</returns>
        private async Task ExecuteIpcAsync(uint processId, int messageId, Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "予想外のエラー:ProcessId:{ProcessId}, MessageId:{MessageId}", processId, messageId);
            }
            finally
            {
                try
                {
                    CopyDataMessage.SetEvent(processId, messageId);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "イベントセット失敗:ProcessId:{ProcessId}, MessageId:{MessageId}", processId, messageId);
                }
            }
        }
    }
}

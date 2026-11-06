using ControllerEngineeringStudio.ApplicationService.Interfaces;
using ControllerEngineeringStudio.ApplicationService.Models;
using ControllerEngineeringStudio.Common.IO;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Yaskawa.ControllerEngineeringService.Client.V1;
using Yaskawa.ControllerEngineeringService.Common;
using Yaskawa.ControllerEngineeringService.Server.V1;

namespace ControllerEngineeringStudio.ApplicationService.Services
{
    /// <summary>
    /// エンジニアリングサーバーサービス
    /// </summary>
    internal class EngineeringServerService : GrpcServiceBase<EngineeringServerService>, IEngineeringServerService
    {
        #region Fields
        /// <summary>ServerService</summary>
        private readonly ServerGrpcService.ServerGrpcServiceClient _serverService;
        /// <summary>ClientService</summary>
        private readonly ClientGrpcService.ClientGrpcServiceClient _clientService;

        /// <summary>HeartbeatTaskキャンセルトークン</summary>
        private CancellationTokenSource _cancellationTokenSource = new();
        /// <summary>HeartbeatTask</summary>
        private Task? _heartbeatTask;
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        /// <param name="context">EngineeringServer情報</param>
        /// <param name="serverService">ServerServiceClient</param>
        /// <param name="clientService">ClientServiceClient</param>
        public EngineeringServerService(
            ILogger<EngineeringServerService> logger,
            EngineeringServerContext context,
            ServerGrpcService.ServerGrpcServiceClient serverService,
            ClientGrpcService.ClientGrpcServiceClient clientService)
                : base(logger, context)
        {
            _serverService = serverService;
            _clientService = clientService;
        }

        /// <inheritdoc/>
        public async Task<bool> StartEngineeringAsync(StartEngineeringParameter parameter)
        {
            if (_context.IsConnected)
            {
                // 接続済み
                return true;
            }

            if (!await StartServerAsync())
            {
                // サーバー起動失敗
                return false;
            }

            var result = await ExecuteGrpcAsync(
                _clientService.AddClientAsync(new AddClientReq
                {
                    LegacyProgrammerProcessId = parameter.LegacyProgrammerProcessId
                }));

            if (!result.Success
                || string.IsNullOrEmpty(result.Result?.SessionHandle))
            {
                // 接続失敗
                return false;
            }

            _context.SessionHandle = result.Result.SessionHandle;
            _context.IsOnline = parameter.IsOnline;
            _context.MotionFactoryId = parameter.MotionFactoryId;
            _context.PortNo = parameter.PortNo;
            _context.IPAddress = parameter.IPAddress;
            _context.LegacyProgrammerProcessId = parameter.LegacyProgrammerProcessId;
            _heartbeatTask = StartHeartbeatAsync(_cancellationTokenSource.Token);

            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> EndEngineeringAsync()
        {
            await StopHeartbeatAsync();

            if (!_context.IsConnected)
            {
                // 接続されていない
                return true;
            }

            var result = await ExecuteGrpcAsync(
                _clientService.RemoveClientAsync(new RemoveClientReq
                {
                    SessionHandle = _context.SessionHandle
                }));
            if (!result.Success
                && result.ErrorCode != (int)ErrorCode.SessionHandleNotFound)
            {
                // セッションハンドルが存在しないため、OKのため、それ以外のエラーは失敗にする
                return false;
            }

            // セッションハンドルクリア
            _context.SessionHandle = string.Empty;

            return true;
        }

        /// <summary>
        /// EngineeringServer起動済みか？
        /// </summary>
        /// <returns>true:起動済み、false:未起動</returns>
        private async Task<bool> IsRunningAsync()
        {
            // バージョン情報を取得して取得出来たら起動済みとする
            try
            {
                await _serverService.GetVersionAsync(new GetVersonReq(), deadline: DateTime.UtcNow.AddMilliseconds(500));
                return true;
            }
            catch (RpcException)
            {
                return false;
            }
        }

        /// <summary>
        /// EngineeringServer起動
        /// </summary>
        /// <returns>true:成功、false:失敗</returns>
        private async Task<bool> StartServerAsync()
        {
            if (await IsRunningAsync())
            {
                // 起動済み
                return true;
            }

            // EngineeringServer起動
            // EngineeringServerはHeartbeatが行わなかったら自己終了するため、プロセス終了はしない
            var startInfo = new ProcessStartInfo
            {
                FileName = AppPath.EngineeringServerExeFile,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = AppContext.BaseDirectory,
            };
            try
            {
                Process.Start(startInfo);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "EngineeringServer起動失敗");
                return false;
            }

            // 起動待ち
            var timeout = TimeSpan.FromSeconds(30);
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (await IsRunningAsync())
                {
                    return true;
                }

                // 接続できないので、少し待つ
                await Task.Delay(300);
            }

            return false;
        }

        /// <summary>
        /// EngineeringServerのHeartbeat実行
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>Task</returns>
        private async Task StartHeartbeatAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    var result = await ExecuteGrpcAsync(
                        _clientService.HeartbeatAsync(new HeartbeatReq
                        {
                            SessionHandle = _context.SessionHandle
                        }));
                    if (!result.Success)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("HeartbeatCancel");
            }
        }

        /// <summary>
        /// EngineeringServerのHeartbeat停止
        /// </summary>
        /// <returns>Task</returns>
        private async Task StopHeartbeatAsync()
        {
            try
            {
                // Heartbeatタスクをキャンセルして終了を待つ
                _cancellationTokenSource.Cancel();
                if (_heartbeatTask != null)
                {
                    await _heartbeatTask;
                }
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = new();
                _heartbeatTask = null;
            }
        }
    }
}

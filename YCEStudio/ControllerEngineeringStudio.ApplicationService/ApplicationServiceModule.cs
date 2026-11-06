using ControllerEngineeringStudio.ApplicationService.Interfaces;
using ControllerEngineeringStudio.ApplicationService.Models;
using ControllerEngineeringStudio.ApplicationService.Services;
using ControllerEngineeringStudio.Common.Module;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Pipes;
using Yaskawa.ControllerEngineeringService.AiChat.V1;
using Yaskawa.ControllerEngineeringService.Client.V1;
using Yaskawa.ControllerEngineeringService.NativeEnginnering.V1;
using Yaskawa.ControllerEngineeringService.Server.V1;
using Yaskawa.ControllerEngineeringService.STProgramming.V1;
using Yaskawa.ControllerEngineeringService.Variable.V1;

namespace ControllerEngineeringStudio.ApplicationService
{
    /// <summary>
    /// gRPCのラッパーモジュール
    /// </summary>
    public class ApplicationServiceModule : IModule
    {
        /// <inheritdoc/>
        public static void RegisterServices(IServiceCollection services)
        {
            // gRPC通信で使用するHTTPハンドラーを生成する
            var socketsHttpHandler = new SocketsHttpHandler();
            // 通常のTCPソケット接続の代わりに、
            // Windows Named Pipeへ接続するためのコールバックを設定
            socketsHttpHandler.ConnectCallback =
                async (context, token) =>
                {
                    // エンジニアリングサーバーのNamed Pipeへ接続する
                    // '.'はローカルコンピュータの意味
                    var stream = new NamedPipeClientStream(
                    ".",
                    // TODO：2026/09/09 (YE)Yuta Hatanaka MPEとYRMStudioで切り替える
                    "Yaskawa.MPE720.ControllerEngineeringService",
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                    // サーバーがPipeを生成するまで待機する
                    await stream.ConnectAsync(token);
                    // gRPCの通信ストリームとして返却する
                    return stream;
                };
            var grpcChannel = GrpcChannel.ForAddress("http://localhost",
                new GrpcChannelOptions
                {
                    HttpHandler = socketsHttpHandler
                });
            services.AddSingleton(new ServerGrpcService.ServerGrpcServiceClient(grpcChannel));
            services.AddSingleton(new ClientGrpcService.ClientGrpcServiceClient(grpcChannel));
            services.AddSingleton(new STProgrammingGrpcService.STProgrammingGrpcServiceClient(grpcChannel));
            services.AddSingleton(new NativeEngineeringGrpcService.NativeEngineeringGrpcServiceClient(grpcChannel));
            services.AddSingleton(new AiChatGrpcService.AiChatGrpcServiceClient(grpcChannel));
            services.AddSingleton(new VariableGrpcService.VariableGrpcServiceClient(grpcChannel));
            services.AddSingleton<EngineeringServerContext>();
            services.AddSingleton<IEngineeringServerService, EngineeringServerService>();
            // 図面ごとにサービス(EnginieeringHandle)を分ける仕様の為、Transient
            services.AddTransient<IStProgramService, StProgramService>();
            services.AddTransient<IRegisterAccessService, RegisterAccessService>();
            services.AddSingleton<IAiChatService, AiChatService>();
            services.AddSingleton<IVariableService, VariableService>();
        }
    }
}

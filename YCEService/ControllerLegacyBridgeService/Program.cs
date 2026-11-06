using Microsoft.AspNetCore.Server.Kestrel.Core;
using Yaskawa.ControllerLegacyBridgeService.Infrastructure;
using Yaskawa.ControllerLegacyBridgeService.Server.V1.GrpcService;
using Yaskawa.ControllerLegacyBridgeService.CimScopeWrapper.V1.GrpcService;
using Yaskawa.ControllerLegacyBridgeService.CmifWrapper.V1.GrpcService;

var builder = WebApplication.CreateBuilder(args);

//appsettings.jsonファイルの名称をexe名.jsonに変更する
var exeName =
	Path.GetFileNameWithoutExtension(Environment.ProcessPath)
	?? throw new InvalidOperationException("ProcessPath is null.");
builder.Configuration.AddJsonFile($"{exeName}.json");

// Add services to the container.
builder.Services.AddGrpc();

//シングルトンのサービスを登録する
builder.Services.AddSingleton<ComponentManager>();
builder.Services.AddSingleton<SelfTerminationService>();	//バッググラウンドサービスだが、シングルトンに登録しておかないとDI注入できない

//バックグラウンドサービスを登録する
builder.Services.AddHostedService(provider =>
	provider.GetRequiredService<SelfTerminationService>());

//サービスメソッドコールをインターセプトして、ロギングする
builder.Services.AddGrpc(options =>
{
	options.Interceptors.Add<LoggingInterceptor>();
});

//クライアントとの接続方式をTCPではなく、Named Pipe方式に変更する。
//ツール名は、jsonファイルより取得する
var toolName = builder.Configuration
	.GetSection("AppSettings")
	.GetValue<string>("ToolName")
	?? throw new InvalidOperationException("Tool name is null."); 
builder.WebHost.ConfigureKestrel(options =>
{
	//パイプファイル名は公開しないため、ここで直接指定する
	options.ListenNamedPipe(
		$"Yaskawa.{toolName}.ControllerLegacyBridgeService",
		pipeOptions =>
		{
			pipeOptions.Protocols = HttpProtocols.Http2;
		});
});

var app = builder.Build();

//サービスの登録
app.MapGrpcService<LBServerGrpcServiceImpl>();			//サーバーサービス
app.MapGrpcService<CimScopeWrapperGrpcServiceImpl>();	//CimScopeラッパーサービス
app.MapGrpcService<CmifWrapperGrpcServiceImpl>();		//CMIFラッパーサービス
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

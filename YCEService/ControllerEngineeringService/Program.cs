using Microsoft.AspNetCore.Server.Kestrel.Core;
using Yaskawa.ControllerEngineeringService.Application;
using Yaskawa.ControllerEngineeringService.Client.V1.GrpcService;
using Yaskawa.ControllerEngineeringService.Server.V1.GrpcService;
using Yaskawa.ControllerEngineeringService.Infrastructure;
using Yaskawa.ControllerEngineeringService.NativeEnginnering.V1.GrpcService;
using Yaskawa.ControllerEngineeringService.STProgramming.V1.GrpcService;
using Yaskawa.ControllerEngineeringService.Variable.V1.GrpcService;
using Yaskawa.ControllerEngineeringService.AiChat.V1.GrpcService;

var builder = WebApplication.CreateBuilder(args);

//Debugger.Launch()を入れると、ここでデバッガの選択画面が表示されてデバッグできる
//Debugger.Launch();

//appsettings.jsonファイルの名称をexe名.jsonに変更する
var exeName =
	Path.GetFileNameWithoutExtension(Environment.ProcessPath)
	?? throw new InvalidOperationException("ProcessPath is null.");
builder.Configuration.AddJsonFile($"{exeName}.json");

//起動引数をServerOptionsにバインドする。デフォルト値はjsonファイルより取得する
var options = builder.Configuration.GetSection("ServerOptions")
	.Get<ServerOpsions>() ?? new ServerOpsions();

//シングルトンのサービスを登録する
builder.Services.AddSingleton<SessionHandleManager>();
builder.Services.AddSingleton<ILegacyBridgeServiceManager, LegacyBridgeServiceManager>();
builder.Services.AddSingleton(options);
builder.Services.AddSingleton<LegacyBridgeServiceHeartBeatService>();
builder.Services.AddSingleton<ComponentContextManager>();
builder.Services.AddSingleton<YCEServicePipeClient>();

//バックグラウンドサービスを登録する
builder.Services.AddHostedService<SessionHealthCheckService>();
builder.Services.AddHostedService<LegacyBridgeServiceHostedService>();
builder.Services.AddHostedService<LegacyBridgeServiceHeartBeatService>();

//アプリケーションレイヤのサービスを登録する
builder.Services.AddScoped<IProgramService, ProgramService>();
builder.Services.AddScoped<ICmifService, CmifService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IVariableService, VariableService>();

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
		$"Yaskawa.{toolName}.ControllerEngineeringService",
		pipeOptions =>
		{
			pipeOptions.Protocols = HttpProtocols.Http2;
		});
});

var app = builder.Build();

//アプリケーションが終了する時にレガシーブリッジサービスを停止する
app.Lifetime.ApplicationStopped.Register(() =>
{
	var legacyBridgeServiceManager = app.Services.GetRequiredService<ILegacyBridgeServiceManager>();
	legacyBridgeServiceManager.StopAsync().GetAwaiter().GetResult();

	var client = app.Services.GetRequiredService<YCEServicePipeClient>();
	client.Disconnect();
});

//サービスの登録
app.MapGrpcService<ClientGrpcServiceImpl>();			//クライアントサービス
app.MapGrpcService<ServerGrpcServiceImpl>();			//サーバーサービス
app.MapGrpcService<NativeEngineeringGrpcServiceImpl>();	//ネイティブエンジニアリングサービス
app.MapGrpcService<STProgrammingGrpcServiceImpl>();		//STプログラミングサービス
app.MapGrpcService<VariableGrpcServiceImpl>();			//変数サービス
app.MapGrpcService<AiChatGrpcServiceImpl>();			//AIチャットサービス
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

using Grpc.Core;
using Microsoft.AspNetCore.Http.Headers;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Reflection.PortableExecutable;
using Yaskawa.ControllerEngineeringService.Common;

namespace Yaskawa.ControllerEngineeringService.Infrastructure
{
	/// <summary>
	/// 既存プログラマに要求するリクエスト種別
	/// C++側のCommandと値を必ず合わせる必要がある
	/// </summary>
	internal enum YcePipeCommand : uint
	{
		FindSymbolRegister = 1,     //シンボル/レジスタ検索
	}

	/// <summary>
	/// 既存アプリから返される処理結果
	/// </summary>
	internal enum YcePipeResult : uint
	{
		Success = 0,            //成功
		InvalidCommand = 1,     //リクエスト種別異常
		InvalidData = 2,        //データ異常
		InternalError = 3       //システムエラー
	}

	/// <summary>
	/// Named Pipeを使用した既存C++アプリとの通信クラス
	/// </summary>
	public sealed class YCEServicePipeClient : IDisposable
	{
		/// <summary>
		/// パイプ名称
		/// </summary>
		private const string PipeName = "YCEServicePipe";

		/// <summary>
		/// プロセスIDとパイプストリームを紐付けるディクショナリ
		/// </summary>
		private readonly ConcurrentDictionary<int, NamedPipeClientStream> _dicPipeStream = new();

		private uint requestId = 1;
		private const  uint requestHeaderVersion = 1;

		/// <summary>
		/// 既存プログラマとの接続
		/// </summary>
		/// <param name="legacyProcessId">既存プログラマのプロセスID</param>
		/// <param name="cancellationToken">キャンセル情報</param>
		/// <returns>なし</returns>
		public async Task ConnectAsync(int legacyProcessId, CancellationToken cancellationToken = default)
		{
			//NamedPipeClientStream pipe;
			//該当するパイプストリームが既に作成されているか調べる
			if (_dicPipeStream.TryGetValue(legacyProcessId, out var pipe))
			{
				//既に作成されているので接続済みか調べる
				if (pipe?.IsConnected == true)
				{
					//接続済みなので何もしない
					return;
				}
				pipe?.Dispose();
			}
			else
			{
				//まだ作成されていないので作成する
				string newPipeName = $"{PipeName}_{legacyProcessId}";

				pipe = new System.IO.Pipes.NamedPipeClientStream(
					".",
					newPipeName,
					PipeDirection.InOut,
					PipeOptions.Asynchronous);

				//ディクショナリに登録する
				_dicPipeStream.TryAdd(legacyProcessId, pipe);

			}

			await pipe!.ConnectAsync(cancellationToken);
		}

		public async Task WriteRequestHeaderAsync(
			Int32 legacyProcessId,
			uint command,
			uint requestDataLength
			)
		{
			//----------------------------------------------------------
			// RequestHeader作成(Version1)
			//
			// struct RequestHeader
			// {
			//		DWORD nVersion;
			//		DWORD nRequestId;
			//		DWORD nCommand;
			//		DWORD nDataLength;
			// };
			//----------------------------------------------------------
			//リクエストヘッダ
			byte[] requestHeader = new byte[16];

			//バージョン番号
			BinaryPrimitives.WriteUInt32LittleEndian(
				requestHeader.AsSpan(0, 4), (uint)requestHeaderVersion);

			//リクエストID
			BinaryPrimitives.WriteUInt32LittleEndian(
				requestHeader.AsSpan(4, 4), requestId++);

			//リクエスト種別
			BinaryPrimitives.WriteUInt32LittleEndian(
			requestHeader.AsSpan(8, 4), command);

			//リクエストデータ長
			BinaryPrimitives.WriteUInt32LittleEndian(
			requestHeader.AsSpan(12, 4), requestDataLength);

			// RequestHeader送信
			await WritePipeAsync(legacyProcessId, requestHeader);
		}

		public async Task<(uint result, uint dataLength)> ReadResponceHeaderAsync(
			Int32 legacyProcessId
			)
		{
			//----------------------------------------------------------
			// ResponseHeader受信
			//
			// struct ResponseHeader
			// {
			//     DWORD nVersion;
			//     DWORD nRequestId;
			//     DWORD nResult;
			//     DWORD nDataLength;
			// };
			//----------------------------------------------------------

			byte[] responseHeader = new byte[16];

			//レスポンスデータ受信
			await ReadExactAsync(legacyProcessId, responseHeader);

			//バージョン番号
			uint versionNo = BinaryPrimitives.ReadUInt32LittleEndian(responseHeader.AsSpan(0, 4));
			if (versionNo != 1)
			{
				//このヘッダバージョンは知らない
				//セッションハンドルが指定されていない
				var trailers = new Metadata
				{
					{"error-code", ((int)(ErrorCode.UnsupportedResponseVersion)).ToString()}
				};
				throw new RpcException(
					new Status(
						StatusCode.Unknown,
						""),
					trailers);
			}

			//リクエストID
			uint requestId = BinaryPrimitives.ReadUInt32LittleEndian(responseHeader.AsSpan(4, 4));
			//処理結果
			uint result = BinaryPrimitives.ReadUInt32LittleEndian(responseHeader.AsSpan(8, 4));
			//データサイズ
			uint dataLength = BinaryPrimitives.ReadUInt32LittleEndian(responseHeader.AsSpan(12, 4));

			return (result, dataLength);
		}

		/// <summary>
		/// NamedPipe通信からの読み込み
		/// </summary>
		/// <param name="legacyProcessId">既存プログラマのプロセスID</param>
		/// <param name="buffer">読み込みエリア</param>
		/// <param name="cancellationToken">キャンセル情報</param>
		/// <returns>なし</returns>
		public async Task ReadExactAsync(
			int legacyProcessId,
			byte[] buffer,
			CancellationToken cancellationToken = default)
		{
			int totalRead = 0;

			//プロセスIDからパイプストリームを取得する
			if (_dicPipeStream.TryGetValue(legacyProcessId, out var pipe) == false)
			{
				//登録されているパイプが存在しない
				return;
			}

			if (pipe == null)
			{
				return;
			}

			//指定サイズになるまで読み込む
			while (totalRead < buffer.Length)
			{
				int read = await pipe.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);
				if (read == 0)
				{
					// 0byteは相手側がPipeを切断した
					return;
				}

				totalRead += read;
			}
		}

		/// <summary>
		/// NamedPipe通信への書き込み
		/// </summary>
		/// <param name="legacyProcessId">既存プログラマのプロセスID</param>
		/// <param name="requestData">リクエストデータ</param>
		/// <param name="bFlash">書き込み後のフラッシュ指定
		///		true=フラッシュする　false=フラッシュしない
		/// </param>
		/// <param name="cancellationToken">キャンセル情報</param>
		/// <returns>なし</returns>
		public async Task WritePipeAsync(
			int legacyProcessId,
			byte[] requestData,
			bool bFlash = false,
			CancellationToken cancellationToken = default
			)
		{
			//プロセスIDからパイプストリームを取得する
			if (_dicPipeStream.TryGetValue(legacyProcessId, out var pipe) == false)
			{
				//登録されているパイプが存在しない
				return;
			}
			if (pipe == null)
			{
				return;
			}

			//データを書き込む
			await pipe.WriteAsync(requestData, cancellationToken);

			//フラッシュが必要であれば、フラッシュする
			if (bFlash)
			{
				await pipe.FlushAsync(cancellationToken);
			}
		}

		/// <summary>
		/// Named Pipeの接続状態を確認する
		/// </summary>
		private void EnsureConnected(int legacyProcessId)
		{
			//プロセスIDからパイプストリームを取得する
			if (_dicPipeStream.TryGetValue(legacyProcessId, out var pipe) == false)
			{
				//登録されているパイプが存在しない
				return;
			}
			if (pipe == null || !pipe.IsConnected)
			{
				//既存プログラマに接続されていない
				throw new InvalidOperationException("");
			}
		}

		/// <summary>
		/// ディスポーズ
		/// </summary>
		public void Dispose()
		{
		}

		/// <summary>
		/// 切断処理
		/// </summary>
		/// <param name="legacyProcessId">既存プログラマのプロセスID</param>
		public void Disconnect(int legacyProcessId = 0)
		{
			//NamePipeを閉じる
			Action<int> removeStream =
				pid =>
				{
					//プロセスIDからパイプストリームを取得する
					if (_dicPipeStream.TryGetValue(pid, out var pipe) == false)
					{
						//登録されているパイプが存在しない
						return;
					}
					pipe?.Dispose();
					pipe = null;

					//ディクショナリから消去する
					_dicPipeStream.TryRemove(pid, out _);
				};

			if (legacyProcessId == 0)
			{
				//プロセスIDが指定されていないので、登録されているものをすべて閉じる
				foreach (var pipe in _dicPipeStream.Values)
				{
					pipe?.Dispose();
				}
				_dicPipeStream.Clear();
			}
			else
			{
				//NamedPipeを閉じる
				removeStream(legacyProcessId);
			}
		}
	}
}

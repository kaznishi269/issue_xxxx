using Grpc.Core;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Yaskawa.ControllerEngineeringCommon.Utility;
using Yaskawa.ControllerLegacyBridgeService.Infrastructure;
using static Yaskawa.ControllerEngineeringCommon.Utility.Regsub;

namespace Yaskawa.ControllerLegacyBridgeService.CmifWrapper.V1.GrpcService
{
	/// <summary>
	/// Cmifコンポーネントタイプ
	/// </summary>
	public class ComponentContextCmif : ComponentContextBase
	{
		/// <summary>
		/// 通信ハンドル
		/// </summary>
		public IntPtr? CmHandle {  get; set; }
		/// <summary>
		/// 接続情報-論理ポート番号
		/// </summary>
		public int PortNo {  get; set; }
		/// <summary>
		/// 接続情報-IPアドレス
		/// </summary>
		public string IpAddress { set; get; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public ComponentContextCmif()
				: base(ComponentType.TypeCMIF)
		{
			CmHandle = null;
			IpAddress = "";
			PortNo = 0;
		}
	}

	/// <summary>
	/// Cmifラッパーサービス
	/// </summary>
	public class CmifWrapperGrpcServiceImpl : CmifWrapperGrpcService.CmifWrapperGrpcServiceBase
	{
		//ポートオープン用
		public static class CmConst
		{
			public const int CM_RELAY_NUM = 8;
		}
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct PassInfoStr
		{
			public byte cir;        // 回線番号
			public byte DeviceID;   // デバイスID
			public byte Adress;     // 中継先アドレス
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CmRlyInfo
		{
			public byte PassCount;      //中継段数

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = CmConst.CM_RELAY_NUM)]
			public PassInfoStr[] DSTPAS;    //往路中継パス

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = CmConst.CM_RELAY_NUM)]
			public PassInfoStr[] RETPAS;    //復路中継パス
		}
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CmOpen
		{
			// 通信一般情報
			public ushort pt_no;    //ポート番号
			public uint timeout;    //通信タイムアウト
			public uint LogonTout;  //ログオンタイムアウト
			public ushort Retry;    //リトライ回数
			public byte Attribute;  //通信属性
			public byte DataDirection;  //接続ターゲット設定

			// デバイス接続情報
			public ushort ut_no;    //ユニット番号
			public uint IPAddress;  //IPアドレス
			public byte PassCount;  //中継段数

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = CmConst.CM_RELAY_NUM)]
			public PassInfoStr[] DSTPAS;    //往路中継パス

			public ushort dev_no;   //CPU番号

			// 既存215中継対応
			public ushort nt_no;    //ネットワーク番号
			public ushort st_no;    //ステーション番号
		}

		//レジスタリード
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CplRegBlock
		{
			public byte s_v;        //S/V
			public byte t_v;        //T/V
			public uint DataAdr;    //データ番地
			public byte DwgType;    //DWG種別
			public byte kozu;       //子図面番号
			public byte magozu;     //孫図面番号
			public byte dummy;      //予備
			public ushort Count;    //レジスタ数
		}
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CplRegRreq
		{
			public ushort BlockCnt;     //要求ブロック数
			public IntPtr regparam;     //要求ブロックアドレス
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
			public byte[] filename;     //ファイル名(NULL詰め)
		}
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CplRegRans
		{
			public IntPtr buffer;   //応答データ
			public int size;        //バッファサイズ
			public int anssize;     //転送サイズ
		}
		internal static class CMIF
		{
			//ポートオープン
			[DllImport("CMIF.dll", CallingConvention = CallingConvention.StdCall)]
			public static extern IntPtr CmPortOpen(ref CmOpen open);

			//ポートクローズ
			[DllImport("CMIF.dll", CallingConvention = CallingConvention.StdCall)]
			public static extern uint CmPortClose(IntPtr handle);

			//レジスタリード
			[DllImport("CMIF.dll", CallingConvention = CallingConvention.StdCall)]
			public static extern uint CpPnlRegRead(IntPtr handle, ref CplRegRreq request, ref CplRegRans response);
		}

		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<CmifWrapperGrpcServiceImpl> _logger;
		/// <summary>
		/// コンポーネントマネージャインスタンス
		/// </summary>
		private readonly ComponentManager _componentManager;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーインスタンス</param>
		/// <param name="componentManager">コンポーネントマネージャインスタンス</param>
		public CmifWrapperGrpcServiceImpl(ILogger<CmifWrapperGrpcServiceImpl> logger, ComponentManager componentManager)
		{
			_logger = logger;
			_componentManager = componentManager;
		}

		/// <summary>
		/// コンポーネントオープン
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		///		bool isOnline:オンライン　true=オンライン　false=オフライン
		///		int32 portNo:論理ポート番号
		///		string ipAddress:IPアドレス
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		string componentHandle:コンポーネントハンドル
		/// </returns>
		[SupportedOSPlatform("windows")]
		public override Task<OpenComponentRes> OpenComponent(OpenComponentReq req, ServerCallContext context)
		{
			//通信プラットフォームのポートをオープンする
			var bytes = System.Net.IPAddress.Parse(req.IpAddress).GetAddressBytes();
			uint ip =
				((uint)bytes[3] << 24) |
				((uint)bytes[2] << 16) |
				((uint)bytes[1] << 8) |
				bytes[0];

			var open = new CmOpen
			{
				pt_no = (ushort)(req.PortNo - 1),   //ポート番号は０始まり
				timeout = 0x2710,
				LogonTout = 0x2720,
				Retry = 0,
				Attribute = 0x10,
				DataDirection = 0,
				ut_no = 0,
				IPAddress = ip,
				PassCount = 0,
				DSTPAS = new PassInfoStr[CmConst.CM_RELAY_NUM],
				dev_no = 1,
				nt_no = 0,
				st_no = 0
			};

			var cmifComponentContext = new ComponentContextCmif();
			IntPtr handle = CMIF.CmPortOpen(ref open);
			if (handle != IntPtr.Zero)
			{
				//	//オープン成功したので、アクセス情報に接続情報とハンドルを格納
				cmifComponentContext.CmHandle = handle;
				cmifComponentContext.PortNo = req.PortNo;
				cmifComponentContext.IpAddress = req.IpAddress;

				_componentManager.AddComponent(cmifComponentContext);
			}
			else
			{
				//オープン失敗
			}

			//返却する
			return Task.FromResult(new OpenComponentRes
			{
				ComponentHandle = cmifComponentContext.GetComponentHandle(),
			});
		}

		/// <summary>
		/// コンポーネントのクローズ
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		///		string componentHandle:コンポーネントハンドル
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		int32 result:実行結果
		/// </returns>
		[SupportedOSPlatform("windows")]
		public override Task<CloseComponentRes> CloseComponent(CloseComponentReq req, ServerCallContext context)
		{
			if (_componentManager.IsComponentHandleExist(req.ComponentHandle) == false)
			{
				//指定されたコンポーネントハンドルが登録されていない
				return Task.FromResult(new CloseComponentRes
				{
					Result = CmifWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}

			//通信プラットフォームのポートをクローズする
			ComponentContextCmif? component = (ComponentContextCmif?)_componentManager.GetComponent(req.ComponentHandle);
			if (component == null || component.CmHandle == null)
			{
				//コンポーネントハンドルまたは通信ハンドルが取得できない
				return Task.FromResult(new CloseComponentRes
				{
					Result = CmifWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}
			CMIF.CmPortClose(component.CmHandle.Value);

			//コンポーネントマネージャから削除する
			_componentManager.RemoveComponent(req.ComponentHandle);

			//返却する
			return Task.FromResult(new CloseComponentRes
			{
				Result = CmifWrapperErrorCode.NoError,
			});
		}

		//レジスタリード
		[SupportedOSPlatform("windows")]
		public override Task<RegReadRes> RegRead(RegReadReq req, ServerCallContext context)
		{
			//コンポーネントハンドルは登録されているか?_
			if(_componentManager.IsComponentHandleExist(req.ComponentHandle) == false)
			{
				//指定されたコンポーネントハンドルが登録されていない
				return Task.FromResult(new RegReadRes
				{
					Result = CmifWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}

			//レジスタよりレジスタ情報を取得する
			RegisterSource srcCode;
			if(Regsub.GcvtRly2src(req.Registar.ToUpper(), req.Registar.Length, out srcCode) != 0)
			{
				//指定されたレジスタが不正
				return Task.FromResult(new RegReadRes
				{
					Result = CmifWrapperErrorCode.InvalidRegister,
				});

			}
			//レジスタ情報を作成
			CplRegBlock block = new CplRegBlock();
			block.s_v = srcCode.bySV;
			block.t_v = srcCode.byTB;
			block.DataAdr = (uint)srcCode.nAddress;
			block.Count = 1;

			//unmanagedへメモリコピー
			IntPtr pBlock =
				Marshal.AllocHGlobal(
					Marshal.SizeOf<CplRegBlock>());

			Marshal.StructureToPtr(
				block,
				pBlock,
				false);

			var cmreq = new CplRegRreq
			{
				BlockCnt = 1,
				regparam = pBlock,
				filename = new byte[9]
			};

			//応答バッファ確保
			IntPtr pBuffer = Marshal.AllocHGlobal(4096);
			//クリア
			byte[] zeros = new byte[4096];
			Marshal.Copy(zeros, 0, pBuffer, zeros.Length);

			var cmans = new CplRegRans
			{
				buffer = pBuffer,
				size = 4096,
				anssize = 0
			};

			string regValue = new string("");
			ComponentContextCmif? component = (ComponentContextCmif?)_componentManager.GetComponent(req.ComponentHandle);
			if (component == null || component.CmHandle == null)
			{
				//コンポーネントハンドル又は通信パンドルが取得できない
				return Task.FromResult(new RegReadRes
				{
					Result = CmifWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}
			IntPtr? handle = component.CmHandle;
			//レジスタ読み込み実行
			uint rc = CMIF.CpPnlRegRead(
					handle!.Value,
					ref cmreq,
					ref cmans);
			if (rc == 0)
			{
				CplRegBlock reg = Marshal.PtrToStructure<CplRegBlock>(cmans.buffer);
				int size = Marshal.SizeOf<CplRegBlock>();
				if ((block.t_v & 0xf0)>>4 == 0x01)
				{
					//Word型
					short value = Marshal.ReadInt16(cmans.buffer, size);
					regValue = value.ToString();
				}
				else if ((block.t_v & 0xf0) >> 4 == 0x02)
				{
					//Long型
					int value = Marshal.ReadInt32(cmans.buffer, size);
					regValue = value.ToString();
				}
			}
			Marshal.FreeHGlobal(pBuffer);
			Marshal.FreeHGlobal(pBlock);

			//返却する
			return Task.FromResult(new RegReadRes
			{
				Result = CmifWrapperErrorCode.NoError,
				Value = regValue
			});
		}
	}
}

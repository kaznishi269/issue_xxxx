using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yaskawa.ControllerEngineeringCommon.Utility
{
	/// <summary>
	/// 共通定義
	/// </summary>
	public static class LegacyConstants
	{
		//C7l_sdef.h
		public const int OK = 0;   // 正常終了
		public const int NG = -1;  // エラー
		public const int ON = 1;   // フラグ ON
		public const int OFF = 0;   // フラグ OFF
		public const int STOPPER = -1;  // 検索ストッパー

		//C7l_comp.h
		/************************************************************************/
		/* 中間コード アドレスモード                                           */
		/************************************************************************/
		public const ushort AM_N = 0x0000; // 名前、コメント
		public const ushort AM_IMM = 0x0100; // 定数
		public const ushort AM_C = 0x0200; // ＃変数
		public const ushort AM_D = 0x0300; // Ｄ変数
		public const ushort AM_I = 0x0400; // Ｉ変数
		public const ushort AM_O = 0x0500; // Ｏ変数
		public const ushort AM_M = 0x0600; // Ｍ変数
		public const ushort AM_S = 0x0700; // Ｓ変数
		public const ushort AM_X = 0x0800; // Ｘ変数
		public const ushort AM_Y = 0x0900; // Ｙ変数
		public const ushort AM_Z = 0x0A00; // Ｚ変数
		public const ushort AM_A = 0x0B00; // Ａ変数
		public const ushort AM_IJ = 0x0C00; // 演算レジスタ
		public const ushort AM_E = 0x0D00; // Ｅ変数
		public const ushort AM_GC = 0x0E00; // Ｃ変数
		public const ushort AM_ADR = 0x0F00; // シンボルテーブル参照
		public const ushort AM_G = 0x1000; // Ｇ変数

		public const ushort AM_DBGREG = 0x2000; // デバッグレジスタＢ CP-316
		public const ushort AM_DBB = 0x2100; // デバッグレジスタＢ CP-316
		public const ushort AM_DBF = 0x2200; // デバッグレジスタＦ CP-316
		public const ushort AM_DBU = 0x2300; // デバッグレジスタＵ CP-316
		public const ushort AM_DBW = 0x2400; // デバッグレジスタＷ CP-316
		public const ushort AM_DBG = 0x2500; // デバッグレジスタＧ CP-317
		public const ushort AM_DBH = 0x2600; // デバッグレジスタＨ CP-317
		public const ushort AM_DBJ = 0x2700; // デバッグレジスタＪ CP-317
		public const ushort AM_DBN = 0x2800; // デバッグレジスタＮ CP-317
		public const ushort AM_DBP = 0x2900; // デバッグレジスタＰ CP-317
		public const ushort AM_DBQ = 0x2A00; // デバッグレジスタＱ CP-317
		public const ushort AM_DBR = 0x2B00; // デバッグレジスタＲ CP-317
		public const ushort AM_DBT = 0x2C00; // デバッグレジスタＴ CP-317

		/************************************************************************/
		/* 中間コード データタイプ（オペランドタイプ）                         */
		/************************************************************************/
		public const ushort OT_NOP = 0x00;   // ＮＯＰ
		public const ushort OT_B = 0x01;   // ビット
		public const ushort OT_C = 0x02;   // バイト
		public const ushort OT_W = 0x03;   // ワード
		public const ushort OT_L = 0x04;   // ロング
		public const ushort OT_F = 0x05;   // フロート
		public const ushort OT_A = 0x06;   // ポインタ
		public const ushort OT_D = 0x07;   // ダブル
		public const ushort OT_Q = 0x08;   // QUAD

		public const ushort OT_HW = 0x0B;   // ワード（HEX）
		public const ushort OT_HL = 0x0C;   // ロング（HEX）
		public const ushort OT_HQ = 0x0D;   // クアッド（HEX）

		public const ushort OT_DLY = 0x10;   // ディレイタイマ用（現在値）

		public const byte EXTADDRESS_MASK = 0x40;

		//Filapi
		public const long NO_ERROR = 0;
		public const long PARAM_ERR = -1;
		public const long FLERR_NONAME = -2;
		public const long FLERR_BADSTR = -3;
		public const long FLERR_BADNAME = -4;
		public const long FLERR_BADSPC = -5;

		//MiscUtil
		public const int MMRECMAX = 4094;
		public const int DWGLEN = 8;
		public const int CS_BYTELENGTH_32 = 32;
		public const char PREFIX_NAMEC_DWGEX = 'E';
		public const char PREFIX_NAMEc_DWGEX = 'e';
		public const char PREFIX_NAMEC_DWGP = 'P';
		public const char PREFIX_NAMEc_DWGP = 'p';
		public const short PRGID_LADDER_DWGA = 0x0001;
		public const short PRGID_LADDER_DWGI = 0x0002;
		public const short PRGID_LADDER_DWGEX = 0x0003;
		public const short PRGID_LADDER_DWGH = 0x0004;
		public const short PRGID_LADDER_DWGL = 0x0006;
		public const short PRGID_LADDER_FUNC = 0x0008;
		public const short PRGID_MOTION_PRG = 0x000B;
		public const short PRGID_LADDER_DWGP = 0x0011;
		public const short PRGID_LADDER_MACRO = 0x7F02;
	}
}

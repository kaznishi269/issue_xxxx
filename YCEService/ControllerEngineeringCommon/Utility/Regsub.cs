using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Yaskawa.ControllerEngineeringCommon.Utility.cvtapi;

namespace Yaskawa.ControllerEngineeringCommon.Utility
{
	public class Regsub
	{
		public sealed class RegisterSource
		{
			public byte bySV;
			public byte byTB;
			public int nAddress;
		}

		/************************************************************************/
		/* V2 アドレスモード変換テーブル                                       */
		/************************************************************************/
		public struct S_DC_V2AM
		{
			public ushort amdat;   // AMコード
			public byte dspcd;   // レジスタ文字
			public short hex;     // HEX表示

			public S_DC_V2AM(ushort amdat, char dspcd, short hex)
			{
				this.amdat = amdat;
				this.dspcd = (byte)dspcd;
				this.hex = hex;
			}
		};

		public static readonly S_DC_V2AM[] dc_2vam =
		{
			new S_DC_V2AM(LegacyConstants.AM_S,   'S', LegacyConstants.OFF),       //  0: Ｓレジスタ
			new S_DC_V2AM(LegacyConstants.AM_I,   'I', LegacyConstants.ON),        //  1: Ｉレジスタ
			new S_DC_V2AM(LegacyConstants.AM_O,   'O', LegacyConstants.ON),        //  2: Ｏレジスタ
			new S_DC_V2AM(LegacyConstants.AM_M,   'M', LegacyConstants.OFF),       //  3: Ｍレジスタ
			new S_DC_V2AM(LegacyConstants.AM_C,   '#', LegacyConstants.OFF),       //  4: ＃レジスタ
			new S_DC_V2AM(LegacyConstants.AM_D,   'D', LegacyConstants.OFF),       //  5: Ｄレジスタ
			new S_DC_V2AM(LegacyConstants.AM_E,   'E', LegacyConstants.OFF),       //  6: Ｅレジスタ
			new S_DC_V2AM(LegacyConstants.AM_GC,  'C', LegacyConstants.OFF),       //  7: Ｃレジスタ
			new S_DC_V2AM(LegacyConstants.AM_X,   'X', LegacyConstants.OFF),       //  8: Ｘレジスタ
			new S_DC_V2AM(LegacyConstants.AM_Y,   'Y', LegacyConstants.OFF),       //  9: Ｙレジスタ
			new S_DC_V2AM(LegacyConstants.AM_Z,   'Z', LegacyConstants.OFF),       // 10: Ｚレジスタ
			new S_DC_V2AM(LegacyConstants.AM_A,   'A', LegacyConstants.OFF),       // 11: Ａレジスタ
			new S_DC_V2AM(LegacyConstants.AM_G,   'G', LegacyConstants.OFF),       // 12: Ｇレジスタ
			new S_DC_V2AM(LegacyConstants.AM_DBF, 'F', LegacyConstants.ON),        // 13: Ｆレジスタ(dbgR)
			new S_DC_V2AM(LegacyConstants.AM_DBU, 'U', LegacyConstants.ON),        // 14: Ｕレジスタ(dbgR)
			new S_DC_V2AM(LegacyConstants.AM_DBW, 'W', LegacyConstants.ON),        // 15: Ｗレジスタ(dbgR)
			new S_DC_V2AM(LegacyConstants.AM_DBG, '-', LegacyConstants.ON),        // 16: 無効レジスタ(dbgR)
													// MP3000のGレジスタ対応により本テーブルを無効とする
			new S_DC_V2AM(LegacyConstants.AM_DBH, 'H', LegacyConstants.ON),        // 17: Ｈレジスタ(dbgR)
			new S_DC_V2AM(LegacyConstants.AM_DBJ, 'J', LegacyConstants.ON),        // 18: Ｊレジスタ(dbgR)
			new S_DC_V2AM(LegacyConstants.AM_DBN, 'N', LegacyConstants.ON),        // 19: Ｎレジスタ(dbgR)
			new S_DC_V2AM(LegacyConstants.AM_DBP, 'P', LegacyConstants.ON),        // 20: Ｐレジスタ(dbgR)
			new S_DC_V2AM(LegacyConstants.AM_DBQ, 'Q', LegacyConstants.ON),        // 21: Ｑレジスタ(dbgR)
			new S_DC_V2AM(LegacyConstants.AM_DBR, 'R', LegacyConstants.ON),        // 22: Ｒレジスタ(dbgR)
			new S_DC_V2AM(LegacyConstants.AM_DBT, 'T', LegacyConstants.ON),        // 23: Ｔレジスタ(dbgR)

			new S_DC_V2AM(0xFFFF, ' ', LegacyConstants.STOPPER)     // 検索ストッパー
		};

		/*----------------------------------------------------------------------*/
		/* 中間コードOT, ソースコードT/B 変換テーブル                          */
		/*----------------------------------------------------------------------*/
		public struct S_DC_T2OT
		{
			public ushort otdat;   // OTコード
			public byte dspcd;   // レジスタ文字

			public S_DC_T2OT(ushort otdat, char dspcd)
			{
				this.otdat = otdat;
				this.dspcd = (byte)dspcd;
			}
		}

		/*----------------------------------------------------------------------*/
		/* OT, T/B 変換テーブル                                                 */
		/*----------------------------------------------------------------------*/
		private static readonly S_DC_T2OT[] dc_t2ot =
		{
			//       OT      DSP       T/B
			new S_DC_T2OT(LegacyConstants.OT_B,   'B'),   //  0 : Bit
			new S_DC_T2OT(LegacyConstants.OT_W,   'W'),   //  1 : Word
			new S_DC_T2OT(LegacyConstants.OT_L,   'L'),   //  2 : Long
			new S_DC_T2OT(LegacyConstants.OT_F,   'F'),   //  3 : Float
			new S_DC_T2OT(LegacyConstants.OT_Q,   'Q'),   //  4 : Quad
			new S_DC_T2OT(LegacyConstants.OT_D,   'D'),   //  5 : Double
			new S_DC_T2OT(0x0000, ' '),   //  6 : Spare
			new S_DC_T2OT(0x0000, ' '),   //  7 : Spare
			new S_DC_T2OT(LegacyConstants.OT_A,   'A'),   //  8 : Address
			new S_DC_T2OT(0x0000, ' '),   //  9 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 10 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 11 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 12 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 13 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 14 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 15 : Spare
			new S_DC_T2OT(0xFFFF, ' '),   // STOPPER
		};


		/*----------------------------------------------------------------------*/
		/* Double用 OT, T/B 変換テーブル                                       */
		/*----------------------------------------------------------------------*/
		private static readonly S_DC_T2OT[] dc_t2otDbl =
		{
			new S_DC_T2OT(LegacyConstants.OT_B,   'B'),   //  0 : Bit
			new S_DC_T2OT(LegacyConstants.OT_W,   'W'),   //  1 : Word
			new S_DC_T2OT(LegacyConstants.OT_L,   'L'),   //  2 : Long
			new S_DC_T2OT(LegacyConstants.OT_F,   'F'),   //  3 : Float
			new S_DC_T2OT(LegacyConstants.OT_D,   'D'),   //  4 : Double
			new S_DC_T2OT(0x0000, ' '),   //  5 : Spare
			new S_DC_T2OT(0x0000, ' '),   //  6 : Spare
			new S_DC_T2OT(0x0000, ' '),   //  7 : Spare
			new S_DC_T2OT(LegacyConstants.OT_A,   'A'),   //  8 : Address
			new S_DC_T2OT(0x0000, ' '),   //  9 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 10 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 11 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 12 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 13 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 14 : Spare
			new S_DC_T2OT(0x0000, ' '),   // 15 : Spare
			new S_DC_T2OT(0xFFFF, ' '),   // STOPPER
		};

		/*----------------------------------------------------------------------*/
		/* BIN to 文字変換テーブル                                              */
		/*----------------------------------------------------------------------*/

		public struct HCNV
		{
			public byte bincv;
			public byte asccv;

			public HCNV(byte bincv, char asccv)
			{
				this.bincv = bincv;
				this.asccv = (byte)asccv;
			}
		}

		private static readonly HCNV[] h1_dat =
		{
			new HCNV(0x00, '0'), new HCNV(0x01, '1'),
			new HCNV(0x02, '2'), new HCNV(0x03, '3'),
			new HCNV(0x04, '4'), new HCNV(0x05, '5'),
			new HCNV(0x06, '6'), new HCNV(0x07, '7'),
			new HCNV(0x08, '8'), new HCNV(0x09, '9'),
			new HCNV(0x0A, 'A'), new HCNV(0x0B, 'B'),
			new HCNV(0x0C, 'C'), new HCNV(0x0D, 'D'),
			new HCNV(0x0E, 'E'), new HCNV(0x0F, 'F')
		};

		/// <summary>
		/// レジスタ関連サブルーチン
		/// M365Copilotで、C言語よりC#に移植
		/// \ProductIDE\CimScope\NeoCommonLibProj\YcsCommonLib\YeStdLib80\SysSub\PrgSrc\Regsub.c
		/// </summary>
		public static int GcvtRly2src(
			string datAdr,
			int datLen,
			out RegisterSource srcCode)
		{
			int rno;
			int i, j;
			int cnt = 0;
			bool bFlag = false;
			byte tbDat;
			uint wklrc;
			int len;
			EDFRM edfrm = new cvtapi.EDFRM();
			int rc;

			srcCode = new RegisterSource();

			// --------------------------------------
			// S/Vのデータをコードにおとす
			// --------------------------------------
			rc = LegacyConstants.NG;
			rno = 0;

			for (; dc_2vam[rno].hex != LegacyConstants.STOPPER; rno++)
			{
				// Cではdat_adr[cnt]を直接参照しているが、
				// C#では範囲外アクセス防止が必要
				if (cnt >= datLen || cnt >= datAdr.Length)
					return LegacyConstants.NG;

				if (datAdr[cnt] == ' ')
				{
					// 空白NG
					break;
				}

				if (datAdr[cnt] != dc_2vam[rno].dspcd)
				{
					// レジスタ種別不一致
					continue;
				}

				// レジスタ種別設定
				srcCode.bySV = (byte)rno;

				if (rno >= 0x0A)
				{
					// デバッグレジスタ
					if (rno > 16)
					{
						// CP-317 Debug Register
						srcCode.bySV = 0x0F;
						srcCode.bySV |= (byte)((rno - 16) << 4);
					}
					else
					{
						// CP-316 Debug Register
						srcCode.bySV = (byte)rno;
					}
				}

				cnt++;
				rc = LegacyConstants.OK;
				break;
			}

			if (rc != LegacyConstants.OK)
				return LegacyConstants.NG;

			// ----------------------------------------------
			// T/Bのデータをコードにおとす
			// ----------------------------------------------
			rc = LegacyConstants.NG;

			for (i = 0; dc_t2ot[i].otdat != 0xFFFF; i++)
			{
				if (cnt >= datLen || cnt >= datAdr.Length)
					return LegacyConstants.NG;

				if (datAdr[cnt] == ' ')
				{
					// 途中スペース NG
					break;
				}

				if (datAdr[cnt] != dc_t2ot[i].dspcd)
				{
					// データタイプ不一致
					continue;
				}

				if (dc_t2ot[i].dspcd == 'B')
				{
					bFlag = true;
				}

				// データタイプ設定
				tbDat = (byte)i;
				tbDat = (byte)(tbDat << 4);

				// High 4bit set
				srcCode.byTB = tbDat;

				cnt++;
				rc = LegacyConstants.OK;
				break;
			}

			if (rc != LegacyConstants.OK)
				return LegacyConstants.NG;

			// --------------------------------------
			// データ番地をコードにおとす
			// --------------------------------------

			// MP3000はアドレス文字列が可変長であるため、
			// アドレス文字列の終端を探す
			// バッファ最後尾 / 空白 / Null文字
			for (len = cnt;
				 len < datLen &&
				 len < datAdr.Length &&
				 datAdr[len] != ' ' &&
				 datAdr[len] != '\0';
				 len++)
			{
			}

			/*
			 * 元コード：
			 *
			 * if (!(0-9) && !(A-F))
			 *     len--;
			 *
			 * len == cnt の場合、len - 1 が不正になるため
			 * C#版ではチェックを追加。
			 */
			if (len <= cnt)
				return LegacyConstants.NG;

			char lastChar = datAdr[len - 1];

			if (!((lastChar >= '0' && lastChar <= '9') ||
				  (lastChar >= 'A' && lastChar <= 'F')))
			{
				// 添え字分減らす
				len--;
			}

			if (bFlag)
			{
				// Bit番号分減らす
				len--;
			}

			// レジスタ種別、データ型分文字列長を調整
			len -= cnt;

			if (len <= 0)
				return LegacyConstants.NG;

			// --------------------------------------
			// アドレス文字列 → 数値変換
			// --------------------------------------

			string addressText = datAdr.Substring(cnt, len);

			if (dc_2vam[rno].hex == LegacyConstants.ON)
			{
				rc = YcvtHex2b(
					edfrm,
					addressText,
					len,
					out wklrc);
			}
			else
			{
				int wklrcint;
				rc = YcvtDec2b(
					edfrm,
					addressText,
					len,
					0,
					out wklrcint);
				wklrc = (uint)wklrcint;
			}

			if (rc != LegacyConstants.OK)
				return rc;

			// レジスタ番号設定
			srcCode.nAddress = unchecked((int)wklrc);

			// 取得したアドレスのサイズを確認
			if (wklrc > 0xFFFF)
			{
				// 拡張アドレス指定埋め込み
				srcCode.bySV |= LegacyConstants.EXTADDRESS_MASK;
			}

			cnt += len;

			// --------------------------------------
			// ビットアドレスをコードにおとす
			// --------------------------------------
			if (bFlag)
			{
				if (cnt >= datLen || cnt >= datAdr.Length)
				{
					// 編集レングス終了
					return LegacyConstants.NG;
				}

				rc = LegacyConstants.NG;

				for (j = 0; j < 16; j++)
				{
					if (datAdr[cnt] == h1_dat[j].asccv)
					{
						srcCode.byTB |= h1_dat[j].bincv;
						rc = LegacyConstants.OK;
						break;
					}
				}
			}

			return rc;
		}
	}
}

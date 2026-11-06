using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yaskawa.ControllerEngineeringCommon.Utility
{
	/// <summary>
	/// ファイル関連共通関数
	/// M365Copilotで、C言語よりC#に移植
	/// \ProductIDE\CimScope\NeoCommonLibProj\YcsCommonLib\YeBaseLib80\Common\cvtapi.c
	/// </summary>
	public class cvtapi
	{
		/****************************************************************/
		/* 編集情報構造体                                               */
		/****************************************************************/
		public struct EDFRM
		{
			public int leng;   // 全桁数
			public int decpt;  // 小数点以下桁数
			public int zsp;    // 0サプレス
							   // 0: 数字先頭をスペースにする
							   // 1: 数字先頭を0にする
			public int sign;   // 符号有無
							   // 0: 有り
							   // 1: 無し
		}

		public static int YcvtHex2b(
			EDFRM edfrm,        // [I] 編集情報構造体（未使用・将来用）
			string edt_data,     // [I] 16進文字列
			int edt_len,         // [I] 文字列長
			out uint cnv_data)   // [O] バイナリ格納データ
		{
			int rc = LegacyConstants.OK;
			long w_data;

			// データ初期設定
			cnv_data = 0;

			rc = YcvtQHex2b(
				edfrm,
				edt_data,
				edt_len,
				out w_data);

			if (rc == LegacyConstants.OK)
			{
				// 元Cコード:
				// if ((*(ULONGLONG*)&w_data) > ULONG_MAX)

				ulong unsignedData = unchecked((ulong)w_data);

				if (unsignedData > uint.MaxValue)
				{
					rc = LegacyConstants.NG;
				}
				else
				{
					cnv_data = (uint)unsignedData;
				}
			}

			return rc;
		}

		public static int YcvtQHex2b(
			EDFRM edfrm,         // [I] 編集情報構造体（未使用・将来用）
			string edt_data,     // [I] 16進文字列
			int edt_len,         // [I] 文字列長
			out long cnv_data)   // [O] バイナリ格納データ
		{
			int i;
			int rc = LegacyConstants.OK;

			// edfrm は未使用（将来用）
			_ = edfrm;

			// データ初期設定
			cnv_data = 0;

			// unsigned 64bitとして変換処理を行う
			ulong work = 0;

			// 前スペースチェック
			for (i = 0; i < edt_len; i++)
			{
				if (edt_data[i] != ' ')
				{
					break;
				}
			}

			if (i >= edt_len)
			{
				// オールスペースNG
				rc = LegacyConstants.NG;
			}

			for (; i < edt_len && rc == LegacyConstants.OK; i++)
			{
				// スペースが出現した場合
				if (edt_data[i] == ' ')
				{
					for (; i < edt_len; i++)
					{
						if (edt_data[i] != ' ')
						{
							// 途中スペースNG
							rc = LegacyConstants.NG;
							break;
						}
					}

					break;
				}

				// NULL文字が出現した場合
				if (edt_data[i] == '\0')
				{
					for (; i < edt_len; i++)
					{
						if (edt_data[i] != '\0')
						{
							// 途中NULL NG
							rc = LegacyConstants.NG;
							break;
						}
					}

					break;
				}

				// ulongの範囲内か
				if (ulong.MaxValue / 16UL < work)
				{
					rc = LegacyConstants.NG;
					break;
				}

				ulong digit;

				if (edt_data[i] >= '0' && edt_data[i] <= '9')
				{
					// 数字 0～9
					digit = (ulong)(edt_data[i] - '0');
				}
				else if (edt_data[i] >= 'A' && edt_data[i] <= 'F')
				{
					// A～F
					digit = (ulong)(edt_data[i] - 'A' + 10);
				}
				else if (edt_data[i] >= 'a' && edt_data[i] <= 'f')
				{
					// a～f
					digit = (ulong)(edt_data[i] - 'a' + 10);
				}
				else
				{
					// 入力文字NG
					rc = LegacyConstants.NG;
					break;
				}

				// 最後の加算も含めてulongの範囲をチェック
				if (work > (ulong.MaxValue - digit) / 16UL)
				{
					rc = LegacyConstants.NG;
					break;
				}

				work = (work * 16UL) + digit;
			}

			if (rc == LegacyConstants.OK)
			{
				// C版ではULONGLONGのビット列をLONGLONG領域へ格納しているため、
				// uncheckedで同じビットパターンをlongへ移す
				cnv_data = unchecked((long)work);
			}

			return rc;
		}

		public static int YcvtDec2b(
			EDFRM edfrm,         // [I] 編集情報構造体（未使用・将来用）
			string edt_data,     // [I] 10進文字列
			int edt_lng,         // [I] 文字列長
			int edt_dpt,         // [I] 小数点オフセット
			out int cnv_data)    // [O] バイナリ格納データ
		{
			int rc = LegacyConstants.OK;
			long w_data;

			// データ初期設定
			cnv_data = 0;

			rc = YcvtQDec2b(
				edfrm,
				edt_data,
				edt_lng,
				edt_dpt,
				out w_data);

			if (rc == LegacyConstants.OK)
			{
				// LONG（32bit）の範囲内かチェック
				if (w_data > int.MaxValue || w_data < int.MinValue)
				{
					rc = LegacyConstants.NG;
				}
				else
				{
					cnv_data = (int)w_data;
				}
			}

			return rc;
		}

		public static int YcvtQDec2b(
	EDFRM edfrm,         // [I] 編集情報構造体（未使用・将来用）
	string edt_data,     // [I] 10進文字列
	int edt_lng,         // [I] 文字列長
	int edt_dpt,         // [I] 小数点オフセット
	out long cnv_data)   // [O] バイナリ格納データ
		{
			int dpt = 0;         // 小数点以下桁数
			int sign = 0;
			int pcnt = 0;
			int w_dpt;
			int bno;
			int i;
			int rc = LegacyConstants.OK;

			// edfrm は未使用（将来用）
			_ = edfrm;

			// データ初期設定
			cnv_data = 0;

			// 引数チェック
			if (edt_data == null || edt_lng <= 0 || edt_lng > edt_data.Length)
			{
				return LegacyConstants.NG;
			}

			// 前スペースチェック
			for (i = 0; i < edt_lng; i++)
			{
				if (edt_data[i] != ' ')
				{
					break;
				}
			}

			if (i >= edt_lng)
			{
				// オールスペースNG
				return LegacyConstants.NG;
			}

			// 符号チェック
			if (edt_data[i] == '-')
			{
				// 負データ
				sign = 1;
				i++;

				// "-" のみはNG
				if (i >= edt_lng)
				{
					return LegacyConstants.NG;
				}

				if (edt_data[i] > '9' || edt_data[i] < '0')
				{
					rc = LegacyConstants.NG;
				}
			}
			else if (edt_data[i] == '+')
			{
				// 正データ
				sign = 0;
				i++;

				// "+" のみはNG
				if (i >= edt_lng)
				{
					return LegacyConstants.NG;
				}

				if (edt_data[i] > '9' || edt_data[i] < '0')
				{
					rc = LegacyConstants.NG;
				}
			}

			if (i >= edt_lng)
			{
				// レングスNG
				return LegacyConstants.NG;
			}

			// 小数点オフセット負値サポート
			if (edt_dpt < 0)
			{
				w_dpt = 0;
			}
			else
			{
				w_dpt = edt_dpt;
			}

			// 絶対値をulongで保持する。
			// long.MinValue の絶対値 0x8000000000000000 まで扱うため。
			ulong work = 0;

			for (; i < edt_lng && rc == LegacyConstants.OK; i++)
			{
				if (edt_data[i] == ' ')
				{
					// スペース以降はすべてスペースでなければNG
					for (; i < edt_lng; i++)
					{
						if (edt_data[i] != ' ')
						{
							// 途中スペースNG
							rc = LegacyConstants.NG;
							break;
						}
					}

					break;
				}

				if (edt_data[i] == '\0')
				{
					// NULL以降はすべてNULLでなければNG
					for (; i < edt_lng; i++)
					{
						if (edt_data[i] != '\0')
						{
							// 途中NULL NG
							rc = LegacyConstants.NG;
							break;
						}
					}

					break;
				}

				if (edt_data[i] == '.')
				{
					if (edt_dpt == 0)
					{
						// 小数点指定NG
						rc = LegacyConstants.NG;
						break;
					}

					if (dpt != 0)
					{
						// 小数点2個以上NG
						rc = LegacyConstants.NG;
						break;
					}

					dpt++;
					continue;
				}

				if (edt_data[i] < '0' || edt_data[i] > '9')
				{
					// 数字以外NG
					rc = LegacyConstants.NG;
					break;
				}

				if (dpt != 0)
				{
					if (edt_dpt >= 0 && dpt >= w_dpt + 1)
					{
						// 小数点以下桁数終了
						break;
					}

					dpt++;
					pcnt++;
				}

				ulong digit = (ulong)(edt_data[i] - '0');

				/*
				 * 正数:
				 *     long.MaxValue =  9223372036854775807
				 *
				 * 負数:
				 *     long.MinValue = -9223372036854775808
				 *
				 * 負数の場合だけ絶対値 9223372036854775808 まで許可する。
				 */
				ulong limit = (sign == 1)
					? 0x8000000000000000UL
					: 0x7FFFFFFFFFFFFFFFUL;

				// work * 10 + digit が範囲内かチェック
				if (work > (limit - digit) / 10UL)
				{
					rc = LegacyConstants.NG;
					break;
				}

				work = work * 10UL + digit;
			}

			if (rc == LegacyConstants.OK)
			{
				if (edt_dpt >= 0 && dpt < w_dpt + 1)
				{
					// 小数部入力なし時の設定
					if (dpt == 0)
					{
						dpt++;
					}

					int count = w_dpt + 1 - dpt;

					for (i = 0; i < count; i++)
					{
						ulong limit = (sign == 1)
							? 0x8000000000000000UL
							: 0x7FFFFFFFFFFFFFFFUL;

						// work *= 10 のオーバーフローチェック
						if (work > limit / 10UL)
						{
							rc = LegacyConstants.NG;
							break;
						}

						work *= 10UL;
					}
				}
			}

			if (rc == LegacyConstants.OK)
			{
				if (sign == 1)
				{
					// マイナス値設定
					if (work == 0x8000000000000000UL)
					{
						cnv_data = long.MinValue;
					}
					else
					{
						cnv_data = -(long)work;
					}
				}
				else
				{
					cnv_data = (long)work;
				}
			}

			// 小数点オフセット負値サポート
			if (rc == LegacyConstants.OK && edt_dpt < 0)
			{
				w_dpt = -edt_dpt;
				bno = w_dpt - pcnt;

				if (bno < 0)
				{
					rc = LegacyConstants.NG;
				}
				else
				{
					for (i = 0; i < bno; i++)
					{
						cnv_data /= 10;
					}
				}
			}

			return rc;
		}
	}
}

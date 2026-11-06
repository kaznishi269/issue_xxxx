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
	/// \ProductIDE\CimScope\NeoCommonLibProj\YcsCommonLib\YeBaseLib80\Common\Filapi.c
	/// </summary>
	internal class Filapi
	{
		/// <summary>
		/// /************************************************************************/
		/// /*									*/
		/// /*関数名:								*/
		/// /*	YfilChkFileChar( )	ファイル名が禁止名や禁止文字かチェック	*/
		/// /*機能:									*/
		/// /*  ファイル名がシステムで禁止されている名前や				*/
		/// /*	禁止文字を使っていないかチェックする。				*/
		/// /*									*/
		/// /*									*/
		/// /*履歴：	1998.02.12	桑村竜太郎				*/
		/// /*									*/
		/// /************************************************************************/
		/// </summary>
		public static long YfilChkFileChar(string? chkName)
		{
			//
			// パラメータエラー
			//
			if (chkName == null)
				return LegacyConstants.PARAM_ERR;

			if (chkName.Length == 0)
				return LegacyConstants.FLERR_NONAME;

			string tmpPath = chkName.ToUpperInvariant();

			//
			// システム予約名チェック
			//
			long rc = LYfilChkReserveName(tmpPath);
			if (rc != LegacyConstants.NO_ERROR)
				return rc;

			//
			// 使用禁止文字チェック
			//
			rc = LYfilChkCantUseChar(tmpPath);

			return rc;
		}

		/// <summary>
		/// /************************************************************************/
		/// /*									*/
		/// /*関数名:								*/
		/// /*	LYfilChkReserveName( )	ファイル名がｼｽﾃﾑ予約かチェック		*/
		/// /*機能:									*/
		/// /*  ファイル名がｼｽﾃﾑ予約かチェック					*/
		/// /*									*/
		/// /*履歴：	1998.02.12	桑村竜太郎				*/
		/// /*									*/
		/// /************************************************************************/
		/// </summary>
		private static long LYfilChkReserveName(string? chkName)
		{
			if (chkName == null)
				return LegacyConstants.PARAM_ERR;

			if (chkName.Length == 0)
				return LegacyConstants.FLERR_NONAME;

			string tmpName = chkName;

			//
			// 右側の空白削除
			//
			tmpName = tmpName.TrimEnd();

			//
			// 拡張子除去
			// COM1.TXT → COM1
			//
			int dotPos = tmpName.IndexOf('.');
			if (dotPos >= 0)
			{
				tmpName = tmpName.Substring(0, dotPos);
			}

			//
			// 呼び出し元で ToUpper 済みだが安全のため
			//
			tmpName = tmpName.ToUpperInvariant();

			string[] reserve =
			{
				"COM1",
				"COM2",
				"COM3",
				"COM4",
				"PRN",
				"NUL",
				"LPT1",
				"LPT2",
				"LPT3",
				"LPT4",
				"LPT5",
				"LPT6",
				"LPT7",
				"LPT8",
				"LPT9",
				"AUX",
				"CLOCK$",
				"CON",
				"$USRENV"
			};

			foreach (string name in reserve)
			{
				if (tmpName == name)
				{
					return LegacyConstants.FLERR_BADNAME;
				}
			}

			return LegacyConstants.NO_ERROR;
		}

		/// <summary>
		/// /************************************************************************/
		/// /*									*/
		/// /*関数名:								*/
		/// /*	LYfilChkCantUseChar( )	文字列が禁止文字を使用していないか?	*/
		/// /*機能:									*/
		/// /*  文字列が禁止文字を使用していないかチェックする。			*/
		/// /*									*/
		/// /*履歴：	1998.02.12	桑村竜太郎				*/
		/// /*									*/
		/// /************************************************************************/
		/// </summary>
		/// <param name="chkName"></param>
		/// <returns></returns>
		private static long LYfilChkCantUseChar(string? chkName)
		{
			if (chkName == null)
				return LegacyConstants.PARAM_ERR;

			if (chkName.Length == 0)
				return LegacyConstants.FLERR_NONAME;

			string tmpName = chkName.TrimEnd();

			const string taboo = " \\/:,;*?\"<>|";

			for (int i = 0; i < tmpName.Length; i++)
			{
				char ch = tmpName[i];

				//
				// 元コードの YstrIsMultiByte()
				//
				if (IsMultiByte(ch))
					continue;

				//
				// ファイル名途中の空白
				//
				if (ch == ' ')
					return LegacyConstants.FLERR_BADSPC;

				//
				// 禁止文字
				//
				if (taboo.IndexOf(ch) >= 0)
					return	LegacyConstants.FLERR_BADSTR;
			}

			return LegacyConstants.NO_ERROR;
		}

		/// <summary>
		/// マルチバイトチェック
		/// CpoilotがYstrIsMultiByte()の代わりに生成
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		private static bool IsMultiByte(char ch)
		{
			return Encoding.Default.GetByteCount(new[] { ch }) > 1;
		}
	}
}

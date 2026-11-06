using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yaskawa.ControllerEngineeringCommon.Utility
{
	/// <summary>
	/// 共通サブルーチンクラス
	/// \ProductIDE\CimScope\NeoCommonLibProj\YcsCommonLib\YeStdLib80\RegUtil\MiscUtil.cpp
	/// M365Copilotで、C++よりC#に移植
	/// </summary>
	public class MiscUtil
	{
		/// <summary>
		/// /************************************************************************/
		/// /*									*/
		/// /*関数名：								*/
		/// /*	FormalizeProgram() : プログラム名称の整形			*/
		/// /*									*/
		/// /*機能：								*/
		/// /*	プログラム文字列を整形する					*/
		/// /*									*/
		/// /*履歴：	2000.03.02 (YE) 山本英樹				*/
		/// /*									*/
		/// /************************************************************************/
		/// </summary>
		public static bool DbUtil_FormalizePrograms(
			string progIn,
			out string progOut,
			out short programType)
		{
			progOut = string.Empty;
			programType = 0;

			if (string.IsNullOrWhiteSpace(progIn))
				return false;

			progIn = progIn.Trim();

			//
			// マクロプログラム ($$XXXX)
			//
			if (progIn.StartsWith("$$", StringComparison.Ordinal))
			{
				string name = progIn.Substring(2).ToUpperInvariant();

				if (Filapi.YfilChkFileChar(name) != 0)
					return false;

				if (name.Length == 0 || name.Length > LegacyConstants.CS_BYTELENGTH_32)
					return false;

				if (name.Contains('.'))
					return false;

				if (!char.IsLetter(name[0]))
					return false;

				for (int i = 1; i < name.Length; i++)
				{
					char ch = name[i];

					if (char.IsLetterOrDigit(ch))
						continue;

					if (ch == '_' || ch == '-')
						continue;

					return false;
				}

				progOut = name;
				programType = LegacyConstants.PRGID_LADDER_MACRO;
				return true;
			}

			//
			// ラダー図面系
			//
			if (FormalizeLadderDwgProgram(progIn, out progOut))
			{
				int index = 0;

				//
				// $Gxx.xx 形式対応
				//
				if (progOut.StartsWith("$G", StringComparison.OrdinalIgnoreCase))
					index = 2;

				if (!GetDWGProgramType(progOut[index], out programType))
					return false;

				//
				// $G形式は '.' → '_'
				//
				if (progOut.StartsWith("$G", StringComparison.OrdinalIgnoreCase))
				{
					progOut = progOut.Replace('.', '_');
				}

				return true;
			}

			//
			// モーションプログラム
			//
			if (FormalizeMotionProgram(progIn, out progOut))
			{
				programType = LegacyConstants.PRGID_MOTION_PRG;
				return true;
			}

			//
			// ラダーファンクション
			//
			if (FormalizeLadderFuncProgram(progIn, out progOut))
			{
				programType = LegacyConstants.PRGID_LADDER_FUNC;
				return true;
			}

			return false;
		}

		/// <summary>
		/// /************************************************************************/
		/// /*									*/
		/// /*関数名：								*/
		/// /*	FormalizeLadderDwgProgram() : プログラム名称の整形		*/
		/// /*									*/
		/// /*機能：								*/
		/// /*	プログラム文字列を整形する					*/
		/// /*									*/
		/// /*履歴：	2000.03.02 (YE) 山本英樹				*/
		/// /*									*/
		/// /************************************************************************/
		/// </summary>
		private static bool FormalizeLadderDwgProgram(
			string progIn,
			out string progOut)
		{
			progOut = string.Empty;

			if (string.IsNullOrEmpty(progIn))
				return false;

			//
			// 入力文字列を破壊しないように "_" → "." を変換
			//
			string work = progIn.Replace('_', '.');

			int nIndex = 0;

			if (work.Length >= 2 &&
				work[0] == '$' &&
				work[1] == 'G')
			{
				nIndex = 2;
			}

			string target = work.Substring(nIndex);

			if (target.Length == 0)
				return false;

			char prog = char.ToUpperInvariant(target[0]);

			if (!IsDrawingFile(prog))
				return false;

			var sb = new StringBuilder();

			if (nIndex != 0)
				sb.Append("$G");

			sb.Append(prog);

			string leftStr = target.Substring(1);

			//
			// P図面判定
			//
			bool isDwgp =
				prog == LegacyConstants.PREFIX_NAMEC_DWGP ||
				prog == LegacyConstants.PREFIX_NAMEc_DWGP;

			if (isDwgp)
			{
				bool sourceFileName = nIndex != 0;

				int pos = sourceFileName
					? 1
					: leftStr.IndexOf('.');

				string scanNo;

				if (pos < 0)
					scanNo = leftStr;
				else
					scanNo = leftStr.Substring(0, pos);

				foreach (char ch in scanNo)
				{
					bool valid =
						(ch >= '0' && ch <= '9') ||
						(ch >= 'A' && ch <= 'F') ||
						(ch >= 'a' && ch <= 'f');

					if (!valid)
						return false;
				}

				if (scanNo.Length != 1)
					return false;

				sb.Append(scanNo);

				if (leftStr.Length == 1)
				{
					progOut = sb.ToString();
					return true;
				}

				if (!sourceFileName)
					pos++;

				leftStr = leftStr.Substring(pos);

				foreach (char ch in leftStr)
				{
					if (!(char.IsDigit(ch) || ch == '.'))
						return false;
				}

				//
				// 子図面
				//
				pos = sourceFileName
					? 2
					: leftStr.IndexOf('.');

				string kozuStr;

				if (pos < 0)
					kozuStr = leftStr;
				else
					kozuStr = leftStr.Substring(0, pos);

				if (kozuStr.Length < 1)
					return false;

				if (sourceFileName && kozuStr.Length != 2)
					return false;

				if (!long.TryParse(kozuStr, out long kozu))
					return false;

				if (kozu < 0 || kozu > 99)
					return false;

				if (sourceFileName)
					sb.Append($"{kozu:00}");
				else
					sb.Append($".{kozu:00}");

				if (sourceFileName
					? leftStr.Length == 2
					: pos < 0)
				{
					progOut = sb.ToString();
					return true;
				}

				if (!sourceFileName)
					pos++;

				leftStr = leftStr.Substring(pos);

				if (kozu == 0)
					return false;

				//
				// 孫図面
				//
				string magoStr = leftStr;

				if (magoStr.Length < 1)
					return false;

				foreach (char ch in magoStr)
				{
					if (!char.IsDigit(ch))
						return false;
				}

				if (sourceFileName && magoStr.Length != 2)
					return false;

				if (!short.TryParse(magoStr, out short magozu))
					return false;

				if (magozu < 1 || magozu > 99)
					return false;

				if (sourceFileName)
					sb.Append($"{magozu:00}");
				else
					sb.Append($".{magozu:00}");

				progOut = sb.ToString();
				return true;
			}

			//
			// 親図面のみ
			//
			if (leftStr.Length < 1)
			{
				progOut = sb.ToString();
				return true;
			}

			foreach (char ch in leftStr)
			{
				if (!(char.IsDigit(ch) || ch == '.'))
					return false;
			}

			//
			// 子図面
			//
			int dotPos = leftStr.IndexOf('.');

			string kozuPart =
				dotPos < 0
					? leftStr
					: leftStr.Substring(0, dotPos);

			if (!long.TryParse(kozuPart, out long childNo))
				return false;

			if (childNo < 0 || childNo > 99)
				return false;

			sb.Append($"{childNo:00}");

			if (dotPos < 0)
			{
				progOut = sb.ToString();
				return true;
			}

			//
			// 子図面が00で孫図面ありはNG
			//
			if (childNo == 0)
				return false;

			//
			// 孫図面
			//
			string grandChildPart = leftStr.Substring(dotPos + 1);

			if (grandChildPart.Length == 0)
				return false;

			foreach (char ch in grandChildPart)
			{
				if (!char.IsDigit(ch))
					return false;
			}

			if (!short.TryParse(grandChildPart, out short grandChildNo))
				return false;

			if (grandChildNo < 1 || grandChildNo > 99)
				return false;

			sb.Append($".{grandChildNo:00}");

			progOut = sb.ToString();
			return true;
		}

		/// <summary>
		/// /************************************************************************/
		/// /*									*/
		/// /*関数名：								*/
		/// /*	IsDrawingFile() : 図面ファイル判別				*/
		/// /*									*/
		/// /*機能：								*/
		/// /*	図面ファイルかどうかを判別する(case-insensitive)		*/
		/// /*									*/
		/// /*履歴：	1998.11.18 (YE) 山本英樹				*/
		/// /*									*/
		/// /************************************************************************/
		/// </summary>
		private static bool GetDWGProgramType(
		char dwg,
		out short programType)
		{
			programType = 0;

			switch (dwg)
			{
				case 'A':
				case 'a':
					programType = LegacyConstants.PRGID_LADDER_DWGA;
					break;

				case 'H':
				case 'h':
					programType = LegacyConstants.PRGID_LADDER_DWGH;
					break;

				case 'L':
				case 'l':
					programType = LegacyConstants.PRGID_LADDER_DWGL;
					break;

				case LegacyConstants.PREFIX_NAMEC_DWGEX:
				case LegacyConstants.PREFIX_NAMEc_DWGEX:
					programType = LegacyConstants.PRGID_LADDER_DWGEX;
					break;

				case 'I':
				case 'i':
					programType = LegacyConstants.PRGID_LADDER_DWGI;
					break;

				case LegacyConstants.PREFIX_NAMEC_DWGP:
				case LegacyConstants.PREFIX_NAMEc_DWGP:
					programType = LegacyConstants.PRGID_LADDER_DWGP;
					break;
			}

			return programType != 0;
		}

		/// <summary>
		/// /************************************************************************/
		/// /*									*/
		/// /*関数名：								*/
		/// /*	FormalizeMotionProgram() : モーションプログラムの整形		*/
		/// /*									*/
		/// /*機能：								*/
		/// /*									*/
		/// /*									*/
		/// /*									*/
		/// /*									*/
		/// /*									*/
		/// /*履歴：	2000.03.02 (YE) 山本英樹				*/
		/// /*									*/
		/// /************************************************************************/
		/// </summary>
		/// <param name="progIn"></param>
		/// <param name="progOut"></param>
		/// <returns></returns>
		private static bool FormalizeMotionProgram(
		string progIn,
		out string progOut)
		{
			progOut = string.Empty;

			if (string.IsNullOrEmpty(progIn))
				return false;

			int index = 0;

			if (progIn[0] == '$')
				index = 1;

			string programName = progIn.Substring(index)
									   .ToUpperInvariant();

			//
			// モーションプログラム名称チェック
			//
			if (!programName.StartsWith("MPM") &&
				!programName.StartsWith("MPS") &&
				!programName.StartsWith("SPM") &&
				!programName.StartsWith("SPS"))
			{
				return false;
			}

			//
			// '$' を除いた最大文字数チェック
			//
			if (programName.Length > LegacyConstants.DWGLEN - 1)
				return false;

			string prefix = programName.Substring(0, 3);
			string numberPart = programName.Substring(3);

			//
			// 数字のみ許可
			//
			foreach (char ch in numberPart)
			{
				if (!char.IsDigit(ch))
					return false;
			}

			if (!long.TryParse(numberPart, out long programNo))
				return false;

			//
			// プログラム番号: 1 ～ 4094
			//
			if (programNo <= 0 || programNo >= LegacyConstants.MMRECMAX + 1)
				return false;

			if (index == 1)
				progOut = $"${prefix}{programNo:000}";
			else
				progOut = $"{prefix}{programNo:000}";

			return true;
		}

		/// <summary>
		/// 
		/// /************************************************************************/
		/// /*									*/
		/// /*関数名：								*/
		/// /*	FormalizeLadderFuncProgram() : プログラム関数の整形		*/
		/// /*									*/
		/// /*機能：								*/
		/// /*	プログラム文字列を整形する					*/
		/// /*									*/
		/// /*									*/
		/// /*									*/
		/// /*									*/
		/// /*									*/
		/// /*履歴：	2000.03.02 (YE) 山本英樹				*/
		/// /*									*/
		/// /************************************************************************/
		/// </summary>
		private static bool FormalizeLadderFuncProgram(
			string progIn,
			out string progOut)
		{
			progOut = string.Empty;

			if (string.IsNullOrEmpty(progIn))
				return false;

			string programName = progIn.ToUpperInvariant();

			long rc = Filapi.YfilChkFileChar(progIn);
			if (rc != LegacyConstants.NO_ERROR)
				return false;

			//
			// 文字列長チェック (xxxxxxxx = 8文字)
			//
			if (programName.Length > 8)
				return false;

			//
			// '.' は使用不可
			//
			if (programName.Contains('.'))
				return false;

			//
			// 先頭文字は英字
			//
			if (programName[0] < 'A' || programName[0] > 'Z')
				return false;

			//
			// 2文字目以降チェック
			//
			for (int i = 1; i < programName.Length; i++)
			{
				char ch = programName[i];

				// 英字
				if (ch >= 'A' && ch <= 'Z')
					continue;

				// 数字
				if (ch >= '0' && ch <= '9')
					continue;

				// ハイフン
				if (ch == '-')
					continue;

				// アンダースコア
				if (ch == '_')
					continue;

				return false;
			}

			//
			// 元ソースでは図面名・モーション名チェックは
			// 呼び出し順で保証されているため実施しない
			//
			// if (IsDrawingFile(programName[0])) ...
			// if (programName.StartsWith("MPM")) ...
			//

			progOut = programName;
			return true;
		}

		/// <summary>
		/// /************************************************************************/
		/// /*									*/
		/// /*関数名：								*/
		/// /*	IsDrawingFile() : 図面ファイル判別				*/
		/// /*									*/
		/// /*機能：								*/
		/// /*	図面ファイルかどうかを判別する(case-insensitive)		*/
		/// /*									*/
		/// /*履歴：	1998.11.18 (YE) 山本英樹				*/
		/// /*									*/
		/// /************************************************************************/
		/// </summary>
		private static bool IsDrawingFile(char dwg)
		{
			switch (char.ToUpperInvariant(dwg))
			{
				case 'A':
				case 'H':
				case 'L':
				case 'S':
				case 'I':
				case LegacyConstants.PREFIX_NAMEC_DWGEX:
				case LegacyConstants.PREFIX_NAMEC_DWGP:
					return true;

				default:
					return false;
			}
		}
	}
}
	
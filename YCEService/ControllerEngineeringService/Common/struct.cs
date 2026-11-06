using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Yaskawa.ControllerEngineeringService.Common
{
	public class Structures
	{
		/// <summary>
		/// パケットヘッダ
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct PkHead
		{
			public ushort Id;	//パケットID
			public byte Level;	//パケット特権
			public byte Atr;	//パケット属性
			public byte Ver;	//バージョン番号
			public byte No;	//パケット番号

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public byte[] Dummy;	//予備

			public ushort Cnt;		//更新カウンター
			public ushort Step;		//ステップ
			public uint Size;		//パケットサイズ
		}
	}
}

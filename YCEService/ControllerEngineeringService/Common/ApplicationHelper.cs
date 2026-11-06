using Google.Protobuf;
using System.Text;
using System.IO;
using static Yaskawa.ControllerEngineeringService.Common.Structures;

namespace Yaskawa.ControllerEngineeringService.Common
{
	/// <summary>
	/// アプリケーションレイヤのヘルパークラス
	/// </summary>
	public class ApplicationHelper
	{
		/// <summary>
		/// パケットヘッダ付きのパケットデータバイトストリングの作成
		/// </summary>
		/// <param name="pkHead">パケットヘッダ情報</param>
		/// <param name="body">パケットデータ</param>
		/// <returns>パケットのバイトストリングデータ</returns>
		public static ByteString MakePacketByteString(Structures.PkHead pkHead, ByteString body)
		{
			const int headerSize = 16;

			// パケット全体のサイズを設定
			pkHead.Size = (uint)(headerSize + body.Length);

			// パケットヘッダーをByteStringに変換
			ByteString headerData = MakePacketHeaderByteString(pkHead);

			// Header + Body用の領域を作成
			byte[] packetData = new byte[headerData.Length + body.Length];

			// Headerをコピー
			headerData.CopyTo(packetData, 0);

			// Headerの後ろにBodyをコピー
			body.CopyTo(packetData, headerData.Length);

			return ByteString.CopyFrom(packetData);
		}

		/// <summary>
		/// パケットヘッダのバイトストリング作成
		/// </summary>
		/// <param name="pkHead">パケットヘッダ情報</param>
		/// <returns>パケットヘッダのバイトストリング</returns>
		public static ByteString MakePacketHeaderByteString(Structures.PkHead pkHead)
		{
			//パケットヘッダの作成
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);

			writer.Write(pkHead.Id);    //パケットID
			writer.Write(pkHead.Level); //パケット特権
			writer.Write(pkHead.Atr);   //パケット属性
			writer.Write(pkHead.Ver);   //バージョン番号
			writer.Write(pkHead.No);    //パケット番号

			writer.Write((byte)0);      //Dummy
			writer.Write((byte)0);      //Dummy

			writer.Write(pkHead.Cnt);   //更新カウンター
			writer.Write(pkHead.Step);  //ステップ
			writer.Write(pkHead.Size);  //パケットサイズ

			writer.Flush();

			return ByteString.CopyFrom(stream.ToArray());
		}

		/// <summary>
		/// パケットのヘッダーとボディ分割
		/// </summary>
		/// <param name="packet">パケットデータ</param>
		/// <param name="header">パケットヘッダ</param>
		/// <param name="body">ボディーデータ</param>
		/// <returns>
		///		true:分割成功
		///		false:分割失敗(パケットデータがnull、パケットサイズ不良)
		/// </returns>
		public static bool TryParsePacketByteString(
			ByteString packet,
			out PkHead header,
			out ByteString body)
		{
			const int headerSize = 16;

			// 戻り値を初期化
			header = default;
			body = ByteString.Empty;

			// パケットサイズチェック
			if (packet == null || packet.Length < headerSize)
			{
				return false;
			}

			byte[] packetData = packet.ToByteArray();

			using var stream = new MemoryStream(packetData, 0, headerSize);
			using var reader = new BinaryReader(stream);

			header = new PkHead
			{
				Id = reader.ReadUInt16(),
				Level = reader.ReadByte(),
				Atr = reader.ReadByte(),
				Ver = reader.ReadByte(),
				No = reader.ReadByte(),
				Dummy = reader.ReadBytes(2),
				Cnt = reader.ReadUInt16(),
				Step = reader.ReadUInt16(),
				Size = reader.ReadUInt32()
			};

			// Headerより後ろをBodyとして取得
			body = ByteString.CopyFrom(
				packetData,
				headerSize,
				packetData.Length - headerSize);

			return true;
		}
	}
}

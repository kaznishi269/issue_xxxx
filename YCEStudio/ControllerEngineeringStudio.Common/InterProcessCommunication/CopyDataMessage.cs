using ControllerEngineeringStudio.Common.Resources;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace ControllerEngineeringStudio.Common.InterProcessCommunication
{
    /// <summary>
    /// WM_COPYDATAの定義
    /// </summary>
    public static class CopyDataMessage
    {
        /// <summary>MFCのBOOLのTRUE</summary>
        public static readonly int BoolTrue = 1;
        /// <summary>MFCのBOOLのFALSE</summary>
        public static readonly int BoolFalse = 0;

        /// <summary>
        /// booleanをBOOLに変換
        /// </summary>
        /// <param name="value">変換元の値</param>
        /// <returns>変換後の値</returns>
        public static int ConvertToBool(bool value)
        {
            return value ? BoolTrue : BoolFalse;
        }

        #region Windows MappingFile
        /// <summary>
        /// マッピングファイル名の取得
        /// </summary>
        /// <param name="processId">プロセスId</param>
        /// <param name="messageId">メッセージId</param>
        /// <returns>マッピングファイル名</returns>
        private static string GetMappedFileName(uint processId, int messageId)
        {
            return $"{CommonStrings.AppName}[{processId}]_{messageId}";
        }

        /// <summary>
        /// マッピングファイルからデータを読み込む
        /// </summary>
        /// <typeparam name="TData">データ型</typeparam>
        /// <param name="processId">プロセスID</param>
        /// <param name="messageId">メッセージID</param>
        /// <returns>読み込んだデータ</returns>
        public static TData ReadMappedFile<TData>(uint processId, int messageId) where TData : struct
        {
            using var mappedFile = MemoryMappedFile.OpenExisting(GetMappedFileName(processId, messageId));
            using var accessor = mappedFile.CreateViewAccessor();
            int size = Marshal.SizeOf<TData>();
            byte[] buffer = new byte[size];
            accessor.ReadArray(0, buffer, 0, size);

            TData result;
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(buffer, 0, ptr, size);
                result = Marshal.PtrToStructure<TData>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return result;
        }

        /// <summary>
        /// マッピングファイルにデータを書き込む
        /// </summary>
        /// <param name="processId">プロセスID</param>
        /// <param name="messageId">メッセージID</param>
        /// <param name="data">書き込むデータ</param>
        public static void WriteMappedFile<TData>(uint processId, int messageId, TData data) where TData : struct
        {
            using var mappedFile = MemoryMappedFile.OpenExisting(GetMappedFileName(processId, messageId));
            using var accessor = mappedFile.CreateViewAccessor();
            int size = Marshal.SizeOf<TData>();
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(data, ptr, false);
                byte[] buffer = new byte[size];
                Marshal.Copy(ptr, buffer, 0, size);
                accessor.WriteArray(0, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        #endregion

        #region Windows Events
        /// <summary>
        /// イベント名の取得
        /// </summary>
        /// <param name="processId">プロセスId</param>
        /// <param name="messageId">メッセージId</param>
        /// <returns>イベント名</returns>
        private static string GetEventName(uint processId, int messageId)
        {
            return $"{CommonStrings.AppName}[{processId}]_{messageId}_Event";
        }

        /// <summary>
        /// イベントセット
        /// </summary>
        /// <param name="processId">プロセスId</param>
        /// <param name="messageId">メッセージId</param>
        public static void SetEvent(uint processId, int messageId)
        {
            using var eventHandle = EventWaitHandle.OpenExisting(GetEventName(processId, messageId));
            eventHandle.Set();
        }
        #endregion
    }

    #region WM_COPYDATA Message
    /// <summary>
    /// CopyDataメッセージのメッセージID
    /// COPYDATASTRUCTのdwDataで使用する
    /// </summary>
    public enum CopyDataMessageId
    {
        IsReady = 501,
        CanClose = 502,
        OpenDocument = 503,
        IsOpenedDocument = 504,
    }

    /// <summary>
    /// CanCloseMessageData
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CanCloseMessageData
    {
        /// <summary>
        /// 可能/不可(BOOL)
        /// </summary>
        public int canClose;
    }

    /// <summary>
    /// OpenDocumentMessageData
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OpenDocumentMessageData
    {
        /// <summary>
        /// ファイル名
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
        public string fileName;

        /// <summary>
        /// 成功/失敗(BOOL)
        /// </summary>
        public int success;
    }

    /// <summary>
    /// IsOpenedDocumentMessageData
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct IsOpenedDocumentMessageData
    {
        /// <summary>
        /// ファイル名
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
        public string fileName;

        /// <summary>
        /// オープン中/オープンしていない(BOOL)
        /// </summary>
        public int opened;
    }
    #endregion
}

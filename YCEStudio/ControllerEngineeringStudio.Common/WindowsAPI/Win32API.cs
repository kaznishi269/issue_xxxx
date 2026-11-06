using System.Runtime.InteropServices;

namespace ControllerEngineeringStudio.Common.Win32
{
    /// <summary>
    /// Win32API定義
    /// </summary>
    public static class Win32API
    {
        /// <summary>Windowスタイル</summary>
        public const int GWL_STYLE = -16;

        /// <summary>最小化ボタン</summary>
        public const long WS_MINIMIZEBOX = 0x00020000;

        /// <summary>最大化ボタン</summary>
        public const long WS_MAXIMIZEBOX = 0x00010000;

        /// <summary>
        /// Window属性取得
        /// </summary>
        /// <param name="hWnd">ウィンドウハンドル</param>
        /// <param name="nIndex">何を取得するか</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Window属性設定
        /// </summary>
        /// <param name="hWnd">ウィンドウハンドル</param>
        /// <param name="nIndex">何を設定するか</param>
        /// <param name="dwNewLong">設定する値</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        /// <summary>WM_COPYDATA※COPYDATASTRUCTを送信する際に設定する値</summary>
        public const int WM_COPYDATA = 0x004A;

        /// <summary>
        /// メッセージ送信(Send)
        /// </summary>
        /// <param name="hWnd">ウィンドウハンドル</param>
        /// <param name="Msg">送信されるメッセージ</param>
        /// <param name="wParam">追加のメッセージ固有情報</param>
        /// <param name="lParam">追加のメッセージ固有情報</param>
        /// <returns></returns>
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);
    }

    /// <summary>
    /// COPYDATASTRUCT
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
        /// <summary>
        /// 受信アプリケーションに渡されるデータの型。 受信側アプリケーションは、有効な型を定義します。
        /// </summary>
        public IntPtr dwData;

        /// <summary>
        /// lpData メンバーが指すデータのサイズ (バイト単位)。
        /// </summary>
        public int cbData;

        /// <summary>
        /// 受信アプリケーションに渡されるデータ。 このメンバーは NULL にすることができます。
        /// </summary>
        public IntPtr lpData;
    }
}

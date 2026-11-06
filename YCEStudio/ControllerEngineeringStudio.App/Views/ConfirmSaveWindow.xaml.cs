using ControllerEngineeringStudio.Common.Win32;
using System.Windows;
using System.Windows.Interop;

namespace ControllerEngineeringStudio.App.Views
{
    /// <summary>
    /// ConfirmSaveWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfirmSaveWindow : Window
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ConfirmSaveWindow()
        {
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            var style = Win32API.GetWindowLongPtr(hwnd, Win32API.GWL_STYLE).ToInt64();
            // 最小化、最大化ボタンを削除
            style &= ~Win32API.WS_MINIMIZEBOX;
            style &= ~Win32API.WS_MAXIMIZEBOX;
            Win32API.SetWindowLongPtr(hwnd, Win32API.GWL_STYLE, new IntPtr(style));
        }
    }
}

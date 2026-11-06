using ControllerEngineeringStudio.Common.Resources;

namespace ControllerEngineeringStudio.Common.IO
{
    /// <summary>
    /// アプリケーションのパス
    /// </summary>
    public static class AppPath
    {
        /// <summary>
        /// AppDataのツールディレクトリ
        /// </summary>
        public static string AppDataDirectory =>
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                $@"YASKAWA\{CommonStrings.AppName}");

        /// <summary>
        /// ドッキング画面のレイアウトファイル
        /// </summary>
        public static string DockingLayoutFile =>
            Path.Combine(AppDataDirectory, "dockingLayout.xml");

        /// <summary>
        /// EngineeringServerの実行ファイル
        /// </summary>
        public static string EngineeringServerExeFile =>
            Path.Combine(AppContext.BaseDirectory, "Yaskawa.ControllerEngineeringService.exe");
    }
}

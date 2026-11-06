using System.Globalization;

namespace ControllerEngineeringStudio.Common.Startup
{
    /// <summary>
    /// 起動引数のキー
    /// </summary>
    public static class StartupArgsKey
    {
        /// <summary>プロジェクトファイルパス</summary>
        public const string ProjectFilePath = "ProjectFilePath";

        /// <summary>機種名</summary>
        public const string CPU = "CPU";

        /// <summary>ロケールID</summary>
        public const string Lang = "LANG";

        /// <summary>ロケールIDとカルチャの対応ディクショナリ</summary>
        public static IReadOnlyDictionary<string, CultureInfo> LangDic { get; } =
            new Dictionary<string, CultureInfo>
            {
                { "0411", new CultureInfo("ja-JP") },
                { "0409", new CultureInfo("en-US") },
                { "0804", new CultureInfo("zh-CN") },
            };

        /// <summary>プロセスId</summary>
        public const string ProcessId = "ProcessId";

        /// <summary>プロジェクトリンクモーションファクトリId</summary>
        public const string LinkedMotionFactoryId = "LINKEDMOTIONFACTORYID";

        /// <summary>モーションファクトリId</summary>
        public const string MotionFactoryId = "MOTIONFACTORYID";

        /// <summary>オンライン</summary>
        public const string Online = "ONLINE";

        /// <summary>最小化するか</summary>
        public const string IsMinimized = "IsMinimized";
    }
}

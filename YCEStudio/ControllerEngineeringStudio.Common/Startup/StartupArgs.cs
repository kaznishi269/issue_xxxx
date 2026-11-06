namespace ControllerEngineeringStudio.Common.Startup
{
    /// <summary>
    /// 起動引数情報
    /// </summary>
    public sealed record class StartupArgs
    {
        /// <summary>
        /// プロジェクトファイルパス
        /// </summary>
        public string ProjectFilePath { get; init; } = string.Empty;

        /// <summary>
        /// 機種名
        /// </summary>
        public string CPU { get; init; } = string.Empty;

        /// <summary>
        /// 言語
        /// </summary>
        public string Lang { get; init; } = string.Empty;

        /// <summary>
        /// プロセスId
        /// </summary>
        public int ProcessId { get; init; }

        /// <summary>
        /// プロジェクトリンクモーションファクトリId
        /// </summary>
        public int LinkedMotionFactoryId { get; init; }

        /// <summary>
        /// モーションファクトリId
        /// </summary>
        public int MotionFactoryId { get; init; }

        /// <summary>
        /// オンラインか？
        /// </summary>
        public bool IsOnline { get; init; }

        /// <summary>
        /// 最小化するか？
        /// </summary>
        public bool IsMinimized { get; init; } = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="args">起動引数</param>
        public StartupArgs(string[] args)
        {
            foreach (string arg in args)
            {
                if (!arg.StartsWith("/")
                    || !arg.Contains("="))
                {
                    // 引数のフォーマット不正のため、スキップ
                    continue;
                }

                // 「/key=value」のを=で2分割する
                var temp = arg[1..].Split('=', 2);
                var key = temp[0];
                var value = temp[1];
                if (key.Equals(StartupArgsKey.ProjectFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    ProjectFilePath = value;
                }
                else if (key.Equals(StartupArgsKey.CPU, StringComparison.OrdinalIgnoreCase))
                {
                    CPU = value;
                }
                else if (key.Equals(StartupArgsKey.Lang, StringComparison.OrdinalIgnoreCase))
                {
                    Lang = value;
                }
                else if (key.Equals(StartupArgsKey.ProcessId, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out var processId))
                    {
                        ProcessId = processId;
                    }
                }
                else if (key.Equals(StartupArgsKey.LinkedMotionFactoryId, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out var motionFactoryId))
                    {
                        LinkedMotionFactoryId = motionFactoryId;
                    }
                }
                else if (key.Equals(StartupArgsKey.MotionFactoryId, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out var motionFactoryId))
                    {
                        MotionFactoryId = motionFactoryId;
                    }
                }
                else if (key.Equals(StartupArgsKey.Online, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out var online))
                    {
                        IsOnline = online == 1;
                    }
                }
                else if (key.Equals(StartupArgsKey.IsMinimized, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out var isMinimized))
                    {
                        IsMinimized = isMinimized == 1;
                    }
                }
            }
        }
    }
}

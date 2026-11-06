using Telerik.Windows.Controls.ConversationalUI;

namespace ControllerEngineeringStudio.AiChatTool.Models
{
    /// <summary>
    /// チャットメッセージモデル
    /// </summary>
    public sealed record ChatMessageModel
    {
        /// <summary>
        /// テキスト
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Author
        /// </summary>
        public Author Author { get; set; } = null!;

        /// <summary>
        /// 作成日時
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// エラー有り？
        /// </summary>
        public bool IsError { get; set; }
    }
}

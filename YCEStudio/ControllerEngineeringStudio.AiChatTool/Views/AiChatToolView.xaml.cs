using System.Windows.Controls;
using Telerik.Windows.Controls.ConversationalUI;

namespace ControllerEngineeringStudio.AiChatTool.Views
{
    /// <summary>
    /// AiChatToolView.xaml の相互作用ロジック
    /// </summary>
    public partial class AiChatToolView : UserControl
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AiChatToolView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// RadChatメッセージ送信イベント
        /// </summary>
        /// <param name="sender">RadChat</param>
        /// <param name="e">メッセージ送信イベント引数</param>
        private void OnRadChatSendMessage(object sender, Telerik.Windows.Controls.ConversationalUI.SendMessageEventArgs e)
        {
            if (e.Message is not TextMessage message)
            {
                return;
            }
            // メッセージ内容がない場合は、送信しない
            if (string.IsNullOrEmpty(message.Text))
            {
                e.Handled = true;
            }
        }
    }
}

using ControllerEngineeringStudio.AiChatTool.Models;
using Telerik.Windows.Controls.ConversationalUI;

namespace ControllerEngineeringStudio.AiChatTool.Converters
{
    /// <summary>
    /// チャットメッセージコンバーター(RadChat用)
    /// </summary>
    internal sealed class ChatMessageConverter : IMessageConverter
    {
        /// <summary>
        /// ChatMessageModelをTextMessageに変換
        /// </summary>
        /// <param name="item">ChatMessageModel</param>
        /// <returns>TextMessage</returns>
        /// <exception cref="ArgumentException">ChatMessageModel以外の引数の場合、発生</exception>
        public MessageBase ConvertItem(object item)
        {
            if (item is not ChatMessageModel model)
            {
                throw new ArgumentException(
                    "ChatMessageModelが必要です。",
                    nameof(item));
            }

            return new TextMessage(
                model.Author,
                model.Text,
                model.CreationDate);
        }

        /// <summary>
        /// TextMessageをChatMessageModelに変換
        /// </summary>
        /// <param name="message">TextMessage</param>
        /// <returns>ChatMessageModel</returns>
        /// <exception cref="NotSupportedException">TextMessage以外の引数の場合、発生</exception>
        public object ConvertMessage(MessageBase message)
        {
            if (message is not TextMessage textMessage)
            {
                throw new NotSupportedException(
                    $"未対応のメッセージ形式です: {message.GetType().Name}");
            }

            return new ChatMessageModel
            {
                Author = textMessage.Author,
                Text = textMessage.Text,
                CreationDate = textMessage.CreationDate
            };
        }
    }
}

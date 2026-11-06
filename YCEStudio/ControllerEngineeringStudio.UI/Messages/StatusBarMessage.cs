namespace ControllerEngineeringStudio.UI.Messages
{
    /// <summary>
    /// ステータスバー更新メッセージ
    /// </summary>
    /// <param name="StatusText">テキスト</param>
    public record class StatusBarMessage(
        string StatusText);
}

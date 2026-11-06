namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// ドッキング画面の状態変更メッセージ
    /// </summary>
    /// <param name="DockingId">ドッキングID</param>
    /// <param name="Key">キー</param>
    /// <param name="IsHidden">非表示か</param>
    public record class DockingStateChangedMessage(
        DockingId DockingId,
        string Key,
        bool IsHidden);
}

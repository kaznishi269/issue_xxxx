namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// ドッキングId
    /// ドッキングのレイアウトファイル(との互換性を維持するため、
    /// 既存値の変更、削除はしないこと。
    /// 削除する場合は、Reserveに変更すること。
    /// 新規追加する場合は、Reserveもしくは、末尾を使用すること。
    /// </summary>
    public enum DockingId
    {
        SampleTool,
        SampleDocument,
        AiChatTool,
    }
}

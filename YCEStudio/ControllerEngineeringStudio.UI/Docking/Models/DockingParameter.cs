namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// ドッキングパラメーター
    /// </summary>
    /// <param name="DockingId">ドッキングID</param>
    /// <param name="Key">キー</param>
    public sealed record DockingParameter(
        DockingId DockingId,
        string Key);
}

namespace ControllerEngineeringStudio.App.Parameters
{
    /// <summary>
    /// 保存確認画面のパラメータ
    /// </summary>
    /// <param name="FileList">確認対象のファイル名</param>
    public record class ConfirmSaveWindowParameter(
        IReadOnlyCollection<string> FileList);
}

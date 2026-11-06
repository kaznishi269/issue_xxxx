namespace ControllerEngineeringStudio.ApplicationService.Models
{
    /// <summary>
    /// StartEngineeringParameter
    /// </summary>
    /// <param name="IsOnline">オンラインか</param>
    /// <param name="PortNo">ポートNo</param>
    /// <param name="IPAddress">IPアドレス</param>
    /// <param name="MotionFactoryId">モーションファクトリID</param>
    /// <param name="LegacyProgrammerProcessId">FWXIDEのプロセスID</param>
    public record class StartEngineeringParameter(
        bool IsOnline,
        int PortNo,
        string IPAddress,
        int MotionFactoryId,
        int LegacyProgrammerProcessId);
}

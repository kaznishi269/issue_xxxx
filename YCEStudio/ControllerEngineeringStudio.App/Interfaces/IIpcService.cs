using System.Windows;

namespace ControllerEngineeringStudio.App.Interfaces
{
    /// <summary>
    /// プロセス間通信サービス
    /// </summary>
    public interface IIpcService
    {
        /// <summary>
        /// プロセス間通信を行うウィンドウのアタッチ
        /// </summary>
        /// <param name="window">ウィンドウ</param>
        void Attach(Window window);

        /// <summary>
        /// デタッチ
        /// </summary>
        void Detach();
    }
}

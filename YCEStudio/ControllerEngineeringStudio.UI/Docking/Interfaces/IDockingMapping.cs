using ControllerEngineeringStudio.UI.Navigation;

namespace ControllerEngineeringStudio.UI.Docking
{
    internal interface IDockingMapping : IViewMapping
    {
        /// <summary>
        /// ドッキングId
        /// </summary>
        DockingId DockingId { get; }
    }
}

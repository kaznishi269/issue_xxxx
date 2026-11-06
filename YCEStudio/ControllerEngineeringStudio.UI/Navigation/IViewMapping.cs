namespace ControllerEngineeringStudio.UI.Navigation
{
    /// <summary>
    /// ViewMappingインターフェース
    /// </summary>
    internal interface IViewMapping
    {
        /// <summary>
        /// ViewModel種別
        /// </summary>
        Type ViewModelType { get; }

        /// <summary>
        /// View種別
        /// </summary>
        Type ViewType { get; }
    }
}

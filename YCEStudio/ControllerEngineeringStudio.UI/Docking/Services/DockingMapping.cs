namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// DockingMapping
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel</typeparam>
    /// <typeparam name="TView">View</typeparam>
    internal sealed class DockingMapping<TViewModel, TView> : IDockingMapping
    {
        #region Properties
        /// <inheritdoc/>
        public DockingId DockingId { get; }

        /// <inheritdoc/>
        public Type ViewModelType => typeof(TViewModel);

        /// <inheritdoc/>
        public Type ViewType => typeof(TView);
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">DockingId</param>
        public DockingMapping(DockingId id)
        {
            DockingId = id;
        }
    }
}

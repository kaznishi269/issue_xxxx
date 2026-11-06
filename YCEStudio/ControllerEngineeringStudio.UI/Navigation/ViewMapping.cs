namespace ControllerEngineeringStudio.UI.Navigation
{
    /// <summary>
    /// ViewMapping
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel</typeparam>
    /// <typeparam name="TView">View</typeparam>
    internal sealed class ViewMapping<TViewModel, TView> : IViewMapping
    {
        /// <inheritdoc/>
        public Type ViewModelType => typeof(TViewModel);

        /// <inheritdoc/>
        public Type ViewType => typeof(TView);
    }
}

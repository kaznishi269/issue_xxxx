using CommunityToolkit.Mvvm.ComponentModel;
using ControllerEngineeringStudio.ApplicationService.Interfaces;
using ControllerEngineeringStudio.UI.Docking;

namespace ControllerEngineeringStudio.SampleDocument.ViewModels
{
    public partial class SampleDocumentViewModel : DocumentViewModelBase
    {
        private readonly IStProgramService _stProgramService;

        [ObservableProperty]
        private string _text = string.Empty;

        partial void OnTextChanged(string value)
        {
            IsDirty = true;
        }

        public override DockingId DockingId => DockingId.SampleDocument;

        public SampleDocumentViewModel(IStProgramService stProgramService)
        {
            _stProgramService = stProgramService;
        }

        public override async Task<bool> InitializeAsync(DockingParameter parameter)
        {
            if (!await base.InitializeAsync(parameter))
            {
                return false;
            }
            _defaultHeader = Key;

            // EngineeringServerなしでも画面確認したいので、、失敗しても続行する
            if (await _stProgramService.StartEngineeringAsync(Key))
            {
                var result = await _stProgramService.GetPropertyAsync();
                if (result.Success && !string.IsNullOrEmpty(result.Result))
                {
                    _defaultHeader += $" : {result.Result}";
                }
            }

            Header = _defaultHeader;

            return true;
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await _stProgramService.EndEngineeringAsync();
        }
    }
}

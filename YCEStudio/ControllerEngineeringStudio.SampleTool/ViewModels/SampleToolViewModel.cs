using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControllerEngineeringStudio.ApplicationService.Interfaces;
using ControllerEngineeringStudio.UI.Docking;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ControllerEngineeringStudio.SampleTool.ViewModels
{
    public partial class SampleToolViewModel : ToolViewModelBase
    {
        private readonly IRegisterAccessService _registerAccessService;
        private readonly IVariableService _variableService;

        /// <inheritdoc/>
        public override DockingId DockingId => DockingId.SampleTool;

        [ObservableProperty]
        private bool _isBusy = true;

        [ObservableProperty]
        private string _busyContent = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "入力してください")]
        [RegularExpression(@"^(?:[SMG][WLFQD]\d+|[MG]B(?:[0-9A-F]|[0-9]+[0-9A-F]))$", ErrorMessage = "M,Gレジスタのみ入力可")]
        private string _registerName = string.Empty;

        partial void OnRegisterNameChanged(string value)
        {
            var upperValue = value.ToUpper();
            if (upperValue != value)
            {
                RegisterName = upperValue;
            }
        }

        [ObservableProperty]
        private string _registerValue = "-";

        [ObservableProperty]
        private string _findVariableName = string.Empty;

        [ObservableProperty]
        private string _findResultRegisterName = string.Empty;
        
        [ObservableProperty]
        private string _findRegisterName = string.Empty;
        
        [ObservableProperty]
        private string _findResultVariableName = string.Empty;

        [RelayCommand]
        private async Task Find()
        {
            FindResultRegisterName = string.Empty;
            FindResultVariableName = string.Empty;
            try
            {
                var findSymbolRes = await _variableService.FindSymbolAsync(FindVariableName, string.Empty);
                FindResultRegisterName = findSymbolRes.Result ?? string.Empty;

                var findRegisterRes = await _variableService.FindRegisterAsync(FindRegisterName, string.Empty);
                FindResultVariableName = findRegisterRes.Result ?? string.Empty;
            }
            catch
            {
            }
        }

        private CancellationTokenSource _cancellationTokenSource = new();
        private Task? _monitorTask;

        public SampleToolViewModel(IRegisterAccessService registerAccessService, IVariableService variableService)
        {
            _registerAccessService = registerAccessService;
            _variableService = variableService;

            // EngineeringServerでSW3固定になっているので、UIもとりあえず固定
            RegisterName = "SW00003";
        }

        /// <inheritdoc/>
        public override async Task<bool> InitializeAsync(DockingParameter parameter)
        {
            await base.InitializeAsync(parameter);
            _defaultHeader = Key;
            Header = _defaultHeader;

            IsBusy = true;
            BusyContent = "Loading...";
            _ = LoadAsync();
            return true;
        }

        private async Task LoadAsync()
        {
            if (await _registerAccessService.StartEngineeringAsync())
            {
                _monitorTask = MonitorTask(_cancellationTokenSource.Token);
            }

            await _variableService.StartEngineeringAsync();

            IsBusy = false;
        }

        public override async ValueTask DisposeAsync()
        {
            IsBusy = true;
            BusyContent = "Closing...";
            await base.DisposeAsync();

            try
            {
                _cancellationTokenSource.Cancel();
                if (_monitorTask != null)
                {
                    await _monitorTask;
                }
                await _registerAccessService.EndEngineeringAsync();
                await _variableService.EndEngineeringAsync();
            }
            catch (Exception)
            {
                // 終了処理なので、例外無視
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _monitorTask = null;
            }
        }

        private async Task MonitorTask(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    if (HasErrors
                        || string.IsNullOrEmpty(RegisterName))
                    {
                        RegisterValue = "-";
                        continue;
                    }

                    var result = await _registerAccessService.ReadRegisterAsync(RegisterName, cancellationToken);
                    if (result.Success)
                    {
                        RegisterValue = result.Result ?? string.Empty;
                    }
                    else
                    {
                        RegisterValue = "取得失敗";
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControllerEngineeringStudio.UI.Dialog;
using System.ComponentModel.DataAnnotations;

namespace ControllerEngineeringStudio.SampleDocument.ViewModels
{
    public partial class NewSampleDocumentDialogViewModel : DialogViewModelBase<string>
    {
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessageResourceType = typeof(SampleDocument.Resources.Strings),
            ErrorMessageResourceName = nameof(SampleDocument.Resources.Strings.FileNameRequiredError))]
        private string _name = string.Empty;

        [RelayCommand(CanExecute = nameof(CanOK))]
        private void OK()
        {
            Result = Name;
            Close(true);
        }

        private bool CanOK()
        {
            return !HasErrors;
        }

        [RelayCommand]
        private void Cancel()
        {
            Close(false);
        }

        public override bool Initialize(object? parameter)
        {
            if (parameter is string name)
            {
                Name = name;
            }

            return true;
        }

        public NewSampleDocumentDialogViewModel()
        {
            ErrorsChanged += (_, _) =>
            {
                OKCommand.NotifyCanExecuteChanged();
            };
        }
    }
}

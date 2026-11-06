using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControllerEngineeringStudio.App.Constants;
using ControllerEngineeringStudio.App.Parameters;
using ControllerEngineeringStudio.UI.Dialog;

namespace ControllerEngineeringStudio.App.ViewModels
{
    /// <summary>
    /// 閉じる確認画面のViewModel
    /// </summary>
    public partial class ConfirmSaveWindowViewModel : DialogViewModelBase<ConfirmSaveResult>
    {
        /// <summary>未保存ファイル名一覧</summary>
        [ObservableProperty]
        private IReadOnlyList<string> _fileList = new List<string>();

        #region Commands
        /// <summary>
        /// 保存
        /// </summary>
        [RelayCommand]
        private void Save()
        {
            Result = ConfirmSaveResult.Save;
            Close(true);
        }

        /// <summary>
        /// 保存しない
        /// </summary>
        [RelayCommand]
        private void NotSave()
        {
            Result = ConfirmSaveResult.NotSave;
            Close(true);
        }

        /// <summary>
        /// キャンセル
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            Result = ConfirmSaveResult.Cancel;
            Close(true);
        }
        #endregion

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="parameter">ダイアログ初期化用パラメータ</param>
        public override bool Initialize(object? parameter)
        {
            if (parameter is not ConfirmSaveWindowParameter param)
            {
                throw new InvalidOperationException("パラメータ不正");
            }

            FileList = param.FileList.ToList();

            return true;
        }
    }
}

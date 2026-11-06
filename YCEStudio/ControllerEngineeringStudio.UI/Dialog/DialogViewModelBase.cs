using CommunityToolkit.Mvvm.ComponentModel;

namespace ControllerEngineeringStudio.UI.Dialog
{
    /// <summary>
    /// ダイアログViewModel
    /// </summary>
    public abstract class DialogViewModelBase<TResult> : ObservableValidator
    {
        /// <summary>
        /// 結果
        /// </summary>
        public TResult? Result { get; protected set; }

        /// <summary>
        /// View側へクローズ要求
        /// </summary>
        public event Action<bool>? RequestClose;

        /// <summary>
        /// クローズ
        /// </summary>
        /// <param name="isResult">ShowWindowの結果</param>
        protected void Close(bool isResult)
        {
            RequestClose?.Invoke(isResult);
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="parameter">ダイアログ初期化用パラメータ</param>
        /// <returns>true:成功、false:失敗</returns>
        public virtual bool Initialize(object? parameter)
        {
            return true;
        }
    }
}

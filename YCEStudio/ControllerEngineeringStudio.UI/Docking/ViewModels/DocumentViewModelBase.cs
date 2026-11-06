using CommunityToolkit.Mvvm.ComponentModel;

namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// ドキュメントウィンドウViewModel
    /// </summary>
    public abstract partial class DocumentViewModelBase : DockingViewModelBase
    {
        /// <summary>
        /// 編集状態か？
        /// </summary>
        [ObservableProperty]
        private bool _isDirty = false;

        /// <summary>
        /// IsDirty変更後
        /// </summary>
        /// <param name="value">変更後の値</param>
        partial void OnIsDirtyChanged(bool value)
        {
            if (value)
            {
                // 未保存変更有りの場合、ヘッダー*を付ける
                Header = $"* {_defaultHeader}";
            }
            else
            {
                Header = _defaultHeader;
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <returns>true:保存成功、false:保存失敗</returns>
        public virtual Task<bool> SaveAsync()
        {
            if (!IsDirty)
            {
                // 編集状態でないため、保存不要
                return Task.FromResult(true);
            }

            return Task.FromResult(true);
        }
    }
}

using System.Windows;

namespace ControllerEngineeringStudio.UI.Dialog
{
    /// <summary>
    /// ダイアログサービスインターフェース
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// メッセージボックス表示
        /// </summary>
        /// <param name="messageBoxText">メッセージ内容</param>
        /// <param name="caption">キャプション</param>
        /// <param name="button">ボタン</param>
        /// <param name="icon">アイコン</param>
        /// <param name="defaultResult">デフォルト</param>
        /// <returns></returns>
        MessageBoxResult ShowMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult = MessageBoxResult.None);

        // TODO:余力があれば、ボタン数やボタンの文字列を指定できる基本的なメッセージ画面を作成する

        /// <summary>
        /// ダイアログ表示
        /// </summary>
        /// <typeparam name="TViewModel">ダイアログのViewModel</typeparam>
        /// <typeparam name="TResult">ダイアログ結果</typeparam>
        /// <param name="parameter">ダイアログに渡すパラメータ</param>
        /// <returns>ダイアログ結果</returns>
        TResult? ShowDialog<TViewModel, TResult>(object? parameter = null) where TViewModel : DialogViewModelBase<TResult>;
    }
}

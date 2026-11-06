using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using ControllerEngineeringStudio.App.ViewModels;
using ControllerEngineeringStudio.UI.Messages;
using System.ComponentModel;
using System.Windows;
using Telerik.Windows.Controls;

namespace ControllerEngineeringStudio.App.Behaviors
{
    /// <summary>
    /// RadMenuItemBehavior
    /// </summary>
    public static class RadMenuItemBehavior
    {
        /// <summary>メッセンジャー</summary>
        private static IMessenger? _messenger;
        /// <summary>
        /// メッセンジャー
        /// </summary>
        private static IMessenger Messenger => _messenger ??= Ioc.Default.GetRequiredService<IMessenger>();

        /// <summary>RadMenuItemのHighLightedプロパティ</summary>
        private static readonly DependencyPropertyDescriptor HighlightDescriptor =
                DependencyPropertyDescriptor.FromProperty(
                    RadMenuItem.IsHighlightedProperty,
                    typeof(RadMenuItem));

        #region IsNotifyStatusBar
        /// <summary>
        /// ステータスバー通知プロパティ
        /// </summary>
        public static readonly DependencyProperty IsNotifyStatusBarProperty =
            DependencyProperty.RegisterAttached(
                "IsNotifyStatusBar",
                typeof(bool),
                typeof(RadMenuItemBehavior),
                new PropertyMetadata(false, OnIsNotifyStatusBarChanged));

        /// <summary>
        /// ステータスバー通知プロパティの取得
        /// </summary>
        /// <param name="obj">オブジェクト</param>
        /// <returns>ステータスバー通知プロパティ</returns>
        public static bool GetIsNotifyStatusBar(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsNotifyStatusBarProperty);
        }

        /// <summary>
        /// ステータスバー通知プロパティの設定
        /// </summary>
        /// <param name="obj">オブジェクト</param>
        /// <param name="value">true:ステータスバー通知する、false:しない</param>
        public static void SetIsNotifyStatusBar(DependencyObject obj, bool value)
        {
            obj.SetValue(IsNotifyStatusBarProperty, value);
        }

        /// <summary>
        /// ステータスバー通知プロパティ変更イベント
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void OnIsNotifyStatusBarChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is not RadMenuItem item)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                // イベント追加
                item.Loaded -= OnLoaded;
                item.Unloaded -= OnUnloaded;
                item.Loaded += OnLoaded;
                item.Unloaded += OnUnloaded;
            }
            else
            {
                // イベント解除
                HighlightDescriptor.RemoveValueChanged(item, OnHighlightedChanged);
                item.Loaded -= OnLoaded;
                item.Unloaded -= OnUnloaded;
            }
        }

        /// <summary>
        /// Loadedイベント
        /// </summary>
        /// <param name="sender">RadMenuItem</param>
        /// <param name="e">イベント引数</param>
        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            // IsHighlightプロパティ変更イベント追加
            if (sender is not RadMenuItem item)
            {
                return;
            }
            HighlightDescriptor.RemoveValueChanged(item, OnHighlightedChanged);
            HighlightDescriptor.AddValueChanged(item, OnHighlightedChanged);
        }

        /// <summary>
        /// Unloadedイベント
        /// </summary>
        /// <param name="sender">RadMenuItem</param>
        /// <param name="e">イベント引数</param>
        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // IsHighlightプロパティ変更イベント削除
            if (sender is not RadMenuItem item)
            {
                return;
            }
            HighlightDescriptor.RemoveValueChanged(item, OnHighlightedChanged);
        }

        /// <summary>
        /// RadMenuItemのIsHighlighted変更後イベント
        /// </summary>
        /// <param name="sender">RadMenuItem</param>
        /// <param name="e">イベント引数</param>
        private static void OnHighlightedChanged(object? sender, EventArgs e)
        {
            if (sender is not RadMenuItem item)
            {
                return;
            }

            if (item.DataContext is not MenuItemViewModel vm)
            {
                return;
            }

            if (item.IsHighlighted)
            {
                // RadMenuItemがハイライトされた場合、ステータスバーにメニューの説明を表示する
                Messenger.Send(new StatusBarMessage(vm.Description));
            }
            else
            {
                // RadMenuItemがハイライトされていない場合、ステータスバーをリセットする
                Messenger.Send(new StatusBarMessage(string.Empty));
            }
        }
        #endregion
    }
}

using System.Windows;

namespace ControllerEngineeringStudio.SampleDocument.Views
{
    /// <summary>
    /// NewSampleDocumentDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class NewSampleDocumentDialog : Window
    {
        public NewSampleDocumentDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _fileNameTextBox.Focus();
        }
    }
}

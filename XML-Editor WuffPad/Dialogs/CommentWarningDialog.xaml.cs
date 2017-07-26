using System.Windows;

namespace XML_Editor_WuffPad
{
    /// <summary>
    /// Interaktionslogik für CommentWarningDialog.xaml
    /// </summary>
    public partial class CommentWarningDialog : Window
    {
        public CommentWarningDialog()
        {
            InitializeComponent();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public bool DoNotShowAgain
        {
            get
            {
                return checkBoxDontShowAgain.IsChecked == true;
            }
        }
    }
}

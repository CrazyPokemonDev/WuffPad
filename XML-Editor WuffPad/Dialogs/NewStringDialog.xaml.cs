using System;
using System.Windows;
using System.Windows.Input;

namespace XML_Editor_WuffPad
{
    /// <summary>
    /// Interaktionslogik für NewStringDialog.xaml
    /// </summary>
    public partial class NewStringDialog : Window
    {
        public NewStringDialog(string defaultKey)
        {
            InitializeComponent();
            textBoxKey.Text = defaultKey;
        }

        private void okayButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            textBoxKey.SelectAll();
            textBoxKey.Focus();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public string Key
        {
            get
            {
                return textBoxKey.Text;
            }
        }

        private void textBoxKey_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) DialogResult = true;
        }
    }
}

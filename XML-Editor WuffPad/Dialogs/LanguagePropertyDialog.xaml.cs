using System;
using System.Windows;

namespace XML_Editor_WuffPad
{
    /// <summary>
    /// Interaktionslogik für LanguagePropertyDialog.xaml
    /// </summary>
    public partial class LanguagePropertyDialog : Window
    {
        private bool canCancel;
        private string __base;
        private string __name;
        private string __owner;
        private string __variant;
        private string __code;
        private string __isDefault;
        public LanguagePropertyDialog(bool firstTime = false, string name = "", string owner = "", string _base = "", string variant = "", string code = "", string isDefault = "")
        {
            InitializeComponent();
            canCancel = !firstTime;
            __base = _base;
            __name = name;
            __owner = owner;
            __variant = variant;
            __code = code;
            __isDefault = isDefault;
        }

        #region Fields
        public string LanguageName
        {
            get
            {
                return textBoxName.Text;
            }
        }

        public string LanguageOwner
        {
            get
            {
                return textBoxOwner.Text;
            }
        }

        public string LanguageBase
        {
            get
            {
                return textBoxBase.Text;
            }
        }

        public string LanguageVariant
        {
            get
            {
                return textBoxVariant.Text;
            }
        }

        public string LanguageCode
        {
            get
            {
                return textBoxCode.Text;
            }
        }

        public string LanguageIsDefault
        {
            get
            {
                return textBoxDefault.Text;
            }
        }
        #endregion

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (canCancel) DialogResult = false;
            else MessageBox.Show("You can't cancel now!");
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            execOkay();
        }

        private void execOkay()
        {
            if (textBoxBase.Text == "" || textBoxName.Text == "" || textBoxVariant.Text == "")
            {
                MessageBox.Show("Please fill all required boxes!");
                return;
            }
            canCancel = true;
            DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            textBoxName.Focus();
            textBoxBase.Text = __base;
            textBoxName.Text = __name;
            textBoxOwner.Text = __owner;
            textBoxVariant.Text = __variant;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!canCancel)
            {
                MessageBox.Show("You can't cancel now!");
                e.Cancel = true;
            }
        }
    }
}

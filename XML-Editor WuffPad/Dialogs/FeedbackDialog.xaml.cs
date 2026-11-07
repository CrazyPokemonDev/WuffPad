using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace XML_Editor_WuffPad
{
    /// <summary>
    /// Interaktionslogik für FeedbackDialog.xaml
    /// </summary>
    public partial class FeedbackDialog : Window
    {
        private const string issueTrackerUrl = "https://github.com/CrazyPokemonDev/WuffPad/issues/new";
        public FeedbackDialog()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // The feedback script the dialog used to call lived on the WuffPad
            // server, which is gone. GitHub's issue form is prefilled instead.
            string url = issueTrackerUrl
                + "?title=" + Uri.EscapeDataString("Feedback from " + usernameBox.Text)
                + "&body=" + Uri.EscapeDataString(inputBox.Text + "\n\nFrom: " + usernameBox.Text);
            try
            {
                Process.Start(url);
                DialogResult = true;
            }
            catch
            {
                MessageBox.Show("Could not open the issue tracker.\n"
                    + "Please report the problem at " + issueTrackerUrl);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using Telegram.Bot;

namespace XML_Editor_WuffPad
{
    /// <summary>
    /// Interaktionslogik für FeedbackDialog.xaml
    /// </summary>
    public partial class FeedbackDialog : Window
    {
        private const string feedbackUrl = "http://88.198.66.60/sendFeedback.php?";
        public FeedbackDialog()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            HttpClient client = new HttpClient();
            client.GetAsync(feedbackUrl + "message=" + WebUtility.HtmlEncode(inputBox.Text + "\nFrom: " + usernameBox.Text));
            MessageBox.Show("Message sent!");
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

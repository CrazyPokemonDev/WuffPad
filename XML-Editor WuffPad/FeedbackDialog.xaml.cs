using System;
using System.Collections.Generic;
using System.Linq;
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
        private string token;
        private const long developerID = 267376056;
        public FeedbackDialog(string botToken)
        {
            InitializeComponent();
            token = botToken;
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            TelegramBotClient client = new TelegramBotClient(token);
            Task t = client.SendTextMessageAsync(developerID, inputBox.Text + "\nFrom: " + usernameBox.Text);
            t.Wait();
            MessageBox.Show("Message sent!");
            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

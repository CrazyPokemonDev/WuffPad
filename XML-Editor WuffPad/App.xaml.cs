using System.IO;
using System.Windows;

namespace XML_Editor_WuffPad
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 1)
            {
                MainWindow m = new XML_Editor_WuffPad.MainWindow(e.Args[0]);
                m.Show();
            }
            else
            {
                MainWindow m = new XML_Editor_WuffPad.MainWindow();
                m.Show();
            }
        }
    }
}

using System.IO;
using System.Windows;
using XML_Editor_WuffPad.Core;

namespace XML_Editor_WuffPad
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Command line switch used by the build and by tools/smoke-test.ps1 to
            // verify that a fresh copy can work without the (dead) update server.
            if (SelfTest.IsRequested(e.Args))
            {
                Shutdown(SelfTest.Run(e.Args));
                return;
            }
            if (e.Args.Length == 1 && File.Exists(e.Args[0]))
            {
                // Opening a file from Explorer used to create and show an empty
                // main window in addition to the one with the file in it.
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

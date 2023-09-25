using System.Windows;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            CoreLib.Initialize();

            var wnd = new MainWindow(e.Args);
            wnd.Show();
        }
    }
}

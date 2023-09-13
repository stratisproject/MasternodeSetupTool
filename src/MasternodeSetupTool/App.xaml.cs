using System.Collections.Generic;
using System.Windows;
using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Stratis.Bitcoin.Utilities.JsonConverters;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            FlurlHttp.Configure(settings => {
                var jsonSettings = new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter>()
                    {
                        new DateTimeToUnixTimeConverter(),
                        new IsoDateTimeConverter(),
                    }
                };

                settings.JsonSerializer = new NewtonsoftJsonSerializer(jsonSettings);
            });


            var wnd = new MainWindow(e.Args);
            wnd.Show();
        }
    }
}

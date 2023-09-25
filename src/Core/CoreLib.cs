using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Stratis.Bitcoin.Utilities.JsonConverters;

namespace MasternodeSetupTool
{
    public static class CoreLib
    {
        public static void Initialize()
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
        }
    }
}
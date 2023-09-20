using MasternodeSetupTool;
using Newtonsoft.Json;

class Program
{
    static void Main(string[] args)
    {
        Configuration configuration = TryGetConfigurationFromParams(args) ?? new Configuration();

        var stateHandler = new AutomatedStateHandler(configuration);

        var stateMachine = new StateMachine(configuration.networkType, stateHandler);

        Task.Run(async () =>
        {
            switch (configuration.flowType)
            {
                case FlowType.RunNode:
                    stateMachine.OnRunNode();
                    break;
                case FlowType.SetupNode:
                    stateMachine.OnSetupNode();
                    break;
            }

            while (true)
            {
                await stateMachine.TickAsync();
                await Task.Delay(1000);
            }
        }).Wait();
    }

    private static Configuration? TryGetConfigurationFromParams(string[] args)
    {
        if (args.Length != 0)
        {
            string? configArg = args.FirstOrDefault(a => a.Contains("-config"));
            if (configArg != null)
            {
                string[] parts = configArg.Split('=');
                if (parts.Length == 2)
                {
                    string? filePath = parts[1];
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        try
                        {
                            string jsonConfig = File.ReadAllText(filePath);
                            Console.WriteLine($"Using configuration from {filePath}");
                            Console.WriteLine(jsonConfig);
                            return JsonConvert.DeserializeObject<Configuration>(jsonConfig);
                        } catch
                        {

                        }
                    }
                }
            }
        }

        return null;
    }

}
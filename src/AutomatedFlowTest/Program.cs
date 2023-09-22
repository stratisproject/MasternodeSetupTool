using AutomatedFlowTest;
using MasternodeSetupTool;
using Newtonsoft.Json;

class Program
{
    static void Main(string[] args)
    {
        Configuration configuration = TryGetConfigurationFromParams(args) ?? new Configuration();

        IStateHandler stateHandler = new AutomatedStateHandler(configuration, BuildLogger(configuration));
        IStateHolder stateHolder = new DefaultStateHolder(repeatOnEndState: false);

        var stateMachine = new StateMachine(
            networkType: configuration.networkType, 
            stateHandler: stateHandler,
            stateHolder: stateHolder);

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
                if (stateHolder.CurrentState == StateMachine.State.End)
                {
                    return;
                }

                await stateMachine.TickAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(200));
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

    private static AutomatedStateHandler.ILogger BuildLogger(Configuration configuration)
    {
        List<AutomatedStateHandler.ILogger> loggers = new List<AutomatedStateHandler.ILogger>();

        if (configuration != null)
        {
            if (configuration.writeConsoleLog)
            {
                loggers.Add(new ConsoleLogger());
            }

            if (!string.IsNullOrEmpty(configuration.logFilePath))
            {
                loggers.Add(new FileLogger(configuration.logFilePath));
            }
        }

        return new CompositeLogger(loggers);
    }
}
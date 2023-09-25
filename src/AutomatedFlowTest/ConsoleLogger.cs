namespace AutomatedFlowTest
{
    public class ConsoleLogger : AutomatedStateHandler.ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}

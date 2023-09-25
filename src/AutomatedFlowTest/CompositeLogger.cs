namespace AutomatedFlowTest
{
    public class CompositeLogger: AutomatedStateHandler.ILogger
    {
        private IEnumerable<AutomatedStateHandler.ILogger> Loggers;

        public CompositeLogger(IEnumerable<AutomatedStateHandler.ILogger> loggers)
        {
            this.Loggers = loggers;
        }

        public void Log(string message)
        {
            foreach (AutomatedStateHandler.ILogger logger in this.Loggers)
            {
                logger.Log(message);
            }
        }
    }
}

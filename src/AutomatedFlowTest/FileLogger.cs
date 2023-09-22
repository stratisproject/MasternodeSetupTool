namespace AutomatedFlowTest
{
    public class FileLogger : AutomatedStateHandler.ILogger
    {
        private readonly string FilePath;

        public FileLogger(string filePath)
        {
            this.FilePath = filePath;

            // Test if we can access the file or directory
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(this.FilePath));  // Ensure directory exists
                File.AppendAllText(this.FilePath, "");  // Try accessing the file
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize the logger with file path: {this.FilePath}.", ex);
            }
        }

        public void Log(string message)
        {
            try
            {
                File.AppendAllText(this.FilePath, message + "\n");
            }
            catch
            {
            }
        }
    }
}

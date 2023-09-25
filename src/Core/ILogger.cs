using System;

namespace MasternodeSetupTool
{
    public interface ILogger
    {
        void Info(string message, string? updateTag = null);

        void Error(string message);
        void Error(Exception exception);
        void Error(string message, Exception exception);
    }
}

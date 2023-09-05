using System.Windows;

namespace MasternodeSetupTool
{
    public static class DebugVisibility
    {
        public static Visibility DebugOnly
        {
#if DEBUG
            get { return Visibility.Visible; }
#else
            get { return Visibility.Collapsed; }
#endif
        }

        public static Visibility ReleaseOnly
        {
#if DEBUG
            get { return Visibility.Collapsed; }
#else
            get { return Visibility.Visible; }
#endif
        }
    }
}

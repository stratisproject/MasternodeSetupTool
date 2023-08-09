using System.Windows;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for CreateRestoreUseExisting.xaml
    /// </summary>
    public partial class CreateRestoreUseExisting : Window
    {
        public enum ButtonChoice
        {
            CreateWallet,
            RestoreWallet,
            UseExistingWallet
        }

        private ButtonChoice buttonChoice;

        public CreateRestoreUseExisting()
        {
            InitializeComponent();
        }

        public ButtonChoice Choice
        {
            get
            {
                return this.buttonChoice;
            }
        }

        private void CreateWalletButton_Click(object sender, RoutedEventArgs e)
        {
            this.buttonChoice = ButtonChoice.CreateWallet;
        }

        private void RestoreWalletButton_Click(object sender, RoutedEventArgs e)
        {
            this.buttonChoice = ButtonChoice.RestoreWallet;
        }

        private void UseExistingWalletButton_Click(object sender, RoutedEventArgs e)
        {
            this.buttonChoice = ButtonChoice.UseExistingWallet;
        }
    }
}

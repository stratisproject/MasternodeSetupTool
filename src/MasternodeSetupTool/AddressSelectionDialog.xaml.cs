using System.Collections.Generic;
using System.Windows;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for WalletSelectionDialog.xaml
    /// </summary>
    public partial class AddressSelectionDialog : Window
    {
        public AddressSelectionDialog(List<AddressItem> addresses)
        {
            InitializeComponent();

            this.ItemsList.ItemsSource = addresses;
        }

        public string? SelectedAddress { get; private set; } = null;

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedAddress = (AddressItem)this.ItemsList.SelectedItem;

            if (selectedAddress != null)
            {
                this.SelectedAddress = selectedAddress.Address;
                Close();
            }
        }
    }
}

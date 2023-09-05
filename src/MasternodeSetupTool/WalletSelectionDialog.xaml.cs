using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for WalletSelectionDialog.xaml
    /// </summary>
    public partial class WalletSelectionDialog : Window
    {
        public WalletSelectionDialog(List<WalletItem> wallets)
        {
            InitializeComponent();

            this.ItemsList.ItemsSource = wallets;
        }

        public string? SelectedWalletName { get; private set; } = null;

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedWallet = (WalletItem)this.ItemsList.SelectedItem;

            if (selectedWallet != null)
            {
                this.SelectedWalletName = selectedWallet.Name;
                Close();
            }
        }
    }
}

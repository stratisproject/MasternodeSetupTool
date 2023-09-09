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
using Humanizer;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for ShowAddressDialog.xaml
    /// </summary>
    public partial class ShowAddressDialog : Window
    {
        private string Address;

        public ShowAddressDialog(NodeType nodeType, string address)
        {
            InitializeComponent();

            this.Address = address;
            this.AddressText.Text = address;

            string walletType;
            if (nodeType == NodeType.MainChain)
            {
                walletType = "Collateral";
            }
            else
            {
                walletType = "Mining";
            }

            this.Title = $"{walletType} address";
            this.LabelText.Text = $"Your {walletType.ToLower()} address is:";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.Address);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

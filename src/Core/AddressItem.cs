using NBitcoin;

namespace MasternodeSetupTool
{
    public class AddressItem
    {
        public string Address { get; set; }

        public AddressItem(string address, Money balance)
        {
            this.Address = address;
            this.Balance = balance;
        }

        public Money Balance { get; set; }

        public string BalanceFormatted
        {
            get
            {
                return $"{this.Balance.ToString(fplus: false, trimExcessZero: true)}";
            }
        }
    }
}

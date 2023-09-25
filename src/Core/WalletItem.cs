using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;

namespace MasternodeSetupTool
{
    public class WalletItem
    {
        public string Name { get; set; }

        public WalletItem(string name, Money balance)
        {
            this.Name = name;
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

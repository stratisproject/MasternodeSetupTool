using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.Bitcoin.Features.Wallet.Models;

namespace MasternodeSetupTool
{
    public interface IRegistrationService
    {
        Network MainchainNetwork { get; }
        NetworkType NetworkType { get; }
        string PubKey { get; }
        string RootDataDir { get; }
        Network SidechainNetwork { get; }

        Task<WalletBuildTransactionModel> BuildTransactionAsync(int apiPort, string walletName, string walletPassword, string accountName, List<RecipientModel> recipients, string? opReturnData = null, string? opReturnAmount = null, string feeType = "low", string? changeAddress = null, bool allowUnconfirmed = false);
        Task<bool> CallJoinFederationRequestAsync(WalletCredentials collateralCredentials, WalletCredentials miningCredentials);
        bool CheckFederationKeyExists();
        Task<bool> CheckIsFederationMemberAsync();
        Task<bool> CheckWalletBalanceAsync(int apiPort, string walletName, int amountToCheck);
        Task<bool> CheckWalletBalanceWithConfirmationsAsync(int apiPort, string walletName, int amountToCheck, int requiredConfirmations);
        Task<bool?> CheckWalletPasswordAsync(int apiPort, string walletName, string walletPassword);
        string CreateFederationKey();
        void DeleteFederationKey();
        Task<bool> EnsureBlockstoreIsSyncedAsync(NodeType nodeType, int apiPort);
        Task<bool> EnsureMainChainNodeAddressIndexerIsSyncedAsync();
        Task<bool> EnsureNodeIsInitializedAsync(NodeType nodeType, int apiPort);
        Task<bool> EnsureNodeIsSyncedAsync(NodeType nodeType, int apiPort);
        Task<bool> FindWalletByNameAsync(int apiPort, string walletName);
        Task<string?> GetFirstWalletAddressAsync(int apiPort, string walletName);
        Task<List<AddressItem>?> GetWalletAddressesAsync(string walletName, int apiPort);
        Task<WalletItem> GetWalletBalanceAsync(string walletName, int apiPort);
        Task<List<WalletItem>?> GetWalletsWithBalanceAsync(int apiPort);
        Task<bool> IsWalletSyncedAsync(int apiPort, string walletName);
        void LaunchBrowser(string url);
        Task<bool> MonitorJoinFederationRequestAsync();
        Task<bool> PerformCrossChainTransferAsync(int apiPort, string walletName, string walletPassword, string amount, string cirrusAddress, string changeAddress);
        Task<bool> RestoreWalletAsync(int apiPort, NodeType nodeType, string walletName, string mnemonic, string passphrase, string password, bool createNewWallet);
        Task<bool> ResyncWalletAsync(int apiPort, string walletName);
        string SendTransaction(int apiPort, string hex);
        Task ShutdownNodeAsync(NodeType nodeType, int apiPort);
        Task<bool> StartMasterNodeDashboardAsync();
        Task<bool> StartNodeAsync(NodeType nodeType, int apiPort, bool reindex = false);
        Task<int> WalletSyncProgressAsync(int apiPort, string walletName);
    }
}
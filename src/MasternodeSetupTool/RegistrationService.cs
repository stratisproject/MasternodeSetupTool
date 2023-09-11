using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Flurl;
using Flurl.Http;
using ICSharpCode.Decompiler.CSharp.Syntax;
using LiteDB;
using NBitcoin;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Controllers.Models;
using Stratis.Bitcoin.Features.BlockStore.Models;
using Stratis.Bitcoin.Features.PoA;
using Stratis.Bitcoin.Features.PoA.Models;
using Stratis.Bitcoin.Features.Wallet.Models;
using Stratis.Bitcoin.Networks;
using Stratis.Features.PoA.Voting;
using Stratis.Sidechains.Networks;

namespace MasternodeSetupTool
{
    public sealed class RegistrationService
    {
        public const int CollateralRequirement = 100_000;
        public const int FeeRequirement = 500;
        public const int DashboardPort = 5000;

        public const int CollateralMinConfirmations = 500;

        private NetworkType networkType;
        private Network mainchainNetwork;
        private Network sidechainNetwork;
        private ILogger logger;

        private string rootDataDir;
        private string pubKey;

        public RegistrationService(NetworkType networkType, ILogger logger)
        {
            this.rootDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StratisNode");
            this.networkType = networkType;
            this.logger = logger;

            if (this.networkType == NetworkType.Mainnet)
            {
                this.mainchainNetwork = new StraxMain();
                this.sidechainNetwork = new CirrusMain();
            }

            if (this.networkType == NetworkType.Testnet)
            {
                this.mainchainNetwork = new StraxTest();
                this.sidechainNetwork = new CirrusTest();
            }

            if (this.networkType == NetworkType.Regtest)
            {
                this.mainchainNetwork = new StraxRegTest();
                this.sidechainNetwork = new CirrusRegTest();
            }
        }

        public NetworkType NetworkType
        {
            get { return this.networkType; }
        }

        public string RootDataDir
        {
            get { return this.rootDataDir; }
        }

        public Network MainchainNetwork
        {
            get { return this.mainchainNetwork; }
        }

        public Network SidechainNetwork
        {
            get { return this.sidechainNetwork; }
        }

        public string PubKey
        {
            get { return this.pubKey; }
        }

        private void Status(string message, string? updateTag = null)
        {
            this.logger.Info(message);
        }

        private void Error(string message)
        {
            this.logger.Error(message);
        }

        private void Error(Exception exception)
        {
            this.logger.Error(exception);
        }

        private void Error(string message, Exception exception)
        {
            this.logger.Error(message, exception);
        }

        public async Task<bool> StartNodeAsync(NodeType nodeType, int apiPort)
        {
            if (await CheckNodeIsRunningAsync(apiPort))
            {
                Status($"{nodeType} node is already running. We will shutdown and rerun it.");
                await ShutdownNodeAsync(nodeType, apiPort);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            var argumentBuilder = new StringBuilder();

            argumentBuilder.Append($"-{nodeType.ToString().ToLowerInvariant()} ");

            if (nodeType == NodeType.MainChain)
                argumentBuilder.Append("-addressindex=1 ");

            if (nodeType == NodeType.SideChain)
            {
                argumentBuilder.Append($"-counterchainapiport={this.mainchainNetwork.DefaultAPIPort} ");
                argumentBuilder.Append($"-signalruri=http://127.0.0.1 ");
            }

            if (this.networkType == NetworkType.Testnet)
            {               
                argumentBuilder.Append("-testnet ");
                // argumentBuilder.Append("-reindex ");
            }

            if (this.networkType == NetworkType.Regtest)
                argumentBuilder.Append("-regtest ");

            Status($"Starting the {nodeType} node on {this.networkType}. Start up arguments: {argumentBuilder}");

            var startInfo = new ProcessStartInfo
            {
                FileName = "Stratis.CirrusMinerD.exe",
                Arguments = argumentBuilder.ToString(),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized,
                WorkingDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CirrusMinerD"))
            };

            var process = Process.Start(startInfo);
            await Task.Delay(TimeSpan.FromSeconds(5));

            if (process != null && process.HasExited)
            {
                Error($"{nodeType} node process failed to start, exiting...");

                return false;
            }

            Status($"{nodeType} node started.");

            return true;
        }

        public async Task ShutdownNodeAsync(NodeType nodeType, int apiPort)
        {
            try
            {
                await $"http://localhost:{apiPort}/api"
                    .AppendPathSegment("node/shutdown")
                    .WithHeader("Content-Type", "application/json-patch+json")
                    .PostAsync();
            }
            catch (Exception e) 
            {
                Error($"Can not shutdown {nodeType}.");
                Error(e);
            }
        }

        private async Task<bool> CheckNodeIsRunningAsync(int apiPort)
        {
            try
            {
                StatusModel blockModel = await $"http://localhost:{apiPort}/api".AppendPathSegment("node/status").GetJsonAsync<StatusModel>();
                return blockModel != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EnsureNodeIsInitializedAsync(NodeType nodeType, int apiPort)
        {
            Status($"Waiting for the {nodeType} node to initialize...");

            bool initialized = false;

            // Call the node status API until the node initialization state is Initialized.
            CancellationToken cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
            do
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    Error($"{nodeType} node failed to initialized in 60 seconds...");
                    break;
                }

                try
                {
                    StatusModel blockModel = await $"http://localhost:{apiPort}/api".AppendPathSegment("node/status").GetJsonAsync<StatusModel>();
                    if (blockModel.State == FullNodeState.Started.ToString())
                    {
                        initialized = true;
                        Status($"{nodeType} node initialized.");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Error(ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(3));
            } while (true);

            return initialized;
        }

        public async Task<bool> EnsureNodeIsSyncedAsync(NodeType nodeType, int apiPort)
        {
            string logTag = Guid.NewGuid().ToString();

            Status($"Waiting for the {nodeType} node to sync with the network...", updateTag: logTag);

            bool result;

            // Call the node status API until the node initialization state is Initialized.
            do
            {
                StatusModel blockModel = await $"http://localhost:{apiPort}/api".AppendPathSegment("node/status").GetJsonAsync<StatusModel>();
                if (blockModel.InIbd.HasValue && !blockModel.InIbd.Value)
                {
                    Status($"{nodeType} node is synced at height {blockModel.ConsensusHeight}.", updateTag: logTag);
                    result = true;
                    break;
                }

                Status($"{nodeType} node syncing, current height {blockModel.ConsensusHeight}...", updateTag: logTag);
                await Task.Delay(TimeSpan.FromSeconds(3));
            } while (true);

            return result;
        }

        public async Task<bool> EnsureMainChainNodeAddressIndexerIsSyncedAsync()
        {
            const string logTag = "MainChainNodeAddressIndexerStatus";

            Status("Waiting for the main chain node to sync it's address indexer...", updateTag: logTag);

            bool result;

            do
            {
                StatusModel blockModel = await $"http://localhost:{this.mainchainNetwork.DefaultAPIPort}/api".AppendPathSegment("node/status").GetJsonAsync<StatusModel>();
                AddressIndexerTipModel addressIndexerModel = await $"http://localhost:{this.mainchainNetwork.DefaultAPIPort}/api".AppendPathSegment("blockstore/addressindexertip").GetJsonAsync<AddressIndexerTipModel>();

                if (blockModel.ConsensusHeight != null)
                {
                    int targetHeight = (int)(blockModel.ConsensusHeight - 50);

                    if (addressIndexerModel.TipHeight > targetHeight)
                    {
                        Status($"Main chain address indexer synced.", updateTag: logTag);
                        result = true;
                        break;
                    } 
                    else
                    {
                        int height = addressIndexerModel.TipHeight ?? 0;
                        int progress = Math.Clamp((int)(100.0 * height / targetHeight), 0, 100);
                        Status($"Main chain node address indexer is syncing, progress: {progress}%, current height: {addressIndexerModel.TipHeight}...", updateTag: logTag);
                        await Task.Delay(TimeSpan.FromSeconds(3));
                    }
                }

                Status($"Main chain node address indexer is syncing, current height {addressIndexerModel.TipHeight}...", updateTag: logTag);
                await Task.Delay(TimeSpan.FromSeconds(3));
            } while (true);

            return result;
        }

        public async Task<bool> FindWalletByNameAsync(int apiPort, string walletName)
        {
            try
            {
                WalletInfoModel walletInfo = await $"http://localhost:{apiPort}/api"
                    .AppendPathSegment("wallet/list-wallets")
                    .GetJsonAsync<WalletInfoModel>().ConfigureAwait(false);

                return walletInfo.WalletNames.Contains(walletName);
            }
            catch (Exception ex)
            {
                Error($"ERROR: An exception occurred trying to find a wallet with a name '{walletName}'.", ex);
            }

            return false;
        }

        public async Task<string?> GetFirstWalletAddressAsync(int apiPort, string walletName)
        {
            try
            {
                var walletBalanceRequest = new WalletBalanceRequest
                {
                    WalletName = walletName,
                    AccountName = "account 0",
                    IncludeBalanceByAddress = true
                };

                WalletBalanceModel walletBalanceModel = await $"http://localhost:{apiPort}/api"
                    .AppendPathSegment("wallet/balance")
                    .SetQueryParams(walletBalanceRequest)
                    .GetJsonAsync<WalletBalanceModel>().ConfigureAwait(false);

                return walletBalanceModel.AccountsBalances.FirstOrDefault()?.Addresses.FirstOrDefault()?.Address;
            }
            catch (Exception ex)
            {
                Error($"ERROR: An exception occurred trying to get the first wallet address for wallet '{walletName}'.", ex);
            }

            return null;
        }

        public async Task<bool> CheckWalletBalanceAsync(int apiPort, string walletName, int amountToCheck)
        {
            try
            {
                var walletBalanceRequest = new WalletBalanceRequest() { WalletName = walletName };
                WalletBalanceModel walletBalanceModel = await $"http://localhost:{apiPort}/api"
                    .AppendPathSegment("wallet/balance")
                    .SetQueryParams(walletBalanceRequest)
                    .GetJsonAsync<WalletBalanceModel>().ConfigureAwait(false);

                if (walletBalanceModel.AccountsBalances[0].SpendableAmount / 100000000 >= amountToCheck)
                {
                    Status($"SUCCESS: The wallet '{walletName}' contains the required amount of {amountToCheck}.");
                    return true;
                }

                Error($"ERROR: The wallet '{walletName}' does not contain the required amount of {amountToCheck}.");
            }
            catch (Exception ex)
            {
                Error($"ERROR: An exception occurred trying to check the wallet balance.", ex);
            }

            return false;
        }

        public async Task<bool> CheckWalletBalanceWithConfirmationsAsync(int apiPort, string walletName, int amountToCheck, int requiredConfirmations)
        {
            try
            {
                string? address = await GetFirstWalletAddressAsync(apiPort, walletName);
                if (address == null)
                {
                    Error($"Can't find first address of the wallet {walletName}");
                    return false;
                }

                AddressBalancesResult addressBalancesResult = await $"http://localhost:{apiPort}/api"
                    .AppendPathSegment("BlockStore/getaddressesbalances")
                    .SetQueryParams(new Dictionary<string, object> {
                        { "addresses", address },
                        { "minConfirmations", requiredConfirmations } })
                    .GetJsonAsync<AddressBalancesResult>();

                if (addressBalancesResult.Balances[0].Balance / 100000000 >= amountToCheck)
                {
                    Status($"SUCCESS: The wallet '{walletName}' contains the required amount of {amountToCheck}.");
                    return true;
                }

                Error($"ERROR: The wallet '{walletName}' does not contain the required amount of {amountToCheck}.");
            }
            catch (Exception ex)
            {
                Error($"ERROR: An exception occurred trying to check the wallet balance.", ex);
            }

            return false;
        }

        public async Task<bool> RestoreWalletAsync(int apiPort, NodeType nodeType, string walletName, string mnemonic, string passphrase, string password, bool createNewWallet)
        {
            string action = createNewWallet ? "create" : "restore";
            Status($"You have chosen to {action} your {nodeType} wallet.");

            var walletRecoveryRequest = new WalletRecoveryRequest()
            {
                CreationDate = DateTime.Now,
                Mnemonic = mnemonic,
                Name = walletName,
                Passphrase = passphrase,
                Password = password
            };

            try
            {
                await $"http://localhost:{apiPort}/api"
                    .AppendPathSegment("wallet/recover")
                    .PostJsonAsync(walletRecoveryRequest);
            }
            catch (FlurlHttpException httpException) when (httpException.StatusCode == 409)
            {
                throw new WalletCollisionException();
            }
            catch (Exception ex)
            {
                Error($"ERROR: An exception occurred trying to recover your {nodeType} wallet.", ex);
                return false;
            }

            WalletInfoModel walletInfoModel = await $"http://localhost:{apiPort}/api"
                .AppendPathSegment("wallet/list-wallets")
                .GetJsonAsync<WalletInfoModel>();

            if (walletInfoModel.WalletNames.Contains(walletName))
            {
                Status($"SUCCESS: {nodeType} wallet has been restored.");
            }
            else
            {
                Error($"ERROR: {nodeType} wallet failed to be restored, exiting the registration process.");
                return false;
            }

            return true;
        }

        public async Task<List<WalletItem>?> GetWalletsWithBalanceAsync(int apiPort)
        {
            try
            {
                WalletInfoModel walletInfoModel = await $"http://localhost:{apiPort}/api"
                    .AppendPathSegment("wallet/list-wallets")
                    .GetJsonAsync<WalletInfoModel>();

                IEnumerable<Task<WalletItem>> getBalanceTasks = walletInfoModel.WalletNames.Select(walletName => GetWalletBalanceAsync(walletName, apiPort));

                WalletItem[] balances = await Task.WhenAll(getBalanceTasks);

                return balances.ToList();
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool?> CheckWalletPasswordAsync(int apiPort, string walletName, string walletPassword)
        {
            try
            {
                WalletLoadRequest request = new WalletLoadRequest()
                {
                    Name = walletName,
                    Password = walletPassword
                };

                IFlurlResponse response = await $"http://localhost:{apiPort}/api"
                    .AppendPathSegment("wallet/load")
                    .PostJsonAsync(request);

                if (response != null)
                {
                    if (response.StatusCode == 200)
                    {
                        return true;
                    }
                    else if (response.StatusCode == 403)
                    {
                        return false;
                    }
                }
                return null;
            }
            catch (FlurlHttpException e) when (e.StatusCode == 403)
            {
                return false;
            }
            catch
            {
                return null;
            }
        }

        public async Task<WalletItem> GetWalletBalanceAsync(string walletName, int apiPort)
        {
            var walletBalanceRequest = new WalletBalanceRequest() { WalletName = walletName };

            WalletBalanceModel walletBalanceModel = await $"http://localhost:{apiPort}/api"
                .AppendPathSegment("wallet/balance")
                .SetQueryParams(walletBalanceRequest)
                .GetJsonAsync<WalletBalanceModel>().ConfigureAwait(false);

            return new WalletItem(name: walletName, balance: walletBalanceModel.AccountsBalances[0].SpendableAmount);
        }

        public class WalletCollisionException: Exception { }

        public async Task<bool> ResyncWalletAsync(int apiPort, string walletName)
        {
            try
            {
                Status($"Your wallet will now be resynced, please be patient...");
                var walletSyncRequest = new WalletSyncRequest()
                {
                    All = true,
                    WalletName = walletName
                };

                await $"http://localhost:{apiPort}/api"
                    .AppendPathSegment("wallet/sync-from-date")
                    .PostJsonAsync(walletSyncRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Error($"ERROR: An exception occurred trying to resync your wallet.", ex);
                return false;
            }

            return true;
        }

        public async Task<bool> IsWalletSyncedAsync(int apiPort, string walletName)
        {
            var walletNameRequest = new WalletName() { Name = walletName };
            WalletGeneralInfoModel walletInfoModel = await $"http://localhost:{apiPort}/api"
                .AppendPathSegment("wallet/general-info")
                .SetQueryParams(walletNameRequest)
                .GetJsonAsync<WalletGeneralInfoModel>().ConfigureAwait(false);

            return (walletInfoModel.ChainTip != null && walletInfoModel.LastBlockSyncedHeight != null) && walletInfoModel.ChainTip == walletInfoModel.LastBlockSyncedHeight;
        }

        public async Task<int> WalletSyncProgressAsync(int apiPort, string walletName)
        {
            var walletNameRequest = new WalletName() { Name = walletName };
            WalletGeneralInfoModel walletInfoModel = await $"http://localhost:{apiPort}/api"
                .AppendPathSegment("wallet/general-info")
                .SetQueryParams(walletNameRequest)
                .GetJsonAsync<WalletGeneralInfoModel>().ConfigureAwait(false);

            return walletInfoModel.ChainTip != null ? (int)((decimal)(walletInfoModel.LastBlockSyncedHeight ?? 0) / (decimal)walletInfoModel.ChainTip * 100.0m) : 0;
        }

        public void DeleteFederationKey()
        {
            string keyFilePath = Path.Combine(this.rootDataDir, this.sidechainNetwork.RootFolderName, this.sidechainNetwork.Name);

            File.Delete(Path.Combine(keyFilePath, KeyTool.KeyFileDefaultName));
        }

        public bool CheckFederationKeyExists()
        {
            string keyFilePath = Path.Combine(this.rootDataDir, this.sidechainNetwork.RootFolderName, this.sidechainNetwork.Name);

            return File.Exists(Path.Combine(keyFilePath, KeyTool.KeyFileDefaultName));
        }

        public string CreateFederationKey()
        {
            string keyFilePath = Path.Combine(this.rootDataDir, this.sidechainNetwork.RootFolderName, this.sidechainNetwork.Name);

            Status($"Your masternode private key will now be generated.");

            Directory.CreateDirectory(keyFilePath);

            // Generate keys for mining.
            var tool = new KeyTool(keyFilePath);

            Key key = tool.GeneratePrivateKey();

            string savePath = tool.GetPrivateKeySavePath();
            tool.SavePrivateKey(key);

            this.pubKey = Encoders.Hex.EncodeData(key.PubKey.ToBytes(false));

            return savePath;
        }

        public async Task<bool> PerformCrossChainTransferAsync(int apiPort, string walletName, string walletPassword, string amount, string cirrusAddress, string changeAddress)
        {
            string federationAddress;
            if (this.sidechainNetwork.NetworkType == NetworkType.Mainnet)
                federationAddress = "yU2jNwiac7XF8rQvSk2bgibmwsNLkkhsHV";
            else
                federationAddress = "tGWegFbA6e6QKZP7Pe3g16kFVXMghbSfY8";

            var recipients = new List<RecipientModel>() { new RecipientModel() { DestinationAddress = federationAddress, Amount = amount.ToString() } };

            try
            {
                WalletBuildTransactionModel builtTransaction = await BuildTransactionAsync(apiPort,
                    walletName,
                    walletPassword,
                    "account 0",
                    recipients,
                    opReturnData: cirrusAddress,
                    opReturnAmount: "0",
                    changeAddress: changeAddress).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(builtTransaction.Hex))
                {
                    Status($"Built cross-chain transaction: {builtTransaction.TransactionId}. Sending...");

                    SendTransaction(apiPort, builtTransaction.Hex);

                    Status($"Sent transaction: {builtTransaction.TransactionId}");

                    return true;
                }
            }
            catch (Exception ex)
            {
                Error("Error building cross-chain transfer transaction", ex);

                return false;
            }

            Error("Error building cross-chain transfer transaction");

            return false;
        }

        public async Task<WalletBuildTransactionModel> BuildTransactionAsync(int apiPort, string walletName, string walletPassword, string accountName, List<RecipientModel> recipients,
            string? opReturnData = null, string? opReturnAmount = null, string feeType = "low", string? changeAddress = null, bool allowUnconfirmed = false)
        {
            var result = await $"http://localhost:{apiPort}/api"
                .AppendPathSegment("wallet/build-transaction")
                .PostJsonAsync(new BuildTransactionRequest
                {
                    WalletName = walletName,
                    AccountName = accountName,
                    FeeType = feeType,
                    Password = walletPassword,
                    Recipients = recipients,
                    AllowUnconfirmed = allowUnconfirmed,
                    ShuffleOutputs = true,
                    OpReturnData = opReturnData,
                    OpReturnAmount = opReturnAmount,
                    SegwitChangeAddress = false,
                    ChangeAddress = changeAddress
                })
                .ReceiveBytes()
                .ConfigureAwait(false);

            WalletBuildTransactionModel buildTransactionModel = JsonConvert.DeserializeObject<WalletBuildTransactionModel>(Encoding.ASCII.GetString(result));

            return buildTransactionModel;
        }

        public string SendTransaction(int apiPort, string hex)
        {
            WalletSendTransactionModel sendActionResult = $"http://localhost:{apiPort}/api"
                .AppendPathSegment("wallet/send-transaction")
                .PostJsonAsync(new SendTransactionRequest
                {
                    Hex = hex
                })
                .ReceiveJson<WalletSendTransactionModel>().GetAwaiter().GetResult();

            return sendActionResult.TransactionId.ToString();
        }

        public async Task<bool> CallJoinFederationRequestAsync(string collateralAddress, string collateralWallet, string collateralPassword, string cirrusWalletName, string cirrusWalletPassword)
        {
            var request = new JoinFederationRequestModel()
            {
                CollateralAddress = collateralAddress,
                CollateralWalletName = collateralWallet,
                CollateralWalletPassword = collateralPassword,
                WalletAccount = "account 0",
                WalletName = cirrusWalletName,
                WalletPassword = cirrusWalletPassword
            };

            try
            {
                JoinFederationResponseModel response = await $"http://localhost:{this.sidechainNetwork.DefaultAPIPort}/api"
                    .AppendPathSegment("collateral/joinfederation")
                    .PostJsonAsync(request)
                    .ReceiveJson<JoinFederationResponseModel>()
                    .ConfigureAwait(false);

                if (response != null && !string.IsNullOrEmpty(response.MinerPublicKey)) 
                {
                    Status($"SUCCESS: The masternode request has now been submitted to the network");
                    return true;
                }
                else
                {
                    Error($"ERROR: Cannot join federation. Please try again.");
                    return false;
                }
            }
            catch (FlurlHttpException ex) when (ex.StatusCode == 400)
            {
                Error($"ERROR: An exception occurred trying to register your masternode.");
                Error($"Error message: {await ex.GetResponseStringAsync()}");
                return false;
            }
            catch (Exception ex)
            {
                Error($"ERROR: An exception occurred trying to register your masternode.", ex);
                return false;
            }
        }

        public async Task<bool> CheckIsFederationMemberAsync()
        {
            try
            {
                FederationMemberDetailedModel memberInfo = await $"http://localhost:{this.sidechainNetwork.DefaultAPIPort}/api"
                    .AppendPathSegment("federation/members/current")
                    .GetJsonAsync<FederationMemberDetailedModel>().ConfigureAwait(false);

                return memberInfo != null && memberInfo.CollateralAddress != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MonitorJoinFederationRequestAsync()
        {
            FederationMemberDetailedModel memberInfo = await $"http://localhost:{this.sidechainNetwork.DefaultAPIPort}/api"
                .AppendPathSegment("federation/members/current")
                .GetJsonAsync<FederationMemberDetailedModel>().ConfigureAwait(false);

            if (memberInfo == null)
            {
                Error("Unable to find federation member details");
                return false;
            }

            StatusModel blockModel = await $"http://localhost:{this.sidechainNetwork.DefaultAPIPort}/api"
                .AppendPathSegment("node/status")
                .GetJsonAsync<StatusModel>().ConfigureAwait(false);

            long? targetHeight = memberInfo.MemberWillStartMiningAtBlockHeight;
            if (targetHeight != null)
            {
                Status($"Expecting registration to complete at block height {memberInfo.MemberWillStartMiningAtBlockHeight}");
            }
            else
            {
                Status($"Waiting for registration to complete...");
            }

            return (blockModel.ConsensusHeight >= memberInfo.MemberWillStartMiningAtBlockHeight);
        }

        public async Task<bool> StartMasterNodeDashboardAsync()
        {
            var argumentBuilder = new StringBuilder();

            argumentBuilder.Append("Stratis.FederatedSidechains.AdminDashboard.exe ");
            argumentBuilder.Append("--nodetype 10K ");
            argumentBuilder.Append($"--mainchainport {this.MainchainNetwork.DefaultAPIPort} ");
            argumentBuilder.Append($"--sidechainport {this.SidechainNetwork.DefaultAPIPort} ");

            if (this.networkType == NetworkType.Mainnet)
            {
                argumentBuilder.Append("--env mainnet ");
                argumentBuilder.Append("--sdadaocontractaddress CbtYboKjnk7rhNbEFzn94UZikde36h6TCb ");
            }

            if (this.networkType == NetworkType.Testnet)
            {
                argumentBuilder.Append("--env testnet ");
                // TODO: Replace with correct address
                //argumentBuilder.Append("--sdadaocontractaddress CbtYboKjnk7rhNbEFzn94UZikde36h6TCb "); 
            }

            if (this.networkType == NetworkType.Regtest)
            {
                argumentBuilder.Append("--env regtest ");
                // TODO: Replace with correct address
                //argumentBuilder.Append("--sdadaocontractaddress CbtYboKjnk7rhNbEFzn94UZikde36h6TCb "); 
            }

            Status($"Starting the masternode dashboard on {this.networkType}. Start up arguments: {argumentBuilder}");
            
            string osSpecificCommand = "CMD.EXE";
            string osSpecificArguments = $"/K \"{argumentBuilder}\"";

            var startInfo = new ProcessStartInfo
            {
                Arguments = osSpecificArguments,
                FileName = osSpecificCommand,
                UseShellExecute = true,
                WorkingDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dashboard"))
            };

            var process = Process.Start(startInfo);
            await Task.Delay(TimeSpan.FromSeconds(5));

            if (process == null || process.HasExited)
            {
                Error($"Masternode dashboard process failed to start, exiting...");
                return false;
            }

            Status($"Masternode dashboard started.");

            return true;
        }

        public void LaunchBrowser(string url)
        {
            Process.Start("explorer", url);
        }

        private bool IsRunningOnWindows()
        {
            OperatingSystem os = Environment.OSVersion;
            PlatformID platform = os.Platform;

            return platform == PlatformID.Win32NT || platform == PlatformID.Win32Windows;
        }
    }

    public enum NodeType
    {
        MainChain,
        SideChain
    }
}

using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgBlockchainInCSharp
{
    class Program
    {

        static void Main(string[] args)
        {
            var network = Network.TestNet;
            var btcPrivateKey = "snipped";
            var btcAddress = "msdZUQorfYpqfboxF6UZX6WrHBa2Hr9nSf";
            //var transaction = "4343d73aece9dc725c52b085ff032c176c1014690fba609c29e5461ca0317b54"; // Input into testnet wallet
            var transaction = "8e3598bcb05ecde8dc7f42f3a51324b588814d861bc7241fea869d9de1b332c7"; // Output from testnet wallet

            //GetTransaction(network, btcPrivateKey, transaction, false);

            var message = "Nicolas Dorier Book Funding Address";
            var address = "1KF8kUVHK42XzgcmJF4Lxz4wcL5WDL97PB";
            var signature = "H1jiXPzun3rXi0N9v9R5fAWrfEae9WPmlL5DJBj1eTStSvpKdRR8Io6/uT9tGH/3OnzG6ym5yytuWoA9ahkC3dQ=";

            //VerifyMessage(network, message, signature, address);

            //GenerateKey(network, passphrase);

        }

        static List<string> CreateAddress(Network network)
        {
            List<string> btcAccount = new List<string>();

            var privateKey = new Key();
            var bitcoinPrivateKey = privateKey.GetBitcoinSecret(network);
            var address = bitcoinPrivateKey.GetAddress();

            Console.WriteLine(bitcoinPrivateKey);
            Console.WriteLine(address);

            Console.ReadKey();

            btcAccount.Add(bitcoinPrivateKey.ToString());
            btcAccount.Add(address.ToString());

            return btcAccount;
        }

        static void GetTransaction(Network network, string btcPrivateKey, string transId, bool spending)
        {
            var client = new QBitNinjaClient(network);
            var transactionId = uint256.Parse(transId);
            var transactionResponse = client.GetTransaction(transactionId).Result;
            var privateKey = new BitcoinSecret(btcPrivateKey);

            Console.WriteLine(transactionResponse.TransactionId);

            if (null != transactionResponse.Block)
                Console.WriteLine("Confirmations: " + transactionResponse.Block.Confirmations);
            else
                Console.WriteLine("Block is not yet set.");

            var message = "Asemco leaves their record on the blockchain.";
            var bytes = Encoding.UTF8.GetBytes(message);

            //foreach (var output in transactionResponse.Transaction.Outputs)
            //{
                
            //    Console.WriteLine(output.Value);
            //}

            if (spending)
            {

                var receivedCoins = transactionResponse.ReceivedCoins;
                OutPoint outPointToSpend = null;
                foreach (var coin in receivedCoins)
                {
                    if (coin.TxOut.ScriptPubKey == privateKey.ScriptPubKey)
                        outPointToSpend = coin.Outpoint;
                }

                if (outPointToSpend == null)
                    throw new Exception("TxOut doesn't contain our ScriptPubKey");
                Console.WriteLine("We want to spend {0}. outpoint:", outPointToSpend.N + 1);

                var transaction = new Transaction();
                transaction.Inputs.Add(new TxIn()
                {
                    PrevOut = outPointToSpend
                });

                var receiverAddress = BitcoinAddress.Create("2N8hwP1WmJrFF5QWABn38y63uYLhnJYJYTF", network);
                var outgoingAmount = new Money(0.0004m, MoneyUnit.BTC);
                var minerFee = new Money(0.00007m, MoneyUnit.BTC);

                var txInAmount = (Money)receivedCoins[(int)outPointToSpend.N].Amount;
                var changeAmount = txInAmount - outgoingAmount - minerFee;

                TxOut testBTCOut = new TxOut()
                {
                    Value = outgoingAmount,
                    ScriptPubKey = receiverAddress.ScriptPubKey
                };

                TxOut changeBackTxOut = new TxOut()
                {
                    Value = changeAmount,
                    ScriptPubKey = privateKey.ScriptPubKey
                };

                //var message = "Asemco leaves their record on the blockchain.";
                //var bytes = Encoding.UTF8.GetBytes(message);
                TxOut transMessage = new TxOut()
                {
                    Value = Money.Zero,
                    ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(bytes)
                };

                transaction.Outputs.Add(testBTCOut);
                transaction.Outputs.Add(changeBackTxOut);
                transaction.Outputs.Add(transMessage);
                transaction.Inputs[0].ScriptSig = privateKey.ScriptPubKey;
                transaction.Sign(privateKey, false);

                BroadcastResponse broadcastResponse = client.Broadcast(transaction).Result;

                if (!broadcastResponse.Success)
                {
                    Console.Error.WriteLine("ErrorCode: " + broadcastResponse.Error.ErrorCode);
                    Console.Error.WriteLine("Error message: " + broadcastResponse.Error.Reason);
                }
                else
                {
                    Console.WriteLine("Success!  You've hit the blockchain!  The transaction hash is below.");
                    Console.WriteLine(transaction.GetHash());
                }
            }
        }

        static void FromWhere(Network network, string transId)
        {

        }

        static void VerifyMessage(Network network, string message, string signature, string address)
        {
            var btcAddress = new BitcoinPubKeyAddress(address);
            bool confirmed = btcAddress.VerifyMessage(message, signature);

            Console.WriteLine("The message, " + message + ", sent by, " + address + ", belongs to them: " + confirmed);
        }

        static void GenerateKey(Network network, string passphrase)
        {
            var passphraseCode = new BitcoinPassphraseCode(passphrase, network, null);
            
            EncryptedKeyResult encryptedKeyResult = passphraseCode.GenerateEncryptedSecret();
            Console.WriteLine("Addy: " + encryptedKeyResult.GeneratedAddress);
            
        }
    }
}

using System;
using System.Threading;
using RestTest.Banking;

namespace restTest
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            //
            // For this simple program:
            // 1) Create a Bank and provide it with its specific characteristics
            // 2) Create a BankAccount, and associate it with the Bank
            // 3) Ask the Bank to get all of the transactions for this account, and to insert them into the local Account representation
            // 4) Ask the local Account representation to print out the DailyBalances that it contains
            //

            // Note that for the embedded configuration, I would want to pull this out into a config file or external storage
            IBank bank = new Bank( new BankDataProviderAndAccountUpdaterForMultiPages( "https://resttest.bench.co/transactions" ) ); 
            IBankAccount account = new BankAccount( bank, 0.0 );
            await bank.GetAllTransactionsAndUpdateAccount(account, CancellationToken.None);
            account.ForEachDailyBalance((dailyBalance) => Console.WriteLine(dailyBalance.ToString()));
        }
    }
}

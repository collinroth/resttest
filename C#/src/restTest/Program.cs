using System;
using System.Threading;
using RestTest.Banking;

namespace restTest
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            IBank bank = new Bank( new BankDataProviderAndAccountUpdaterForMultiPages( "https://resttest.bench.co/transactions" ) );
            IBankAccount account = new BankAccount( bank, 0.0 );
            await bank.GetAllTransactionsAndUpdateAccount(account, CancellationToken.None);
            account.ForEachDailyBalance((dailyBalance) => Console.WriteLine(dailyBalance.ToString()));
        }
    }
}

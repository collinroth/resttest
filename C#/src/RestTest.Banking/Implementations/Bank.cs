using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RestTest.Banking
{
    public class Bank : IBank
    {
        public Bank( IBankDataProviderAndAccountUpdater dataProvider )
        {
            this.DataProvider = dataProvider;
        }
        public async Task GetAllTransactionsAndUpdateAccount(IBankAccount forBankAccount, CancellationToken cancellationToken)
        {
            await DataProvider.GetAllTransactionsAndUpdateAccount(forBankAccount, cancellationToken);
        }
        public IBankDataProviderAndAccountUpdater DataProvider { get; }
    }
}

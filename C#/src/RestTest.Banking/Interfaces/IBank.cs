using System.Threading;
using System.Threading.Tasks;

namespace RestTest.Banking
{
    public interface IBank
    {
        Task GetAllTransactionsAndUpdateAccount(IBankAccount forBankAccount, CancellationToken cancellationToken);
        public IBankDataProviderAndAccountUpdater DataProvider { get; }
    }
}
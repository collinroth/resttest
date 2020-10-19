using System.Threading;
using System.Threading.Tasks;

namespace RestTest.Banking
{
    public interface IBankDataProviderAndAccountUpdater
    {
        Task GetAllTransactionsAndUpdateAccount(IBankAccount forBankAccount, CancellationToken cancellationToken);
    }
}
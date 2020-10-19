using System;
using System.Collections.Generic;

namespace RestTest.Banking
{
    public interface IBankAccountDailyBalances
    {
        double LastDailyEndingBalance { get; }
        IList<IDailyBalance> GetAllDailyBalances();
        void ForEach(Action<IDailyBalance> dailyBalanceAction);
        void InsertSingleNewTransaction(FinancialTransactionDto newTransaction);
        void InsertBatchOfNewTransactions(IList<FinancialTransactionDto> newTransactionBatch);
    }
}
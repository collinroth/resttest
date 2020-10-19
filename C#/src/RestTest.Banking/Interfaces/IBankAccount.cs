using System;
using System.Collections.Generic;

namespace RestTest.Banking
{
    public interface IBankAccount
    {
        IBank Bank { get; }
        double StartingBalance { get; }
        double CurrentBalance { get; }
        void InsertSingleNewTransaction(FinancialTransactionDto newTransaction);
        void InsertBatchOfNewTransactions(IList<FinancialTransactionDto> newTransactionBatch);
        public IList<IDailyBalance> GetAllDailyBalances();
        public void ForEachDailyBalance(Action<IDailyBalance> dailyBalanceAction);
    }
}

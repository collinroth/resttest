using System;
using System.Collections.Generic;
using System.Linq;

namespace RestTest.Banking
{
    public class BankAccount : IBankAccount
    {
        public double CurrentBalance => this._dailyBalances.LastDailyEndingBalance;
        public double StartingBalance { get; private set; }
        public IBank Bank { get; }
        private IBankAccountDailyBalances _dailyBalances;
        public void InsertSingleNewTransaction(FinancialTransactionDto newTransaction)
        {
            InsertBatchOfNewTransactions(new List<FinancialTransactionDto>() { newTransaction });
        }
        public void InsertBatchOfNewTransactions(IList<FinancialTransactionDto> newTransactionBatch)
        {
            _dailyBalances.InsertBatchOfNewTransactions(newTransactionBatch);
        }
        public IList<IDailyBalance> GetAllDailyBalances()
        {
            return this._dailyBalances.GetAllDailyBalances();
        }
        public void ForEachDailyBalance(Action<IDailyBalance> dailyBalanceAction)
        {
            this._dailyBalances.ForEach(dailyBalanceAction);
        }
        public BankAccount(IBank bank, double startingBalance, IBankAccountDailyBalances dailyBalances)
        {
            this.Bank = bank;
            this.StartingBalance = startingBalance;
            this._dailyBalances = dailyBalances;
        }
        public BankAccount(IBank bank, double startingBalance) : this(bank, startingBalance, new BankAccountDailyBalances(startingBalance))
        {
        }
        public BankAccount(IBank bank) : this(bank, 0.0)
        {
        }
    }
}

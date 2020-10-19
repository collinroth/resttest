using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RestTest.Banking
{
    public class BankAccountDailyBalances : IBankAccountDailyBalances
    {
        private SortedList<DateTime, IDailyBalance> _dailyBalances = new SortedList<DateTime, IDailyBalance>();
        private class singleDayTransactionTotalWrapper { public double sumOfDaysTransactions; };
        private object localLock = new object();
        public BankAccountDailyBalances(double startingBalance)
        {
            this.StartingBalance = startingBalance;
        }
        public double StartingBalance { get; private set; }
        public double LastDailyEndingBalance
        {
            get
            {
                lock (this.localLock)
                {
                    double endingBalance = 0.0;
                    if (this._dailyBalances.Count > 0)
                    {
                        endingBalance = this._dailyBalances.Last().Value.EndingBalance;
                    }
                    return endingBalance;
                }
            }
        }
        public void InsertSingleNewTransaction(FinancialTransactionDto newTransaction)
        {
            InsertBatchOfNewTransactions(new List<FinancialTransactionDto>() { newTransaction });
        }
        public void InsertBatchOfNewTransactions(IList<FinancialTransactionDto> newTransactionBatch)
        {
            if (!newTransactionBatch.Any())
            {
                return;
            }

            // Because we have a batch we want to be as efficient 
            // as possible.
            //
            // 1) Combine the transactions into a temporary dictionary with the day as the 
            //    lookup key, and the value as the total sum of batched transactions for this day.
            //    Note that this can be done outside of the thread lock, as it is completely independent.
            //
            var tempDateTransactionTotals = BuildDictionaryOfSumsForEachDay(newTransactionBatch);

            // Everything after this point requires a thread lock to ensure mutual exclusive access
            // to our data structures
            lock (this.localLock)
            {
                // Integrate these sums into our existing sorted list of daily balances.  We'll do this
                // in two additional steps:
                //
                // 2) Add the batched sum for each day into the DailyBalance.  Note that each individual
                //    DailyBalance will be correct, but the sequence of ending/starting/ending balances will 
                //    be out of sync after this update.
                (DateTime earliestDayTouched, double earliestDayTouchedEndingBalance) = IntegrateDaySumDictionaryIntoDailyBalances(tempDateTransactionTotals);

                // 3) So, let's now walk through our sorted list and make the appropriate 
                //    corrections to the start/end balances.  This needs to be done starting 
                //    with the earliest day that we touched in this batch, and moving forward to the
                //    end of our list.
                CorrectStartingAndEndingDailyBalances(earliestDayTouched, earliestDayTouchedEndingBalance);
            }
        }
        private Dictionary<DateTime, singleDayTransactionTotalWrapper> BuildDictionaryOfSumsForEachDay(IList<FinancialTransactionDto> newTransactionBatch)
        {
            // Create a temporary dictionary that captures the day as the key, and the value is the sum of 
            // all transactions from that day
            var tempDateTransactionTotals = new Dictionary<DateTime, singleDayTransactionTotalWrapper>();
            foreach (var transaction in newTransactionBatch)
            {
                DateTime transactionDateWithoutTime = transaction.Date.Date;  // Strip the date of a specific time
                if (tempDateTransactionTotals.TryGetValue(transactionDateWithoutTime, out var transactionTotal))
                {
                    transactionTotal.sumOfDaysTransactions += transaction.Amount;
                }
                else
                {
                    tempDateTransactionTotals.Add(transactionDateWithoutTime, new singleDayTransactionTotalWrapper { sumOfDaysTransactions = transaction.Amount });
                }
            }
            return tempDateTransactionTotals;
        }
        private (DateTime earliestDayTouched, double earliestDayTouchedEndingBalance) IntegrateDaySumDictionaryIntoDailyBalances(Dictionary<DateTime, singleDayTransactionTotalWrapper> tempDateTransactionTotals)
        {
            var earliestDayTouched = tempDateTransactionTotals.First().Key;
            double earliestDayTouchedEndingBalance = 0.0;
            foreach (var daySum in tempDateTransactionTotals)
            {
                if (_dailyBalances.TryGetValue(daySum.Key, out var dailyBalanceToBeAdjusted))
                {
                    dailyBalanceToBeAdjusted.AddToDailyTotalTransactionSum(daySum.Value.sumOfDaysTransactions); // Adjust an existing DailyBalance
                }
                else
                {
                    dailyBalanceToBeAdjusted = new DailyBalance(daySum.Key, 0.0, daySum.Value.sumOfDaysTransactions);
                    _dailyBalances.Add(daySum.Key, dailyBalanceToBeAdjusted); // Create a new DailyBalance
                }
                if (daySum.Key <= earliestDayTouched)
                {
                    earliestDayTouched = daySum.Key;
                    earliestDayTouchedEndingBalance = dailyBalanceToBeAdjusted.EndingBalance;
                }
            }
            return (earliestDayTouched, earliestDayTouchedEndingBalance);
        }
        private void CorrectStartingAndEndingDailyBalances(DateTime earliestDayTouched, double earliestDayTouchedEndingBalance)
        {
            (int correctionIndex, double prevEndingBalance) = CorrectEarliestTouchedStartingAndEndingBalance(earliestDayTouched, earliestDayTouchedEndingBalance);
            CorrectRemainingDaysStartingAndEndingBalances(correctionIndex, prevEndingBalance);
        }
        private (int correctionIndex, double prevEndingBalance) CorrectEarliestTouchedStartingAndEndingBalance(DateTime earliestDayTouched, double earliestDayTouchedEndingBalance)
        {
            double prevEndingBalance;
            var correctionIndex = this._dailyBalances.IndexOfKey(earliestDayTouched);
            IDailyBalance dailyBalanceToBeCorrected = this._dailyBalances.ElementAt(correctionIndex).Value;
            if (correctionIndex == 0)
            {
                prevEndingBalance = this.StartingBalance;
            }
            else
            {
                prevEndingBalance = this._dailyBalances.ElementAt(correctionIndex - 1).Value.EndingBalance;
            }
            dailyBalanceToBeCorrected.AdjustStartingBalance(prevEndingBalance);
            return (correctionIndex, dailyBalanceToBeCorrected.EndingBalance);
        }
        private void CorrectRemainingDaysStartingAndEndingBalances(int correctionIndex, double prevEndingBalance)
        {
            // Correct all days thereafter
            for (correctionIndex += 1;
                 correctionIndex < this._dailyBalances.Count;
                 correctionIndex += 1)
            {
                IDailyBalance dailyBalanceToBeCorrected = this._dailyBalances.ElementAt(correctionIndex).Value;
                dailyBalanceToBeCorrected.AdjustStartingBalance(prevEndingBalance);
                prevEndingBalance = dailyBalanceToBeCorrected.EndingBalance;
            }
        }
        public IList<IDailyBalance> GetAllDailyBalances()
        {
            lock (this.localLock)
            {
                return _dailyBalances.Values.ToArray();
            }
        }
        public void ForEach(Action<IDailyBalance> dailyBalanceAction)
        {
            lock (this.localLock)
            {
                foreach (var dailyBalance in _dailyBalances.Values)
                {
                    dailyBalanceAction(dailyBalance);
                }
            }
        }
    }
}

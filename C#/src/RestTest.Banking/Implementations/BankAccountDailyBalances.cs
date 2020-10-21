using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RestTest.Banking
{
    public class BankAccountDailyBalances : IBankAccountDailyBalances
    {
        private SortedList<DateTime, IDailyBalance> _dailyBalances = new SortedList<DateTime, IDailyBalance>();
        /// <summary>
        /// We need the following contained private class to contain our daily balance
        /// double.  This may initially look strange, because it's a class wrapping a single 
        /// double value - why not just use a double?
        /// 
        /// The answer is that this class allows us to more efficiently update the daily balance 
        /// double value when we have more transactions coming in for the given day.
        /// 
        /// Specifically, this value will be contained within a dictionary,
        /// and as such, I had two choices regarding performance upon updating
        /// an existing day:
        /// 
        ///     a) Allow for multiple probes into the dictionary:
        ///             - See if the day is in the dictionary, returning a double (first probe)
        ///             - If it is, then we need to add to the retrieved value
        ///               from the previous step and write the result back into the
        ///               dictionary (second probe)
        ///        If I was willing to take the 2-probes with "a", then I could simply
        ///        store a dictionary of (DateTime key, double value)
        ///        
        ///     b) Or, leverage a wrapper class that would allow me to do a single
        ///        probe into the dictionary.
        ///             - See if the day is in the dictionary, returning an instance of 
        ///               singleDayTransactionTotalWrapper (first probe)
        ///             - If it is, then update the retrieved instance of 
        ///               singleDayTransactionTotalWrapper (no probe)
        ///        This case demands that we have something to wrap the double outside
        ///        of the dictionary (DateTime key, singleDayTransactionTotalWrapper value)
        ///        
        /// While a probe is O(1), I wanted to take the note regarding performance to
        /// heart.  There may be a better way to do this with less code syntactically in C#.
        /// </summary>
        private class singleDayTransactionTotalWrapper { public double sumOfDaysTransactions; };

        // We have some thread parallelism that we take advantage of below.  This lock object
        // is responsible for ensuring mutual exclusive access to the local content of this 
        // instance
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
                    // Grab the end balance from our last DailyBalance entry
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

            // Because we have a batch, we want to be as efficient 
            // as possible.  There is some work that we can do independently, processing the batch,
            // outside of the work to insert the values into our DailyBalances:
            //
            // NOTE: 
            //    Parallelization across threads could be improved if we allowed each page/batch of 
            //    transactions to be integrated in parallel - without any course grained resource 
            //    locking, as has been used below.  However, to do this we would need:
            //
            //         a) For a transaction update into our DailySum to be atomic.  This would require
            //            us to:
            //
            //                  i) Ensure an atomic addition operation is used when adding 
            //                     our new pageSum to DailyBalance.TotalTransactionSum, as seen
            //                     in DailyBalanceAddToDailyTotalTransactionSum().  Note that
            //                     this atomic behaviour needs to be for all operations across
            //                     the DailyBalance instance that occur within
            //                     DailyBalanceAddToDailyTotalTransactionSum() - so likely this means
            //                     locking that entire object since there are several fields to be
            //                     updated.
            //
            //                  ii) Move to using ThreadSafe dictionaries
            //
            //                  iii) Ensure that our last operation (#3 below) is performed AFTER
            //                       all pages have executed, and not as part of each page integration.
            //
            // The trick with point "iii" is that it would require us to allow our caller class
            // BankDataProviderAndAccountUpdaterForMultiPages
            // to temporarily leave us in an indeterminate state.  That is, each page would leave
            // our sequence of daily min/max invalid.  So, after BankDataProviderAndAccountUpdaterForMultiPages
            // is done with integrating all of the pages, then it would need to ask us to recalc
            // our page max/min across all touched pages.
            //
            // This is not unreasonable, but it does yield ownership of the BankAccountDailyBalance's 
            // proper state to an external class.  And, that such an implementation would need to block 
            // any other thread's usage of BankAccountDailyBalance.  Essentially, the 
            // BankDataProviderAndAccountUpdaterForMultiPages needs to block access to the world while
            // it modifies this instance's state.  
            //
            // For now, I have not approached this opportunity - and instead am doing everything here.
            //
            // So, let's start with:
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
                //    DailyBalance instance will adjust its own ending balance, but the linear sequence of 
                //    ending/starting/ending balances will be out of sync after this update.
                DateTime earliestDayTouched = IntegrateDaySumDictionaryIntoDailyBalances(tempDateTransactionTotals);

                // 3) So, let's now walk through our sorted list and make the appropriate 
                //    corrections to the linear sequence of start/end balances.  This needs to be done starting 
                //    with the earliest day that we touched in this batch, and moving forward to the
                //    end of our list.
                CorrectStartingAndEndingDailyBalances(earliestDayTouched);
            }
        }
        private Dictionary<DateTime, singleDayTransactionTotalWrapper> BuildDictionaryOfSumsForEachDay(IList<FinancialTransactionDto> newTransactionBatch)
        {
            // Create a temporary dictionary that captures the day as the key, and the value is the sum of 
            // all transactions from that day
            var tempDateTransactionTotals = new Dictionary<DateTime, singleDayTransactionTotalWrapper>();
            foreach (var transaction in newTransactionBatch)
            {
                DateTime transactionDateWithoutTime = transaction.Date.Date;  // Strip the date of a specific time (There is no Date-only type in C#)
                if (tempDateTransactionTotals.TryGetValue(transactionDateWithoutTime, out var transactionTotal)) // See singleDayTransactionTotalWrapper class definition note above
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
        private DateTime IntegrateDaySumDictionaryIntoDailyBalances(Dictionary<DateTime, singleDayTransactionTotalWrapper> tempDateTransactionTotals)
        {
            // Iterate through the temporary list of received day balances and update our 
            // stored DailyBalances
            var earliestDayTouched = tempDateTransactionTotals.First().Key;
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
                }
            }
            return earliestDayTouched;
        }
        private void CorrectStartingAndEndingDailyBalances(DateTime earliestDayTouched)
        {
            (int correctionIndex, double prevEndingBalance) = CorrectEarliestTouchedStartingAndEndingBalance(earliestDayTouched);
            CorrectRemainingDaysStartingAndEndingBalances(correctionIndex, prevEndingBalance);
        }
        private (int correctionIndex, double prevEndingBalance) CorrectEarliestTouchedStartingAndEndingBalance(DateTime earliestDayTouched)
        {
            // At first glance, you might wonder: Why do we need to correct the StartingBalance for our first updated/inserted date?
            // After all, a previously existing Date would have had a correct StartingBalance, and we only would have touched 
            // the total transaction amount that occurred on that day.  
            //
            // The concern is over the scenarios where:
            //      a) We inserted a new entry
            //      b) That entry may be at the start of our overall list of Dates, or it may
            //         be downstream amongst existing dates.
            //
            // In these scenarios, we need to set our StartingBalance for the day.
            //
            double prevEndingBalance;
            var correctionIndex = this._dailyBalances.IndexOfKey(earliestDayTouched);
            IDailyBalance dailyBalanceToBeCorrected = this._dailyBalances.ElementAt(correctionIndex).Value;
            if (correctionIndex == 0)
            {
                // First Date in our system.  We must use the StartingBalance for the BankAccount
                prevEndingBalance = this.StartingBalance;
            }
            else
            {
                // There are dates before us.  Grab the previous date's EndingBalance so that we can correct our
                // StartingBalance appropriately
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

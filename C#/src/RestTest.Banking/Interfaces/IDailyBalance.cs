using System;

namespace RestTest.Banking
{
    public interface IDailyBalance
    {
        DateTime Date { get; }
        double StartingBalance { get; }
        public double TotalTransactionSum { get; }
        double EndingBalance { get; }
        void AddToDailyTotalTransactionSum(double amount);
        void AdjustStartingBalance(double startingBalance);
    }
}

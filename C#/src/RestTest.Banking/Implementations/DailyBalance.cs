using System;

namespace RestTest.Banking
{
    public class DailyBalance : IDailyBalance
    {
        public DateTime Date { get; }
        public double StartingBalance { get { return _startingBalance; } }
        private double _startingBalance = 0.0;
        public double TotalTransactionSum { get { return _totalTransactionSum; } }
        private double _totalTransactionSum = 0.0;
        public double EndingBalance { get { return _endingBalance; } }
        private double _endingBalance = 0.0;
        public override string ToString()
        {
            return $"{this.Date:yyyy-MM-dd} {this.EndingBalance:0.00}";
        }
        public void AddToDailyTotalTransactionSum(double amount)
        {
            this._totalTransactionSum += amount;
            this._endingBalance = this._startingBalance + this._totalTransactionSum;
        }
        public void AdjustStartingBalance(double startingBalance)
        {
            this._startingBalance = startingBalance;
            this._endingBalance = this._startingBalance + this._totalTransactionSum;
        }
        public DailyBalance(DateTime date, double startingBalance)
        {
            this.Date = date;
            this._startingBalance = startingBalance;
            this._endingBalance = startingBalance;
            this._totalTransactionSum = this._endingBalance - this._startingBalance;
        }
        public DailyBalance(DateTime date, double startingBalance, double totalTransactionSum ) : this(date, startingBalance)
        {
            AddToDailyTotalTransactionSum(totalTransactionSum);
        }
    }
}

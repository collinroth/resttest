using System;
using Xunit;
using RestTest.Banking;

namespace RestTest.Banking.Tests
{
    public class DailyBalanceTests
    {
        [Theory]
        [InlineData(0.0, 0.0, 0.0)]
        [InlineData(0.0, 5.0, 5.0)]
        [InlineData(0.0, -5.0, -5.0)]
        [InlineData(5.0, 5.0, 10.0)]
        [InlineData(-5.0, -5.0, -10.0)]
        [InlineData(-5.0, 5.0, 0.0)]
        public void AddToDailyTotalTransactionSum_WithCombinationsOfStartAndTransactValues_ReturnsExpectedEndingBalance(double startingValue, double transactionValue, double expectedValue)
        {
            // Arrange
            IDailyBalance dailyBalance = new DailyBalance( DateTime.Parse("01/01/2020 00:00:00"), startingValue );

            // Act
            dailyBalance.AddToDailyTotalTransactionSum(transactionValue);

            // Assert
            dailyBalance.EndingBalance.ShouldEqual(expectedValue);
        }

        [Theory]
        [InlineData(0.0, 0.0, 0.0)]
        [InlineData(0.0, 5.0, 5.0)]
        [InlineData(0.0, -5.0, -5.0)]
        [InlineData(5.0, 5.0, 5.0)]
        [InlineData(-5.0, -5.0, -5.0)]
        [InlineData(-5.0, 5.0, 5.0)]
        public void AddToDailyTotalTransactionSum_WithCombinationsOfStartAndTransactValues_ReturnsExpectedTotalTransactionSum(double startingValue, double transactionValue, double expectedValue)
        {
            // Arrange
            IDailyBalance dailyBalance = new DailyBalance(DateTime.Parse("01/01/2020 00:00:00"), startingValue);

            // Act
            dailyBalance.AddToDailyTotalTransactionSum(transactionValue);

            // Assert
            dailyBalance.TotalTransactionSum.ShouldEqual(expectedValue);
        }

        [Theory]
        [InlineData(40.0)]
        [InlineData(-50.0)]
        [InlineData(0.123456)]
        [InlineData(50000.123456)]
        [InlineData(12.49999)]
        public void DailyBalance_ReturnsExpectedToString(double startingValue)
        {
            // Arrange
            IDailyBalance dailyBalance = new DailyBalance(DateTime.Parse("01/20/2020 00:00:00"), startingValue);

            // Act
            string result = dailyBalance.ToString();

            // Assert
            result.ShouldEqual($"2020-01-20 {startingValue:0.00}");
        }

        [Theory]
        [InlineData(0.0, 0.0, 0.0, 0.0)]
        [InlineData(0.0, 5.0, 0.0, 5.0)]
        [InlineData(0.0, -5.0, 0.0, -5.0)]
        [InlineData(5.0, 5.0, -10.0, -5.0)]
        [InlineData(5.0, -5.0, -5.0, -10.0)]
        [InlineData(5.0, 5.0, -5.0, 0.0)]
        [InlineData(-5.0, -5.0, 0.0, -5.0)]
        public void AdjustStartingBalance_WithCombinationOfValues_ReturnsExpectedEndingBalance(double originalStartingValue, double transactionValue, double newStartingValue, double expectedValue)
        {
            // Arrange
            IDailyBalance dailyBalance = new DailyBalance(DateTime.Parse("01/01/2020 00:00:00"), originalStartingValue);
            dailyBalance.AddToDailyTotalTransactionSum(transactionValue);

            // Act
            dailyBalance.AdjustStartingBalance(newStartingValue);

            // Assert
            dailyBalance.EndingBalance.ShouldEqual(expectedValue);
        }

        [Theory]
        [InlineData(0.0, 0.0, 0.0, 0.0)]
        [InlineData(0.0, 5.0, 0.0, 5.0)]
        [InlineData(0.0, -5.0, 0.0, -5.0)]
        [InlineData(5.0, 5.0, -10.0, 5.0)]
        [InlineData(5.0, -5.0, -5.0, -5.0)]
        [InlineData(5.0, 5.0, -5.0, 5.0)]
        [InlineData(-5.0, -5.0, 0.0, -5.0)]
        public void AdjustStartingBalance_WithCombinationOfValues_ReturnsExpectedTransactionSum(double originalStartingValue, double transactionValue, double newStartingValue, double expectedValue)
        {
            // Arrange
            IDailyBalance dailyBalance = new DailyBalance(DateTime.Parse("01/01/2020 00:00:00"), originalStartingValue);
            dailyBalance.AddToDailyTotalTransactionSum(transactionValue);

            // Act
            dailyBalance.AdjustStartingBalance(newStartingValue);

            // Assert
            dailyBalance.TotalTransactionSum.ShouldEqual(expectedValue);
        }
    }
}

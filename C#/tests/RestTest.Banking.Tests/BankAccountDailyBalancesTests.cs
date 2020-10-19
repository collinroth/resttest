using System;
using Xunit;
using RestTest.Banking;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace RestTest.Banking.Tests
{
    public class BankAccountDailyBalancesTests
    {
        [Theory]
        [InlineData(0.0, 0.0, 0.0)]
        [InlineData(0.0, 5.0, 5.0)]
        [InlineData(0.0, -5.0, -5.0)]
        [InlineData(-5.0, 0.0, -5.0)]
        [InlineData(5.0, 5.0, 10.0)]
        [InlineData(-5.0, -5.0, -10.0)]
        [InlineData(-5.0, 5.0, 0.0)]
        public void InsertSingleNewTransaction_WithCombinationsOfStartAndTransactValues_ReturnsExpectedBalance(double startingValue, double transactionValue, double expectedValue)
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(startingValue);
            FinancialTransactionDto transaction = new FinancialTransactionDto(DateTime.Parse("01/01/2020 00:00:00"), "Ledger", transactionValue, "Company");

            // Act
            dailyBalances.InsertSingleNewTransaction(transaction);

            // Assert
            dailyBalances.LastDailyEndingBalance.ShouldEqual(expectedValue);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_SingleDayTwoTransactions_ReturnsExpectedBalance()
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(0.0);

            var parameters = new List<(string Date, double Value)>()
            {
                ("01/01/2020 00:00:00", 1.0),
                ("01/01/2020 00:00:00", 2.0),
            };

            var transactions = new List<FinancialTransactionDto>();
            foreach (var param in parameters)
            {
                transactions.Add(new FinancialTransactionDto(DateTime.Parse(param.Date), "Ledger", param.Value, "Company"));
            }

            // Act
            dailyBalances.InsertBatchOfNewTransactions(transactions);

            // Assert
            dailyBalances.LastDailyEndingBalance.ShouldEqual(3.0);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_SingleDayThreeTransactions_ReturnsExpectedBalance()
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(0.0);

            var parameters = new List<(string Date, double Value)>()
            {
                ("01/01/2020 00:00:00", 1.0),
                ("01/01/2020 00:00:00", 2.0),
                ("01/01/2020 00:00:00", 3.0),
            };

            var transactions = new List<FinancialTransactionDto>();
            foreach (var param in parameters)
            {
                transactions.Add(new FinancialTransactionDto(DateTime.Parse(param.Date), "Ledger", param.Value, "Company"));
            }

            // Act
            dailyBalances.InsertBatchOfNewTransactions(transactions);

            // Assert
            dailyBalances.LastDailyEndingBalance.ShouldEqual(6);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_TwoDaysOneTransactionEach_ReturnsExpectedBalance()
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(0.0);

            var parameters = new List<(string Date, double Value)>()
            {
                ("01/01/2020 00:00:00", 1.0),
                ("01/02/2020 00:00:00", 2.0),
            };

            var transactions = new List<FinancialTransactionDto>();
            foreach (var param in parameters)
            {
                transactions.Add(new FinancialTransactionDto(DateTime.Parse(param.Date), "Ledger", param.Value, "Company"));
            }

            // Act
            dailyBalances.InsertBatchOfNewTransactions(transactions);

            // Assert
            dailyBalances.LastDailyEndingBalance.ShouldEqual(3.0);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_ThreeSequentialDaysOneTransactionEach_ReturnsExpectedBalance()
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(0.0);

            var parameters = new List<(string Date, double Value)>()
            {
                ("01/01/2020 00:00:00", 1.0),
                ("01/02/2020 00:00:00", 2.0),
                ("01/03/2020 00:00:00", 3.0),
            };

            var transactions = new List<FinancialTransactionDto>();
            foreach (var param in parameters)
            {
                transactions.Add(new FinancialTransactionDto(DateTime.Parse(param.Date), "Ledger", param.Value, "Company"));
            }

            // Act
            dailyBalances.InsertBatchOfNewTransactions(transactions);

            // Assert
            dailyBalances.LastDailyEndingBalance.ShouldEqual(6.0);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_ThreeSparseDaysThreeTransactionsEach_ReturnsExpectedBalance()
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(0.0);

            var parameters = new List<(string Date, double Value)>()
            {
                ("01/01/2020 00:00:00", 1.0),
                ("01/01/2020 01:00:00", 1.0),
                ("01/01/2020 02:00:00", 1.0),
                ("02/15/2020 03:00:00", 2.0),
                ("02/15/2020 04:00:00", 2.0),
                ("02/15/2020 05:00:00", 2.0),
                ("04/01/2020 06:00:00", 3.0),
                ("04/01/2020 07:00:00", 3.0),
                ("04/01/2020 08:00:00", 3.0),
            };

            var transactions = new List<FinancialTransactionDto>();
            foreach (var param in parameters)
            {
                transactions.Add(new FinancialTransactionDto(DateTime.Parse(param.Date), "Ledger", param.Value, "Company"));
            }

            // Act
            dailyBalances.InsertBatchOfNewTransactions(transactions);

            // Assert
            dailyBalances.LastDailyEndingBalance.ShouldEqual(18.0);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_ThreeSparseDaysThreeTransactionsEach_ReturnsExpectedDailyBalance()
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(0.0);

            var parameters = new List<(string Date, double Value)>()
            {
                ("01/01/2020 00:00:00", 1.0),
                ("01/01/2020 01:00:00", 1.0),
                ("01/01/2020 02:00:00", 1.0),
                ("02/15/2020 03:00:00", 2.0),
                ("02/15/2020 04:00:00", 2.0),
                ("02/15/2020 05:00:00", 2.0),
                ("04/01/2020 06:00:00", 3.0),
                ("04/01/2020 07:00:00", 3.0),
                ("04/01/2020 08:00:00", 3.0),
            };

            var transactions = new List<FinancialTransactionDto>();
            foreach (var param in parameters)
            {
                transactions.Add(new FinancialTransactionDto(DateTime.Parse(param.Date), "Ledger", param.Value, "Company"));
            }

            // Act
            dailyBalances.InsertBatchOfNewTransactions(transactions);

            // Assert
            var balances = dailyBalances.GetAllDailyBalances();
            balances.Count.ShouldEqual(3);
            balances[0].Date.ShouldEqual(DateTime.Parse("01/01/2020 00:00:00"));
            balances[0].StartingBalance.ShouldEqual(0);
            balances[0].TotalTransactionSum.ShouldEqual(3);
            balances[0].EndingBalance.ShouldEqual(3);

            balances[1].Date.ShouldEqual(DateTime.Parse("02/15/2020 00:00:00"));
            balances[1].StartingBalance.ShouldEqual(3);
            balances[1].TotalTransactionSum.ShouldEqual(6);
            balances[1].EndingBalance.ShouldEqual(9);

            balances[2].Date.ShouldEqual(DateTime.Parse("04/01/2020 00:00:00"));
            balances[2].StartingBalance.ShouldEqual(9);
            balances[2].TotalTransactionSum.ShouldEqual(9);
            balances[2].EndingBalance.ShouldEqual(18);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_AddingSingleTransactionIntoStartOfExisting_ReturnsExpectedDailyBalance()
        {
            IBankAccountDailyBalances dailyBalances = BuildDailyBalances();

            // Act
            dailyBalances.InsertSingleNewTransaction(new FinancialTransactionDto(DateTime.Parse("01/01/2020 02:00:00"), "Ledger", 100.0, "Company"));

            // Assert
            var balances = dailyBalances.GetAllDailyBalances();
            balances.Count.ShouldEqual(4);
            balances[0].Date.ShouldEqual(DateTime.Parse("01/01/2020 00:00:00"));
            balances[0].StartingBalance.ShouldEqual(0);
            balances[0].TotalTransactionSum.ShouldEqual(100.0);
            balances[0].EndingBalance.ShouldEqual(100.0);

            balances[1].Date.ShouldEqual(DateTime.Parse("01/10/2020 00:00:00"));
            balances[1].StartingBalance.ShouldEqual(100.0);
            balances[1].TotalTransactionSum.ShouldEqual(3);
            balances[1].EndingBalance.ShouldEqual(103.0);

            balances[2].Date.ShouldEqual(DateTime.Parse("02/15/2020 00:00:00"));
            balances[2].StartingBalance.ShouldEqual(103.0);
            balances[2].TotalTransactionSum.ShouldEqual(6);
            balances[2].EndingBalance.ShouldEqual(109.0);

            balances[3].Date.ShouldEqual(DateTime.Parse("04/01/2020 00:00:00"));
            balances[3].StartingBalance.ShouldEqual(109.0);
            balances[3].TotalTransactionSum.ShouldEqual(9);
            balances[3].EndingBalance.ShouldEqual(118.0);
        }

        private IBankAccountDailyBalances BuildDailyBalances()
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(0.0);

            var parameters = new List<(string Date, double Value)>()
            {
                ("01/10/2020 00:00:00", 1.0),
                ("01/10/2020 01:00:00", 1.0),
                ("01/10/2020 02:00:00", 1.0),
                ("02/15/2020 03:00:00", 2.0),
                ("02/15/2020 04:00:00", 2.0),
                ("02/15/2020 05:00:00", 2.0),
                ("04/01/2020 06:00:00", 3.0),
                ("04/01/2020 07:00:00", 3.0),
                ("04/01/2020 08:00:00", 3.0),
            };

            var transactions = new List<FinancialTransactionDto>();
            foreach (var param in parameters)
            {
                transactions.Add(new FinancialTransactionDto(DateTime.Parse(param.Date), "Ledger", param.Value, "Company"));
            }

            dailyBalances.InsertBatchOfNewTransactions(transactions);
            return dailyBalances;
        }

        [Fact]
        public void InsertBatchOfNewTransactions_AddingSingleTransactionIntoMiddleOfExisting_ReturnsExpectedDailyBalance()
        {
            IBankAccountDailyBalances dailyBalances = BuildDailyBalances();

            // Act
            dailyBalances.InsertSingleNewTransaction(new FinancialTransactionDto(DateTime.Parse("01/15/2020 02:00:00"), "Ledger", 100.0, "Company"));

            // Assert
            var balances = dailyBalances.GetAllDailyBalances();
            balances.Count.ShouldEqual(4);
            balances[0].Date.ShouldEqual(DateTime.Parse("01/10/2020 00:00:00"));
            balances[0].StartingBalance.ShouldEqual(0.0);
            balances[0].TotalTransactionSum.ShouldEqual(3);
            balances[0].EndingBalance.ShouldEqual(3.0);

            balances[1].Date.ShouldEqual(DateTime.Parse("01/15/2020 00:00:00"));
            balances[1].StartingBalance.ShouldEqual(3.0);
            balances[1].TotalTransactionSum.ShouldEqual(100.0);
            balances[1].EndingBalance.ShouldEqual(103.0);

            balances[2].Date.ShouldEqual(DateTime.Parse("02/15/2020 00:00:00"));
            balances[2].StartingBalance.ShouldEqual(103.0);
            balances[2].TotalTransactionSum.ShouldEqual(6);
            balances[2].EndingBalance.ShouldEqual(109.0);

            balances[3].Date.ShouldEqual(DateTime.Parse("04/01/2020 00:00:00"));
            balances[3].StartingBalance.ShouldEqual(109.0);
            balances[3].TotalTransactionSum.ShouldEqual(9);
            balances[3].EndingBalance.ShouldEqual(118.0);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_AddingSingleTransactionIntoEndOfExisting_ReturnsExpectedDailyBalance()
        {
            IBankAccountDailyBalances dailyBalances = BuildDailyBalances();

            // Act
            dailyBalances.InsertSingleNewTransaction(new FinancialTransactionDto(DateTime.Parse("05/15/2020 02:00:00"), "Ledger", 100.0, "Company"));

            // Assert
            var balances = dailyBalances.GetAllDailyBalances();
            balances.Count.ShouldEqual(4);
            balances[0].Date.ShouldEqual(DateTime.Parse("01/10/2020 00:00:00"));
            balances[0].StartingBalance.ShouldEqual(0.0);
            balances[0].TotalTransactionSum.ShouldEqual(3);
            balances[0].EndingBalance.ShouldEqual(3.0);

            balances[1].Date.ShouldEqual(DateTime.Parse("02/15/2020 00:00:00"));
            balances[1].StartingBalance.ShouldEqual(3.0);
            balances[1].TotalTransactionSum.ShouldEqual(6);
            balances[1].EndingBalance.ShouldEqual(9.0);

            balances[2].Date.ShouldEqual(DateTime.Parse("04/01/2020 00:00:00"));
            balances[2].StartingBalance.ShouldEqual(9.0);
            balances[2].TotalTransactionSum.ShouldEqual(9);
            balances[2].EndingBalance.ShouldEqual(18.0);

            balances[3].Date.ShouldEqual(DateTime.Parse("05/15/2020 00:00:00"));
            balances[3].StartingBalance.ShouldEqual(18.0);
            balances[3].TotalTransactionSum.ShouldEqual(100.0);
            balances[3].EndingBalance.ShouldEqual(118.0);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_AddingSingleTransactionIntoExistingFirstDay_ReturnsExpectedDailyBalance()
        {
            IBankAccountDailyBalances dailyBalances = BuildDailyBalances();

            // Act
            dailyBalances.InsertSingleNewTransaction(new FinancialTransactionDto(DateTime.Parse("01/10/2020 02:00:00"), "Ledger", 100.0, "Company"));

            // Assert
            var balances = dailyBalances.GetAllDailyBalances();
            balances.Count.ShouldEqual(3);
            balances[0].Date.ShouldEqual(DateTime.Parse("01/10/2020 00:00:00"));
            balances[0].StartingBalance.ShouldEqual(0.0);
            balances[0].TotalTransactionSum.ShouldEqual(103.0);
            balances[0].EndingBalance.ShouldEqual(103.0);

            balances[1].Date.ShouldEqual(DateTime.Parse("02/15/2020 00:00:00"));
            balances[1].StartingBalance.ShouldEqual(103.0);
            balances[1].TotalTransactionSum.ShouldEqual(6);
            balances[1].EndingBalance.ShouldEqual(109.0);

            balances[2].Date.ShouldEqual(DateTime.Parse("04/01/2020 00:00:00"));
            balances[2].StartingBalance.ShouldEqual(109.0);
            balances[2].TotalTransactionSum.ShouldEqual(9);
            balances[2].EndingBalance.ShouldEqual(118.0);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_AddingSingleTransactionIntoExistingMiddleDay_ReturnsExpectedDailyBalance()
        {
            IBankAccountDailyBalances dailyBalances = BuildDailyBalances();

            // Act
            dailyBalances.InsertSingleNewTransaction(new FinancialTransactionDto(DateTime.Parse("02/15/2020 02:00:00"), "Ledger", 100.0, "Company"));

            // Assert
            var balances = dailyBalances.GetAllDailyBalances();
            balances.Count.ShouldEqual(3);
            balances[0].Date.ShouldEqual(DateTime.Parse("01/10/2020 00:00:00"));
            balances[0].StartingBalance.ShouldEqual(0.0);
            balances[0].TotalTransactionSum.ShouldEqual(3.0);
            balances[0].EndingBalance.ShouldEqual(3.0);

            balances[1].Date.ShouldEqual(DateTime.Parse("02/15/2020 00:00:00"));
            balances[1].StartingBalance.ShouldEqual(3.0);
            balances[1].TotalTransactionSum.ShouldEqual(106);
            balances[1].EndingBalance.ShouldEqual(109.0);

            balances[2].Date.ShouldEqual(DateTime.Parse("04/01/2020 00:00:00"));
            balances[2].StartingBalance.ShouldEqual(109.0);
            balances[2].TotalTransactionSum.ShouldEqual(9);
            balances[2].EndingBalance.ShouldEqual(118.0);
        }

        [Fact]
        public void InsertBatchOfNewTransactions_AddingSingleTransactionIntoExistingLastDay_ReturnsExpectedDailyBalance()
        {
            IBankAccountDailyBalances dailyBalances = BuildDailyBalances();

            // Act
            dailyBalances.InsertSingleNewTransaction(new FinancialTransactionDto(DateTime.Parse("04/01/2020 02:00:00"), "Ledger", 100.0, "Company"));

            // Assert
            var balances = dailyBalances.GetAllDailyBalances();
            balances.Count.ShouldEqual(3);
            balances[0].Date.ShouldEqual(DateTime.Parse("01/10/2020 00:00:00"));
            balances[0].StartingBalance.ShouldEqual(0.0);
            balances[0].TotalTransactionSum.ShouldEqual(3.0);
            balances[0].EndingBalance.ShouldEqual(3.0);

            balances[1].Date.ShouldEqual(DateTime.Parse("02/15/2020 00:00:00"));
            balances[1].StartingBalance.ShouldEqual(3.0);
            balances[1].TotalTransactionSum.ShouldEqual(6);
            balances[1].EndingBalance.ShouldEqual(9.0);

            balances[2].Date.ShouldEqual(DateTime.Parse("04/01/2020 00:00:00"));
            balances[2].StartingBalance.ShouldEqual(9.0);
            balances[2].TotalTransactionSum.ShouldEqual(109);
            balances[2].EndingBalance.ShouldEqual(118.0);
        }

        [Fact]
        public void ForEach_MultipleDailyBalances_EachDailyBalanceIsSeen()
        {
            IBankAccountDailyBalances dailyBalances = BuildDailyBalances();
            var expectedBalances = dailyBalances.GetAllDailyBalances();
            List<DateTime> expectedDays = new List<DateTime>();
            List<DateTime> visitedDays = new List<DateTime>();
            dailyBalances.ForEach(dailyBalance => expectedDays.Add(dailyBalance.Date));

            // Act
            dailyBalances.ForEach(dailyBalance => visitedDays.Add(dailyBalance.Date));

            // Assert
            visitedDays.Count.ShouldEqual(expectedBalances.Count);
            expectedDays.ShouldEqual(visitedDays);
        }

        [Fact]
        public void ForEach_SingleDailyBalance_EachDailyBalanceIsSeen()
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(0.0);
            FinancialTransactionDto transaction = new FinancialTransactionDto(DateTime.Parse("01/01/2020 00:00:00"), "Ledger", 20.0, "Company");
            dailyBalances.InsertSingleNewTransaction(transaction);
            var expectedBalances = dailyBalances.GetAllDailyBalances();
            List<DateTime> expectedDays = new List<DateTime>();
            List<DateTime> visitedDays = new List<DateTime>();
            dailyBalances.ForEach(dailyBalance => expectedDays.Add(dailyBalance.Date));

            // Act
            dailyBalances.ForEach(dailyBalance => visitedDays.Add(dailyBalance.Date));

            // Assert
            visitedDays.Count.ShouldEqual(expectedBalances.Count);
            expectedDays.ShouldEqual(visitedDays);
        }

        [Fact]
        public void ForEach_EmptyDailyBalance_DoesNotExecuteAnything()
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(0.0);
            int count = 0;

            // Act
            dailyBalances.ForEach(dailyBalance => count += 1);

            // Assert
            count.ShouldEqual(0);
        }

        [Fact]
        public void GetAllDailyBalances_EmptyDailyBalance_ReturnsEmptyArray()
        {
            // Arrange
            IBankAccountDailyBalances dailyBalances = new BankAccountDailyBalances(0.0);

            // Act
            var expectedBalances = dailyBalances.GetAllDailyBalances();

            // Assert
            expectedBalances.Count.ShouldEqual(0);
        }
    }
}

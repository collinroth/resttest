using System;
using Xunit;
using RestTest.Banking;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Moq.Protected;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;

namespace RestTest.Banking.Tests
{
    public class BankDataProviderAndAccountUpdaterForMultiPagesTests
    {
        [Fact]
        public async void GetAllTransactionsAndUpdateAccount_OnePageWithSingleValidContent_CorrectResultsReturned()
        {
            // Put together the list of pages that will be mock returned from the internet
            List<PageResponseDto> responsePages = new List<PageResponseDto>()
            {
                new PageResponseDto
                { totalCount = 1, page = 1, transactions = new List<FinancialTransactionDto>
                    {
new FinancialTransactionDto{ Date = DateTime.Parse("01/01/2020 00:00:00"), Ledger = "Phone & Internet Expense", Amount = -10, Company = "SHAW CABLESYSTEMS CALGARY AB", },
                    }
                }
            };

            await RunTest(responsePages);
        }

        [Fact]
        public async void GetAllTransactionsAndUpdateAccount_OnePageWithNoValidContent_CorrectResultsReturned()
        {
            // Put together the list of pages that will be mock returned from the internet
            List<PageResponseDto> responsePages = new List<PageResponseDto>()
            {
                new PageResponseDto
                { totalCount = 0, page = 1, transactions = new List<FinancialTransactionDto>
                    {
                    }
                }
            };

            await RunTest(responsePages);
        }

        [Fact]
        public async void GetAllTransactionsAndUpdateAccount_OnePageWithValidContent_CorrectResultsReturned()
        {
            // Put together the list of pages that will be mock returned from the internet
            List<PageResponseDto> responsePages = new List<PageResponseDto>()
            {
                new PageResponseDto
                { totalCount = 2, page = 1, transactions = new List<FinancialTransactionDto>
                    {
new FinancialTransactionDto{ Date = DateTime.Parse("01/01/2020 00:00:00"), Ledger = "Phone & Internet Expense", Amount = -10, Company = "SHAW CABLESYSTEMS CALGARY AB", },
new FinancialTransactionDto{ Date = DateTime.Parse("01/01/2020 00:00:00"), Ledger = "Travel Expense, Nonlocal", Amount = -8.1, Company = "BLACK TOP CABS VANCOUVER BC", },
                    }
                }
            };

            await RunTest(responsePages);
        }

        [Fact]
        public async void GetAllTransactionsAndUpdateAccount_TwoPagesWithValidContent_CorrectResultsReturned()
        {
            // Put together the list of pages that will be mock returned from the internet
            List<PageResponseDto> responsePages = new List<PageResponseDto>()
            {
                new PageResponseDto
                { totalCount = 2, page = 1, transactions = new List<FinancialTransactionDto>
                    {
new FinancialTransactionDto{ Date = DateTime.Parse("01/01/2020 00:00:00"), Ledger = "Phone & Internet Expense", Amount = -10, Company = "SHAW CABLESYSTEMS CALGARY AB", },
                    }
                },
                new PageResponseDto
                { totalCount = 2, page = 2, transactions = new List<FinancialTransactionDto>
                    {
new FinancialTransactionDto{ Date = DateTime.Parse("01/01/2020 00:00:00"), Ledger = "Travel Expense, Nonlocal", Amount = -8.1, Company = "BLACK TOP CABS VANCOUVER BC", },
                    }
                },
            };

            await RunTest(responsePages);
        }

        private async Task RunTest(List<PageResponseDto> expectedResponsePages)
        {
            // ARRANGE

            // Mock the dependency on the page provider
            var singlePageRESTMock = new Mock<IBankDataProviderOfSinglePageREST>();
            int pageNumber = 0;
            singlePageRESTMock.Setup(x => x.GetPageOfTransactions(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    return expectedResponsePages[pageNumber++];
                });

            // Mock the dependency on BankAccount (note that even the Mock callback needs to be thread safe)
            List<IList<FinancialTransactionDto>> receivedBatchesGivenToAccount = new List<IList<FinancialTransactionDto>>();
            var account = new Mock<IBankAccount>();
            account.Setup(x => x.InsertBatchOfNewTransactions(It.IsAny<IList<FinancialTransactionDto>>()))
                                .Callback((IList<FinancialTransactionDto> s) => 
                                            {
                                                lock (receivedBatchesGivenToAccount)
                                                {
                                                    receivedBatchesGivenToAccount.Add(s);
                                                }
                                            });

            // Create the system under test
            BankDataProviderAndAccountUpdaterForMultiPages dataProviderAndAccountUpdater = new BankDataProviderAndAccountUpdaterForMultiPages(singlePageRESTMock.Object);

            // ACT
            await dataProviderAndAccountUpdater.GetAllTransactionsAndUpdateAccount(account.Object, CancellationToken.None);

            // ASSERT

            // confirm that the account received all of the expected transactions

            receivedBatchesGivenToAccount.Count.ShouldEqual(expectedResponsePages.Count);

            // If there is only one page, then we can compare the individual transactions.  However, if there
            // is more than one page, then the pages may have been processed in a different order (on different threads)
            // And since there are no unique identifiers within the transactions, we have to skip any comparisons when
            // there is more than 1 page.
            pageNumber = 0;
            if (expectedResponsePages.Count == 1)
            {
                int expectedTransactionCount = expectedResponsePages[pageNumber].transactions.Count;
                receivedBatchesGivenToAccount[pageNumber].Count.ShouldEqual(expectedTransactionCount);
                for (int transactionNumber = 0;
                     transactionNumber < expectedTransactionCount;
                    transactionNumber++)
                {
                    receivedBatchesGivenToAccount[pageNumber][transactionNumber].ShouldEqual(expectedResponsePages[pageNumber].transactions[transactionNumber]);
                }
            }
        }
    }
}

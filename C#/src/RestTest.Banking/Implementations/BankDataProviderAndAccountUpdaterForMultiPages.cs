using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
namespace RestTest.Banking
{

    public class BankDataProviderAndAccountUpdaterForMultiPages : IBankDataProviderAndAccountUpdater
    {
        private IBankDataProviderOfSinglePageREST RestSinglePageProvider { get; }
        public BankDataProviderAndAccountUpdaterForMultiPages(string baseUrl) : this(new BankDataProviderOfSinglePageREST(baseUrl) )
        {
        }
        public BankDataProviderAndAccountUpdaterForMultiPages(IBankDataProviderOfSinglePageREST restSinglePageProvider)
        {
            this.RestSinglePageProvider = restSinglePageProvider;
        }
        public async Task GetAllTransactionsAndUpdateAccount(IBankAccount forBankAccount, CancellationToken cancellationToken)
        {
            int pageNumber = 1;
            int remainingExpectedTransactions = 0;
            List<Task> mergeIntoAccountTaskList = new List<Task>();
            do
            {
                // Retrieve the next page
                PageResponseDto pageResponse = await RestSinglePageProvider.GetPageOfTransactions(pageNumber, cancellationToken);
                if (pageNumber == 1)
                {
                    remainingExpectedTransactions = pageResponse.totalCount;
                }

                // Kick off a background task of merging the page transactions into the bank account
                mergeIntoAccountTaskList.Add(Task.Factory.StartNew(() => forBankAccount.InsertBatchOfNewTransactions(pageResponse.transactions)));

                // Update our transaction count and page number
                remainingExpectedTransactions -= pageResponse.transactions.Count;
                pageNumber += 1;
            }
            while (remainingExpectedTransactions > 0);

            // Wait until all of the pages have been merged
            Task.WaitAll(mergeIntoAccountTaskList.ToArray(), cancellationToken);
        }
    }
}

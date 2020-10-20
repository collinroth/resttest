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
            // We are about to iterate through a process of:
            //
            // 1) Loop:
            //      a) Retrieve a page of data
            //      b) Kick off the integration that page data into our BankAccount (background thread)
            //    Until all pages have been retrieved.
            //
            // 2) Finally, wait for any remaining account work in step #b to be completed
            //
            // To optimize performance, the code below will overlap this work.  That is, the intent is to
            // allow for the I/O of the next socket call to be performed while we integrate the previous page into our 
            // Account - which is a CPU bound operation.
            // 
            // Obviously, this requires a few things:
            //    a) That a single Account instance is thread safe in its work to merge.  It's possible that we will have
            //       fast I/O and multiple pages may be merging concurrently.
            //    b) We need to recognize (and accept) that pages may be integrated into the Account in a different 
            //       order than when they were received.  In other words, transactions from page 3 may get processed before
            //       page 2.
            //
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

                if (pageNumber != pageResponse.page)
                {
                    throw new ApplicationException($"REST call to {RestSinglePageProvider} provided invalid page information.  Expected pageNumber:{pageNumber} but received {pageResponse.page}.");
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

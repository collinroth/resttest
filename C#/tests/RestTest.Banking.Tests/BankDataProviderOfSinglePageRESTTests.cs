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
    public class BankDataProviderOfSinglePageRESTTests
    {
        private class ArrangedTypes 
        {
            public BankDataProviderOfSinglePageREST RestPagedDataProvider;
            public Mock<HttpMessageHandler> HandlerMock;
            public Uri ExpectedUri;
        }

        private Mock<HttpMessageHandler> SetupMockHttp(string jsonFileText, HttpStatusCode code)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = code,
                   Content = new StringContent(jsonFileText),
               })
               .Verifiable();

            return handlerMock;
        }

        private ArrangedTypes SetupHttpMockWithJsonString(string jsonFileText, HttpStatusCode code, int pageNumber)
        {
            // Prepare the Mock Http
            string baseUri = "https://localhost:44364";
            var expectedUri = new Uri($"{baseUri}/transactions/{pageNumber}.json");
            var handlerMock = SetupMockHttp(jsonFileText, code);

            // Create the system under test
            BankDataProviderOfSinglePageREST restPagedDataProvider = new BankDataProviderOfSinglePageREST(baseUri, handlerMock.Object);

            return new ArrangedTypes
            {
                RestPagedDataProvider = restPagedDataProvider,
                HandlerMock = handlerMock,
                ExpectedUri = expectedUri,
            };
        }

        private ArrangedTypes SetupHttpMock(string jsonFilePath, HttpStatusCode code, int pageNumber)
        {
            // This file needs to be available for this test to execute
            if (!File.Exists(jsonFilePath))
            {
                Assert.True(false);
            }
            string jsonFileText = File.ReadAllText(jsonFilePath);

            return SetupHttpMockWithJsonString(jsonFileText, code, pageNumber);
        }

        [Fact]
        public async void GetAllTransactionsAndUpdateAccount_RetrievePageWithValidContent_CorrectResultsReturned()
        {
            // ARRANGE
            int pageNumber = 1;
            ArrangedTypes arranged = SetupHttpMock(@"jsonTestFiles/SinglePageTestData.json", HttpStatusCode.OK, pageNumber);

            // ACT
            PageResponseDto response = await arranged.RestPagedDataProvider.GetPageOfTransactions(pageNumber, CancellationToken.None);

            // ASSERT
            response.page.ShouldEqual(1);
            response.totalCount.ShouldEqual(1);
            response.transactions[0].Date.ShouldEqual(DateTime.Parse("12/22/2013 00:00:00"));
            response.transactions[0].Ledger.ShouldEqual("Phone & Internet Expense");
            response.transactions[0].Amount.ShouldEqual(-110.71);
            response.transactions[0].Company.ShouldEqual("SHAW CABLESYSTEMS CALGARY AB");
            arranged.HandlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1), // we expected a single external request
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Get  // we expected a GET request
                  && req.RequestUri == arranged.ExpectedUri // to this uri
               ),
               ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async void GetAllTransactionsAndUpdateAccount_BadRequest_CorrectResultsReturned()
        {
            // ARRANGE
            HttpStatusCode code = HttpStatusCode.BadRequest;
            int pageNumber = 1;
            ArrangedTypes arranged = SetupHttpMock(@"jsonTestFiles/blank.json", code, pageNumber);

            // ACT and Assert
            var taskException = await Assert.ThrowsAsync<SimpleHttpResponseException>(() => arranged.RestPagedDataProvider.GetPageOfTransactions(pageNumber, CancellationToken.None));
            taskException.StatusCode.ShouldEqual(code);
        }

        [Fact]
        public async void GetAllTransactionsAndUpdateAccount_RequestFailWith_CorrectResultsReturned()
        {
            // ARRANGE
            HttpStatusCode code = HttpStatusCode.NotFound;
            int pageNumber = 1;
            ArrangedTypes arranged = SetupHttpMock(@"jsonTestFiles/blank.json", code, pageNumber);

            // ACT and Assert
            var taskException = await Assert.ThrowsAsync<SimpleHttpResponseException>(() => arranged.RestPagedDataProvider.GetPageOfTransactions(pageNumber, CancellationToken.None));
            taskException.StatusCode.ShouldEqual(code);
        }
    }
}

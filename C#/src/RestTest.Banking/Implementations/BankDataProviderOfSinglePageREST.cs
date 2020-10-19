using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RestTest.Banking
{
    public class SimpleHttpResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public SimpleHttpResponseException(HttpStatusCode statusCode, string content) : base(content)
        {
            StatusCode = statusCode;
        }
    }
    public class BankDataProviderOfSinglePageREST : IBankDataProviderOfSinglePageREST
    {
        public BankDataProviderOfSinglePageREST(string baseUrl) : this(baseUrl, null)
        {
        }
        public BankDataProviderOfSinglePageREST(string baseUrl, HttpMessageHandler messageHandler)
        {
            this._baseUrl = baseUrl;
            if (messageHandler != null)
            {
                this._httpClient = new HttpClient(messageHandler)
                {
                    BaseAddress = new Uri(_baseUrl),
                    // There is a 100 second timeout on the socket.  If we want to change this,
                    // then we must also set the Timeout property

                    // Client authentication may also need to be established here (if desired)
                };
            }
            else
            {
                this._httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            }
        }
        private string _baseUrl;
        private readonly HttpClient _httpClient;

        public async Task<PageResponseDto> GetPageOfTransactions(int pageNumber, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"transactions/{pageNumber}.json");

            // Use streaming to ensure that we don't wait for the entire response to be completed
            // before we start feeding it to the Json deserializer
            using (var result = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                using (var contentStream = await result.Content.ReadAsStreamAsync())
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return await JsonSerializer.DeserializeAsync<PageResponseDto>(contentStream, LocalJsonSerializerOptions, cancellationToken);
                    }
                    else
                    {
                        using (var streamReader = new StreamReader(contentStream))
                        {
                            string content = $"{result.StatusCode}:{(int)result.StatusCode}:{result.ReasonPhrase} returned from {request.Method} method for uri {request.RequestUri} with the following message:\n" + streamReader.ReadToEnd();
                            throw new SimpleHttpResponseException(result.StatusCode, content);
                        }
                    }
                }
            }
        }
        private static JsonSerializerOptions _options = null;
        private static JsonSerializerOptions LocalJsonSerializerOptions
        {
            get
            {
                if (_options != null)
                    return _options;
                _options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                _options.Converters.Add(new DoubleJsonConverter());
                return _options;
            }
        }
    }
}

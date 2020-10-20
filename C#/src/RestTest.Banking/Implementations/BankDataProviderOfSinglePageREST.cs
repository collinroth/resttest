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
    // Ironically, the HTTPClient class in .net core doesn't throw an exception by default when a failure occurs.
    // And sadly, when you call EnsureSuccessStatusCode() it doesn't put the received StatusCode within the exception.
    // To that end, the SimpleHttpResponseException has been created below:
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
                    // TODO: There is a 100 second timeout on the socket.  If we want to change this,
                    // then we must also set the Timeout property

                    // TODO: Client authentication may also need to be established here (if desired)
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

            // There are two possibilities here:
            //
            //      1) We send the request and block waiting for it to complete, returning the entire result set
            //      2) We send the request, wait for the headers to be completed, and then receive an open stream 
            //         for content - processing that stream as it's being received.
            //
            // I've yielded to option #2 to minimize both the memory footprint and to enable parallelism between the JSON 
            // deserializer in parallel with the socket I/O.
            //
            using (var result = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                using (var contentStream = await result.Content.ReadAsStreamAsync())
                {
                    if (result.IsSuccessStatusCode)
                    {
                        try
                        {
                            return await JsonSerializer.DeserializeAsync<PageResponseDto>(contentStream, LocalJsonSerializerOptions, cancellationToken);
                        }
                        catch (JsonException jsonEx)
                        {
                            // While it would be also nice to capture the content returned and store/print that somewhere
                            // we can't in this situation because we've streamed.
                            ApplicationException appEx = new ApplicationException($"REST {request.Method} call to {request.RequestUri} returned {result.StatusCode}:{(int)result.StatusCode} however the returned data was malformed JSON.",
                                                    jsonEx);
                            throw appEx;
                        }
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
        public override string ToString()
        {
            return $"{this._baseUrl}";
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
                // Note that we have the Amount field coming back from the JSON page 
                // as a string, but we would really like a double.  To that end,
                // we add a converter from "string->double"
                _options.Converters.Add(new DoubleJsonConverter());
                return _options;
            }
        }
    }
}

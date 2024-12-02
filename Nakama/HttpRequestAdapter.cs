// Copyright 2019 The Nakama Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Nakama.TinyJson;

namespace Nakama
{
    /// <summary>
    /// HTTP Request adapter which uses the .NET HttpClient to send requests.
    /// </summary>
    /// <remarks>
    /// Accept header is always set as 'application/json'.
    /// </remarks>
    public class HttpRequestAdapter : IHttpAdapter
    {
        public TransientExceptionDelegate TransientExceptionDelegate => IsTransientException;

        public IClient Client { get; set; }
        private readonly HttpClient _httpClient;

        // Cache the content type on first call since its not going to change
        private string _contentType;
        private string ContentType 
        {
            get
            {
                if(string.IsNullOrEmpty(_contentType))
                {
                    _contentType = Client.Encryption.IsEnabled ? "application/octet-stream" : "application/json";
                }
                return _contentType;
            }
        }

        public HttpRequestAdapter(HttpClient httpClient)
        {
            _httpClient = httpClient;

            // remove cap of max timeout on HttpClient from 100 seconds.
            _httpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        }

        /// <inheritdoc cref="IHttpAdapter"/>
        public async Task<byte[]> SendAsync(string method, Uri uri, IDictionary<string, string> headers, byte[] body,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = new HttpMethod(method),
                Headers =
                {
                    Accept = {new MediaTypeWithQualityHeaderValue(ContentType)}
                }
            };

            foreach (var kv in headers)
            {
                request.Headers.Add(kv.Key, kv.Value);
            }

            if (body != null)
            {
                request.Content = new ByteArrayContent(body);
                request.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(ContentType);
            }

            Client.Logger?.InfoFormat("Send: method='{0}', uri='{1}', body='{2}'", method, uri, body);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            response.Content?.Dispose();

            Client.Logger?.InfoFormat("Received: method='{0}', uri='{1}', status='{2}'", method, uri, response.StatusCode);

            if (((int)response.StatusCode) >= 500)
            {
                // TODO think of best way to map HTTP code to GRPC code since we can't rely
                // on server to process it. Manually adding the mapping to SDK seems brittle.
                throw new ApiResponseException((int) response.StatusCode, Client.Encryption.Decrypt(bytes), -1);
            }

            if (response.IsSuccessStatusCode)
            {
                return bytes;
            }

            var decoded = Client.JsonSerializer.FromJson<Dictionary<string, object>>(Client.Encryption.Decrypt(bytes));
            if (decoded == null)
                decoded = new Dictionary<string, object>();
            string message = decoded.ContainsKey("message") ? decoded["message"].ToString() : string.Empty;
            int grpcCode = decoded.ContainsKey("code") ? (int) decoded["code"] : -1;

            var exception = new ApiResponseException((int) response.StatusCode, message, grpcCode);

            if (decoded.ContainsKey("error"))
            {
                IHttpAdapterUtil.CopyResponseError(this, decoded["error"], exception);
            }

            throw exception;
        }

        /// <summary>
        /// A new HTTP adapter with configuration for gzip support in the underlying HTTP client.
        /// </summary>
        /// <remarks>
        /// NOTE Decompression does not work with Mono AOT on Android.
        /// </remarks>
        /// <param name="decompression">If automatic decompression should be enabled with the HTTP adapter.</param>
        /// <param name="compression">If automatic compression should be enabled with the HTTP adapter.</param>
        /// <returns>A new HTTP adapter.</returns>
        public static IHttpAdapter WithGzip(bool decompression = false, bool compression = false)
        {
            var handler = new HttpClientHandler();
            if (handler.SupportsAutomaticDecompression && decompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
            handler.AllowAutoRedirect = true;

            var client =
                new HttpClient(compression ? (HttpMessageHandler) new GZipHttpClientHandler(handler) : handler);
            return new HttpRequestAdapter(client);
        }

        private static bool IsTransientException(Exception e)
        {
            if (e is ApiResponseException apiException)
            {
                switch (apiException.StatusCode)
                {
                    case 500: // Internal Server Error often (but not always) indicates a transient issue in Nakama, e.g., DB connectivity.
                    case 502: // LB returns this to client if server sends corrupt/invalid data to LB, which may be a transient issue.
                    case 503: // LB returns this to client if LB determines or is told that server is unable to handle forwarded from LB, which may be a transient issue.
                    case 504: // LB returns this to client if LB cannot communicate with server, which may be a temporary issue.
                        return true;
                }
            }

            return false;
        }
    }
}

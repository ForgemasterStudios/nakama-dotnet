/**
 * Copyright 2019 The Nakama Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Nakama
{
    /// <summary>
    /// HTTP Request adapter which uses the .NET HttpClient to send requests.
    /// </summary>
    public class HttpRequestAdapter : IHttpAdapter
    {
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
        public async Task<byte[]> SendAsync(string method, Uri uri, IDictionary<string, string> headers, byte[] body, int timeout)
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

            var timeoutToken = new CancellationTokenSource();
            timeoutToken.CancelAfter(TimeSpan.FromSeconds(timeout));

            Client.Logger?.InfoFormat("Send: method='{0}', uri='{1}'", method, uri);

            var response = await _httpClient.SendAsync(request, timeoutToken.Token);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            response.Content?.Dispose();

            Client.Logger?.InfoFormat("Received: method='{0}', uri='{1}', status='{2}'", method, uri, response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                return bytes;
            }
            
            // Special case for when the result is not successs, throw an error
            var decoded = Client.JsonSerializer.FromJson<Dictionary<string, object>>(Client.Encryption.Decrypt(bytes));
            string message = decoded.ContainsKey("message") ? decoded["message"].ToString() : string.Empty;
            int grpcCode = -1;
            if(decoded.ContainsKey("code"))
            {
                try
                {
                    grpcCode = Convert.ToInt32(decoded["code"]);
                }
                catch (System.Exception){}
            }

            var exception = new ApiResponseException((int)response.StatusCode, message, grpcCode);

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

            var client =
                new HttpClient(compression ? (HttpMessageHandler)new GZipHttpClientHandler(handler) : handler);
            return new HttpRequestAdapter(client);
        }
    }
}

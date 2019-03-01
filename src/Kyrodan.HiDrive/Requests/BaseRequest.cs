using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kyrodan.HiDrive.Requests
{
    public class BaseRequest : IBaseRequest
    {
        public IBaseClient Client { get; private set; }

        public string RequestUrl { get; protected set; }
        public IList<KeyValuePair<string, string>> QueryOptions { get; protected set; }

        public string Method { get; internal set; }

        public BaseRequest(string requestUrl, IBaseClient client)
        {
            RequestUrl = requestUrl;
            Client = client;

            Method = "GET";
            QueryOptions = new List<KeyValuePair<string, string>>();
        }

        public HttpRequestMessage GetHttpRequestMessage()
        {
            var queryString = BuildQueryString();
            var request = new HttpRequestMessage(new HttpMethod(Method),
                string.Concat(RequestUrl, queryString));

            return request;
        }

        internal string BuildQueryString()
        {
            if (QueryOptions == null) return null;

            var stringBuilder = new StringBuilder();

            foreach (var queryOption in QueryOptions)
            {
                if (stringBuilder.Length == 0)
                {
                    stringBuilder.AppendFormat("?{0}={1}", queryOption.Key, queryOption.Value);
                }
                else
                {
                    stringBuilder.AppendFormat("&{0}={1}", queryOption.Key, queryOption.Value);
                }
            }

            return stringBuilder.ToString();
        }

        public string ContentType { get; set; }

        public async Task SendAsync(object serializableObject, CancellationToken cancellationToken,
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
        {
            using (var response = 
                    await SendRequestAsync(serializableObject, cancellationToken, completionOption).ConfigureAwait(false)
                )
            {
                if (response.IsSuccessStatusCode)
                    return;

                string responseString = null;
                if (response.Content != null)
                {
                    responseString = await response.Content.ReadAsStringAsync();
                }

                var error = responseString != null ? JsonConvert.DeserializeObject<ServiceError>(responseString) : new ServiceError() { Code = "unknown" };
                throw new ServiceException(error);

            }
        }

        public async Task<T> SendAsync<T>(object serializableObject, CancellationToken cancellationToken,
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
        {
            using (
                var response =
                    await
                        SendRequestAsync(serializableObject, cancellationToken, completionOption)
                            .ConfigureAwait(false))
            {

                string responseString = null;
                if (response.Content != null)
                {
                    responseString = await response.Content.ReadAsStringAsync();
                }

                if (response.IsSuccessStatusCode)
                    return responseString != null ? JsonConvert.DeserializeObject<T>(responseString) : default(T);

                var error = responseString != null ? JsonConvert.DeserializeObject<ServiceError>(responseString) : new ServiceError() {Code = "unknown"};
                throw new ServiceException(error);
            }
        }

        public async Task<Stream> SendStreamAsync(object serializableObject, CancellationToken cancellationToken, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
        {
            var response = await SendRequestAsync(serializableObject, cancellationToken, completionOption);

            if (response.StatusCode == HttpStatusCode.OK)
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var responseString = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<ServiceError>(responseString);
            throw new ServiceException(error);
        }

        private async Task<HttpResponseMessage> SendRequestAsync(object serializableObject, CancellationToken cancellationToken, HttpCompletionOption completionOption)
        {

            if (string.IsNullOrEmpty(RequestUrl))
            {
                throw new ServiceException(
                    new ServiceError
                    {
                        Code = "invalidRequest",
                        Message = "Request URL is required to send a request.",
                    });
            }

            using (var request = GetHttpRequestMessage())
            {
                await Client.AuthenticateRequestAsync(request).ConfigureAwait(false);

                if (serializableObject != null)
                {
                    if (serializableObject is Stream inputStream)
                    {
                        request.Content = new StreamContent(inputStream);
                    }
                    else
                    {
                        request.Content = new StringContent(JsonConvert.SerializeObject(serializableObject));
                    }

                    if (!string.IsNullOrEmpty(ContentType))
                    {
                        request.Content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
                    }
                }

                return await Client.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
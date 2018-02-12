using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Lib.Data
{
    public static class Http
    {
        /// <summary>
        /// Makes Post reqest using WebClient
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public static string Post(string uri, NameValueCollection pairs)
        {
            pairs = pairs ?? new NameValueCollection();
            string response = null;
            using (WebClient client = new WebClient())
            {
                var bytes = client.UploadValues(uri, pairs);
                response = Encoding.UTF8.GetString(bytes);
            }
            return response;
        }

        public static HttpResponseMessage Post(string uri, MultipartFormDataContent form, AuthenticationHeaderValue authorization = null)
        { 
            using (var httpClient = new HttpClient())
            {
                if (authorization != null)
                {
                    httpClient.DefaultRequestHeaders.Authorization = authorization;
                }
                var response = httpClient.PostAsync(uri, form);
                response.Wait();
                return response.Result;
            }
        }

        public static HttpResponseMessage PostJson(string uri, object json, AuthenticationHeaderValue authorization = null)
        {
            using (var httpClient = new HttpClient())
            {
                if (authorization != null)
                {
                    httpClient.DefaultRequestHeaders.Authorization = authorization;
                }

                var content = new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content);
                response.Wait();
                return response.Result;
            }
        }



        /// <summary>
        /// Makes Get request using WebClient
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public static string Get(string uri, NameValueCollection pairs)
        {
            pairs = pairs ?? new NameValueCollection();
            string query = "";
            for (var i = 0; i < pairs.AllKeys.Length; i++)
            {
                query += string.IsNullOrWhiteSpace(query) ? "?" : "&";
                query += pairs.GetKey(i);
                query += "=";
                var value = pairs.GetValues(i)?.FirstOrDefault();
                query += !string.IsNullOrWhiteSpace(value) ? value : "";
            }

            string response = null;
            using (WebClient client = new WebClient())
            {
                response = client.DownloadString(uri + query);
            }
            return response;
        }
    }
}

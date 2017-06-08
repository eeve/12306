using System;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using OSS.Http.Mos;
using OSS.Http.Extention;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

namespace test1
{
    class HttpUtil
    {

        public async Task<string> Get(string url)
        {
            Task<HttpResponseMessage> task = GetResp(url);
            var response = task.Result;
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    foreach (var i in response.Headers.GetValues("X-Rate-Limit-Remaining"))
                    {
                        Console.WriteLine($"==== 剩余请求次数：{i} ====");
                    }
                }
                catch { }
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        public async Task<HttpResponseMessage> GetResp(string url)
        {
            var reqHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
                UseCookies = false,
                UseProxy = false
            };
            reqHandler.ServerCertificateCustomValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            HttpClient client = new HttpClient(reqHandler);

            var req = new OsHttpRequest();
            // req.RequestSet = msg => msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            req.HttpMothed = HttpMothed.GET;
            req.AddressUrl = url;

            return await req.RestSend(client);
        }

    }
}

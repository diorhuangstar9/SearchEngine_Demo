using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Gather.Services
{   
    public interface IMyHttpService
    {
        Task<string> GetPage(string link);
    }

    public class MyHttpService : IMyHttpService
    {
        private readonly IHttpClientFactory _clientFactory;

        public MyHttpService(IHttpClientFactory clientFactory)  
        {
            _clientFactory = clientFactory;
        }

        public async Task<string> GetPage(string link)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, link);
            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Encoding encoding;
                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    encoding = Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet);
                }
                catch
                {
                    encoding = Encoding.Default;
                }
                return encoding.GetString(await response.Content.ReadAsByteArrayAsync());
            }
            else
            {
                throw new Exception($"StatusCode: {response.StatusCode}");
            }
        }
    }

}

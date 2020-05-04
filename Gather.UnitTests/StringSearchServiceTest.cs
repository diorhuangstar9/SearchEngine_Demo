using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Gather.Services;
using Xunit;

namespace Gather.UnitTests
{

    public class StringSearchServiceTest
    {
        private readonly StringSearchService _stringSearchService;
        const int _max = 1000000;

        public StringSearchServiceTest()
        {
            _stringSearchService = new StringSearchService();
        }

        [Theory]
        [InlineData("abcacabdc", "abd")]
        public void BM_Search_Test1(string content, string pattern)
        {
            // compare BM_Search and string.IndexOf result
            CompareBM_SearchAndProto(content, pattern);
        }

        [Theory]
        [InlineData("https://www.qq.com")]
        [InlineData("https://www.sina.com.cn")]
        public void BM_Search_Test2(string link)
        {
            //get the page link, simulate the real scenario
            var content = string.Empty;
            var request = (HttpWebRequest)WebRequest.Create(link);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                content = reader.ReadToEnd();
            }

            var pattern1 = "<a href=\"";
            var pattern2 = "\"";
            CompareBM_SearchAndProto(content, pattern1, pattern2);
        }

        private void CompareBM_SearchAndProto(string content, string pattern1, string pattern2 = "")
        {
            var startIdx = 0;
            pattern2 = string.IsNullOrWhiteSpace(pattern2) ? pattern1 : pattern2;
            while (startIdx < content.Length)
            {
                var s1 = Stopwatch.StartNew();
                var pattern1StartIndex = _stringSearchService.Search(content, pattern1, startIdx) + pattern1.Length;
                s1.Stop();
                var s2 = Stopwatch.StartNew();
                var pattern1StartIndex_proto = content.IndexOf(pattern1, startIdx) + pattern1.Length;
                s2.Stop();
                Console.WriteLine("s1:" + ((double)(s1.Elapsed.TotalMilliseconds * 1000000) / _max).ToString("0.00 ns"));
                Console.WriteLine("s2:" + ((double)(s2.Elapsed.TotalMilliseconds * 1000000) / _max).ToString("0.00 ns"));
                Assert.Equal(pattern1StartIndex, pattern1StartIndex_proto);
                if (pattern1StartIndex < pattern1.Length)
                    break;
                var pattern2EndIndex = _stringSearchService.Search(content, pattern2, pattern1StartIndex);
                var pattern2EndIndex_proto = content.IndexOf(pattern2, pattern1StartIndex);
                Assert.Equal(pattern2EndIndex, pattern2EndIndex_proto);
                if (pattern2EndIndex <= -1)
                    break;
                startIdx = pattern2EndIndex + pattern2.Length;
            }
        }

    }
}

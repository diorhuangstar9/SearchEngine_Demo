using System;
using Gather.Services;
using Xunit;

namespace Gather.UnitTests
{
    public class BloomFilterTest
    {
        private readonly BloomFilterService _bloomFilterService;

        public BloomFilterTest()
        {
            _bloomFilterService = new BloomFilterService(10);
        }

        [Fact]
        public void BloomFilter_Test1()
        {
            var testTarget = new string[] { "https://www.qq.com", "https://www.sina.com.cn", "https://www.163.com" };
            foreach(var target in testTarget)
            {
                Assert.False(_bloomFilterService.CheckExists(target));
            }
            foreach (var target in testTarget)
            {
                Assert.True(_bloomFilterService.CheckExists(target));
            }
        }
    }
}

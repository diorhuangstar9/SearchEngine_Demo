using System;
using System.Collections.Generic;

namespace Gather.Services
{
    public interface IStringSearchService
    {
        int Search(string content, string pattern, int start = 0, string algorithm = "BM");
    }

    public class StringSearchService : IStringSearchService
    {
        private Dictionary<string, BMPatternCache> bmPatternCacheDict = new Dictionary<string, BMPatternCache>();

        public int Search(string content, string pattern, int start = 0, string algorithm = "BM")
        {
            if (algorithm == "BM")
                return BM_Search(content, pattern, start);
            throw new NotSupportedException("The algorithm is not supported");
        }

        private int BM_Search(string content, string pattern, int start = 0)
        {
            // search for pattern cache
            // if no cache, build pattern, put into cache
            if (!bmPatternCacheDict.TryGetValue(pattern, out BMPatternCache bmCache))
            {
                bmCache = BuildBMPatternCache(pattern);
                bmPatternCacheDict.TryAdd(pattern, bmCache);
            }
            var suffix = bmCache.Suffix;
            var prefix = bmCache.Prefix;
            // do the good suffix(temporarily abandon bad char)
            var i = start + pattern.Length - 1;
            while (i < content.Length)
            {
                var j = pattern.Length - 1;
                while (j >= 0 && content[i] == pattern[j])
                {
                    i--;
                    j--;
                }
                if (j < 0)
                    return i + 1;
                var len = pattern.Length - 1 - j;
                if (len <= 0)
                    i += 1;
                else if (suffix[len] != -1)
                {
                    i += j + 1 - suffix[len];
                }
                else
                {
                    while (len > 0 && !prefix[len])
                        len--;
                    i += pattern.Length - len;
                }
            }
            return -1;
        }

        private BMPatternCache BuildBMPatternCache(string pattern)
        {
            var m = pattern.Length;
            var suffix = new int[m];
            for (var i = 0; i < m; i++)
            {
                suffix[i] = -1;
            }
            var prefix = new bool[m];
            for (var i = 0; i < m - 1; i++)
            {
                var k = m - 1;
                var j = i;
                for (; j >= 0 && pattern[j] == pattern[k]; j--)
                {
                    suffix[m - k] = j;
                    k--;
                }
                if (j == -1)
                    prefix[i + 1] = true;
            }

            return new BMPatternCache { Suffix = suffix, Prefix = prefix };
        }

        class BMPatternCache
        {
            public int[] Suffix { get; set; }
            public bool[] Prefix { get; set; }
        }
    }
}

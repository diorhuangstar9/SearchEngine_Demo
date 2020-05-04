using System;
using System.Collections.Generic;

namespace SearchEngine_Demo.Model
{
    public class BuildCondition
    {
        public IEnumerable<string> SeededPages { get; set; }
        public int? PageCountLimit { get; set; }
    }
}

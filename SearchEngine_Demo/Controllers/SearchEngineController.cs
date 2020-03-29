using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SearchEngine_Demo.Model;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SearchEngine_Demo.Controllers
{
    [Route("api/[controller]")]
    public class SearchEngineController : Controller
    {
        private static readonly int DEFAULT_PAGE_COUNT_LIMIT = 5000;
        
        [HttpGet]
        public string Search()
        {
            return "value";
        }

        [HttpPost("Build")]
        public void Build([FromBody]BuildCondition buildCondition)
        {
            if (buildCondition.PageCountLimit.HasValue)
                buildCondition.PageCountLimit = DEFAULT_PAGE_COUNT_LIMIT;

        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

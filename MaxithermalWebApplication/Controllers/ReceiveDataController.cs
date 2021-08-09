using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace MaxithermalWebApplication.Controllers
{
    public class ReceiveDataController : ApiController
    {
        // GET: api/ReceiveData
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/ReceiveData/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/ReceiveData
        public string Post([FromBody]JObject value)
        {
            
            return "OK";
        }

        // PUT: api/ReceiveData/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/ReceiveData/5
        public void Delete(int id)
        {
        }
    }
}

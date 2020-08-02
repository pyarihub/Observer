using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapService
{
    public class AmazonSettings
    {
        public string accessKey { get; set; }
        public string secretKey { get; set; }
        public string tableName { get; set; }
        public string regionName { get; set; }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace tdp_update_agent.Models
{
    class GetResults
    {
        public Result[] results { get; set; }

    }

    class Result
    {
        public string sampleId { get; set; }
        public string dateTime { get; set; }
        public string uniqueId { get; set; }
    }
}

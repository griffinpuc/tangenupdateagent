using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace tdp_update_agent.Models
{
    class GetResults
    {
        public int total { get; set; }
        public int limit { get; set; }
        public int offset { get; set; }
        public Result[] runs { get; set; }

    }

    class Result
    {
        public string sampleId { get; set; }
        public string dateTime { get; set; }
        public string uniqueId { get; set; }
    }
}

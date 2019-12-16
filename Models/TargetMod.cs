using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tdp_update_agent.Models
{
    public class TargetMod
    {

        public int Id { get; set; }
        public string targetId { get; set; }
        public string name { get; set; }
        public string outcomeCode { get; set; }
        public string outcomeString { get; set; }

    }
}

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace tdp_update_agent.Models
{
    public class RunMod
    {
        public int Id { get; set; }
        public string sampleId { get; set; }
        public string uniqueId { get; set; }
        public string dateTime { get; set; }
        public string assayId { get; set; }
        public string assayName { get; set; }
        public string kitLotId { get; set; }
        public string instrumentUuid { get; set; }
        public string instrumentName { get; set; }

        public virtual ICollection<ResultMod> results { get; set; }
        public virtual ICollection<TargetMod> targets { get; set; }
        public virtual ICollection<WellMod> wells { get; set; }

        [JsonIgnore]
        public string directoryPath { get; set; }
        [JsonIgnore]
        public string fileName { get; set; }

        public string getJSON()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    public class ResultMod
    {

        public int Id { get; set; }
        public string name { get; set; }
        public string outcomeCode { get; set; }
        public string outcomeString { get; set; }

    }

    public class TargetMod
    {

        public int Id { get; set; }
        public string targetId { get; set; }
        public string name { get; set; }
        public string outcomeCode { get; set; }
        public string outcomeString { get; set; }

    }

    public class WellMod
    {

        public int Id { get; set; }
        public string wellId { get; set; }
        public string species { get; set; }
        public string cq { get; set; }
    }
}
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
        public string DirPointer { get; set; }

        public string getJSON()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
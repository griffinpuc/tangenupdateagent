using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace tdp_update_agent.Models
{
    public class InstrumentMod
    {

        public int ID { get; set; }
        public string name { get; set; }
        public string localAddress { get; set; }
        public string status { get; set; }
        public string lastPing { get; set; }
        public DateTime dateAdded { get; set; }
        public bool isActive { get; set; }

        [JsonIgnore]
        [NotMapped]
        public CancellationTokenSource token { get; set; }

        public string getColor()
        {
            if (this.status.Equals("IDLE"))
            {
                return ("#7fba00");
            }
            else if (this.status.Equals("BUSY"))
            {
                return ("#ffb900");
            }
            else
            {
                return ("#f25022");
            }
        }

    }
}

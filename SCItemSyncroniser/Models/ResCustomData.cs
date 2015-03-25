using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SCItemSyncroniser.Models
{
    [DataContract]
    class ResCustomData
    {
        [DataMember]
        public List<string> ResLinks { get; set; }
        [DataMember]
        public string Text { get; set; }

        public ResCustomData()
        {
            ResLinks = new List<string>();
            Text = "";
        }
    }
}

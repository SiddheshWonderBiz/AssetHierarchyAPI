using AssetHierarchyAPI.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class SignalValue
    {
        public int Id { get; set; }
        public int SignalId { get; set; }
        [JsonIgnore]
        public Signal? Signal { get; set; }

        public double Value { get; set; }
    }
}

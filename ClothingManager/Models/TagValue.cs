using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingManager.Models
{
    public class TagValue
    {
        public int ClothingId { get; set; }
        public int TagKeyId { get; set; }
        public string Value { get; set; }

        public TagKey TagKey { get; set; }
        public Clothing Clothing { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingManager.Models
{
    public class TagKey
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public string Values { get; set; }

        public ICollection<TagValue> TagValues { get; set; }
    }
}

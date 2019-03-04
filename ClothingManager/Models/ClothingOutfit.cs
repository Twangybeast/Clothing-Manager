using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingManager.Models
{
    public class ClothingOutfit
    {
        public int Order { get; set; }

        public int ClothingId { get; set; }
        public int OutfitId { get; set; }

        public Clothing Clothing { get; set; }
        public Outfit Outfit { get; set; }
    }
}

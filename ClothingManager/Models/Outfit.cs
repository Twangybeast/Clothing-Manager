using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingManager.Models
{
    public class Outfit
    {
        public int ID { get; set; }
        public string Name { get; set; }

        
        public ICollection<ClothingOutfit> ClothingOutfits { get; set; }

        [Display(Name = "Clothings")]
        public ICollection<Clothing> Clothings { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingManager.Models
{
    public class GradientItemViewModel
    {
        public Clothing Clothing { get; set; }
        public (float, float, float) Position { get; set; }
    }
}

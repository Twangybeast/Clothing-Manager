using ClothingManager.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingManager.Models
{
    public class Clothing 
    {
        public int ID { get; set; }
        public string Name { get; set; }


        [Display(Name="Color")]
        public Int32 Argb
        {
            get
            {
                return Color.ToArgb() & 0xFFFFFF;
            }
            set
            {
                Color = Color.FromArgb(((int)value % 0x01000000) | unchecked((int)0xFF000000));
            }
        }

        [NotMapped]
        public Color Color { get; set; }

        [Display(Name="Image")]
        public string ImagePath { get; set; }
        //public DateTime? AcquiredTime { get; set; }

        [Display(Name = "Tags")]
        public ICollection<TagValue> TagValues { get; set; }
    }
}

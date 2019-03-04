using ClothingManager.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingManager.Data
{
    public class ClothingContext : DbContext
    {
        public ClothingContext(DbContextOptions<ClothingContext> options) : base(options)
        {
        }


        public DbSet<Clothing> Clothings { get; set; }
        public DbSet<TagKey> TagKeys { get; set; }
        public DbSet<TagValue> TagValues { get; set; }

        public DbSet<Outfit> Outfits { get; set; }
        public DbSet<ClothingOutfit> ClothingOutfits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TagValue>()
                .HasKey(t => new { t.TagKeyId, t.ClothingId });
            modelBuilder.Entity<ClothingOutfit>()
                .HasKey(co => new { co.ClothingId, co.OutfitId });
        }
    }
}

﻿// <auto-generated />
using ClothingManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClothingManager.Migrations
{
    [DbContext(typeof(ClothingContext))]
    [Migration("20190218065331_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("ClothingManager.Models.Clothing", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("Argb");

                    b.Property<string>("ImagePath");

                    b.Property<string>("Name");

                    b.HasKey("ID");

                    b.ToTable("Clothings");
                });

            modelBuilder.Entity("ClothingManager.Models.TagKey", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Key");

                    b.Property<string>("Values");

                    b.HasKey("ID");

                    b.ToTable("TagKeys");
                });

            modelBuilder.Entity("ClothingManager.Models.TagValue", b =>
                {
                    b.Property<int>("TagKeyId");

                    b.Property<int>("ClothingId");

                    b.Property<string>("Value");

                    b.HasKey("TagKeyId", "ClothingId");

                    b.HasIndex("ClothingId");

                    b.ToTable("TagValues");
                });

            modelBuilder.Entity("ClothingManager.Models.TagValue", b =>
                {
                    b.HasOne("ClothingManager.Models.Clothing", "Clothing")
                        .WithMany("TagValues")
                        .HasForeignKey("ClothingId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ClothingManager.Models.TagKey", "TagKey")
                        .WithMany()
                        .HasForeignKey("TagKeyId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}

﻿// <auto-generated />
using MemesApi.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace MemesApi.Migrations
{
    [DbContext(typeof(MemeContext))]
    partial class MemeContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.10");

            modelBuilder.Entity("MemesApi.Db.Models.Estimate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClientId")
                        .HasColumnType("TEXT");

                    b.Property<int>("FileId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Score")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Estimates");
                });

            modelBuilder.Entity("MemesApi.Db.Models.MemeFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Files");
                });
#pragma warning restore 612, 618
        }
    }
}
﻿// <auto-generated />
using System;
using DotNetCoreSqlDb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DotNetCoreSqlDb.Migrations
{
    [DbContext(typeof(MyDatabaseContext))]
    [Migration("20240918042929_includeWagerAndDegenerate")]
    partial class includeWagerAndDegenerate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("DotNetCoreSqlDb.Models.Degenerate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("BetsPlaced")
                        .HasColumnType("int");

                    b.Property<int>("BetsWon")
                        .HasColumnType("int");

                    b.Property<double>("CashWallet")
                        .HasColumnType("float");

                    b.Property<double>("TotalWagesPlaced")
                        .HasColumnType("float");

                    b.Property<double>("TotalWagesWon")
                        .HasColumnType("float");

                    b.Property<string>("Username")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Degenerate");
                });

            modelBuilder.Entity("DotNetCoreSqlDb.Models.Todo", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"));

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.ToTable("Todo");
                });

            modelBuilder.Entity("DotNetCoreSqlDb.Models.WagerItems.WagerItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Active")
                        .HasColumnType("bit");

                    b.Property<int>("BetType")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("MatchId")
                        .HasColumnType("int");

                    b.Property<int>("Odds")
                        .HasColumnType("int");

                    b.Property<bool?>("Outcome")
                        .HasColumnType("bit");

                    b.Property<int>("SportType")
                        .HasColumnType("int");

                    b.Property<DateTime>("TimeOfBet")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<int>("WagerJuice")
                        .HasColumnType("int");

                    b.Property<string>("WagerTeamA")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("WagerTeamB")
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("WagerValue")
                        .HasColumnType("float");

                    b.HasKey("Id");

                    b.ToTable("WagerItem");
                });
#pragma warning restore 612, 618
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Configuration; // No longer needed here
using SAOL_DATABSE_INSERT.Models;

namespace SAOL_DATABSE_INSERT.Data;

public partial class saol_dataContext : DbContext
{
    public saol_dataContext()
    {
    }

    public saol_dataContext(DbContextOptions<saol_dataContext> options)
        : base(options)
    {
    }

    public virtual DbSet<adjective_entry> adjective_entries { get; set; }
    public virtual DbSet<adjective_form> adjective_forms { get; set; }
    public virtual DbSet<adverb_entry> adverb_entries { get; set; }
    public virtual DbSet<adverb_form> adverb_forms { get; set; }
    public virtual DbSet<noun_entry> noun_entries { get; set; }
    public virtual DbSet<noun_form> noun_forms { get; set; }
    public virtual DbSet<verb_entry> verb_entries { get; set; }
    public virtual DbSet<verb_form> verb_forms { get; set; }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<adjective_entry>(entity =>
        {
            entity.HasKey(e => e.id).HasName("adjective_entry_pkey");
            entity.ToTable("adjective_entry");
            entity.Property(e => e.id).ValueGeneratedOnAdd(); // Ensure ID is treated as auto-generated
            entity.Property(e => e._class).HasColumnName("class").IsRequired(); // Assuming class is not nullable
        });

        modelBuilder.Entity<adjective_form>(entity =>
        {
            entity.HasKey(e => new { e.entry_id, e.degree, e.form }).HasName("adjective_form_pkey");
            entity.ToTable("adjective_form");

            entity.Property(e => e.entry_id).HasColumnName("entry_id");
            entity.Property(e => e.degree).HasColumnName("degree");
            entity.Property(e => e.form).HasColumnName("form");

            entity.HasOne(d => d.entry).WithMany(p => p.adjective_forms)
                .HasForeignKey(d => d.entry_id)
                .OnDelete(DeleteBehavior.Cascade) // CHANGED from ClientSetNull
                .HasConstraintName("adjective_form_entry_id_fkey");
        });

        modelBuilder.Entity<adverb_entry>(entity =>
        {
            entity.HasKey(e => e.id).HasName("adverb_entry_pkey");
            entity.ToTable("adverb_entry");
            entity.Property(e => e.id).ValueGeneratedOnAdd();
            entity.Property(e => e._class).HasColumnName("class").IsRequired();
        });

        modelBuilder.Entity<adverb_form>(entity =>
        {
            entity.HasKey(e => new { e.entry_id, e.form }).HasName("adverb_form_pkey");
            entity.ToTable("adverb_form");

            entity.Property(e => e.entry_id).HasColumnName("entry_id");
            entity.Property(e => e.form).HasColumnName("form");

            entity.HasOne(d => d.entry).WithMany(p => p.adverb_forms)
                .HasForeignKey(d => d.entry_id)
                .OnDelete(DeleteBehavior.Cascade) // CHANGED from ClientSetNull
                .HasConstraintName("adverb_form_entry_id_fkey");
        });
        

        modelBuilder.Entity<noun_entry>(entity =>
        {
            entity.HasKey(e => e.id).HasName("noun_entry_pkey");
            entity.ToTable("noun_entry");
            entity.Property(e => e.id).ValueGeneratedOnAdd();
            entity.Property(e => e._class).HasColumnName("class").IsRequired();
        });

        modelBuilder.Entity<noun_form>(entity =>
        {
            entity.HasKey(e => new { e.entry_id, e.number, e.form }).HasName("noun_form_pkey");
            entity.ToTable("noun_form");

            entity.Property(e => e.entry_id).HasColumnName("entry_id");
            entity.Property(e => e.number).HasColumnName("number");
            entity.Property(e => e.form).HasColumnName("form");

            entity.HasOne(d => d.entry).WithMany(p => p.noun_forms)
                .HasForeignKey(d => d.entry_id)
                .OnDelete(DeleteBehavior.Cascade) // CHANGED from ClientSetNull
                .HasConstraintName("noun_form_entry_id_fkey");
        });

        modelBuilder.Entity<verb_entry>(entity =>
        {
            entity.HasKey(e => e.id).HasName("verb_entry_pkey");
            entity.ToTable("verb_entry");
            entity.Property(e => e.id).ValueGeneratedOnAdd();
            entity.Property(e => e._class).HasColumnName("class").IsRequired();
        });

        modelBuilder.Entity<verb_form>(entity =>
        {
            entity.HasKey(e => new { e.entry_id, e.section, e.form }).HasName("verb_form_pkey");
            entity.ToTable("verb_form");

            entity.Property(e => e.entry_id).HasColumnName("entry_id");
            entity.Property(e => e.section).HasColumnName("section");
            entity.Property(e => e.form).HasColumnName("form");

            entity.HasOne(d => d.entry).WithMany(p => p.verb_forms)
                .HasForeignKey(d => d.entry_id)
                .OnDelete(DeleteBehavior.Cascade) // CHANGED from ClientSetNull
                .HasConstraintName("verb_form_entry_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
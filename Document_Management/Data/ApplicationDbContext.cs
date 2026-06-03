using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DocumentManagementApp.Models;

namespace DocumentManagementApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet for Documents table
        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Document entity
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("Documents");
                
                entity.Property(d => d.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(d => d.FilePath)
                    .IsRequired();

                entity.Property(d => d.OcrStatus)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Pending");

                entity.Property(d => d.IsProcessed)
                    .HasDefaultValue(false);

                // Optional: Add index for better query performance
                entity.HasIndex(d => d.UserId);
                entity.HasIndex(d => d.OcrStatus);
            });
        }
    }
}

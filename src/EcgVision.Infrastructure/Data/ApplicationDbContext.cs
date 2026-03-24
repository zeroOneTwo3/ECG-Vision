using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcgVision.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<EcgSignal> EcgSignals { get; set; }

    public DbSet<EcgJob> EcgJobs => Set<EcgJob>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Custom configuration for your biometric data could go here
        // For example, ensuring Email is always required and unique:
        builder.Entity<IdentityUser>(entity =>
        {
            entity.Property(u => u.Email).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        builder.Entity<EcgSignal>(entity =>
        {
            entity.HasOne(s => s.Patient)
                  .WithMany()
                  .HasForeignKey(s => s.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(s => s.Jobs)
                  .WithOne(j => j.Signal)
                  .HasForeignKey(j => j.SignalId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EcgJob>(entity =>
        {
            entity.ToTable("EcgJobs");

            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);

            // Map C# Enum to Database String
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .HasDefaultValue(EcgJobStatus.None);

            // TIMESTAMPS: Ensure SQL handles the date if C# doesn't
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // INDEXES: Critical for Background Worker performance
            entity.HasIndex(e => e.Status); // Helps the worker find "Pending" jobs without a full table scan
            entity.HasIndex(e => e.CreatedAt);

            // CONCURRENCY: RowVersion is already handled by [Timestamp] attribute, but can be explicit here if you prefer
            entity.Property(e => e.RowVersion)
                  .IsRowVersion();
        });
    }
}
using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Data;

public class AXDbContext : DbContext
{
    public AXDbContext(DbContextOptions<AXDbContext> options) : base(options)
    {
    }

    public DbSet<BatchJob> BatchJobs { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<BlockingChain> BlockingChains { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<SqlHealth> SqlHealthRecords { get; set; }
    public DbSet<RemediationRuleEntity> RemediationRules { get; set; }
    public DbSet<RemediationExecutionEntity> RemediationExecutions { get; set; }
    public DbSet<Baseline> Baselines { get; set; }
    public DbSet<MaintenanceWindow> MaintenanceWindows { get; set; }
    public DbSet<BatchJobHistoryAnalysis> BatchJobHistoryAnalyses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure BatchJob
        modelBuilder.Entity<BatchJob>(entity =>
        {
            entity.ToTable("BatchJobs");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BatchJobId).IsUnique();
            entity.Property(e => e.BatchJobId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.AosServer).HasMaxLength(100);
        });

        // Configure Session
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Sessions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.Property(e => e.SessionId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AosServer).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Database).HasMaxLength(100);
        });

        // Configure BlockingChain
        modelBuilder.Entity<BlockingChain>(entity =>
        {
            entity.ToTable("BlockingChains");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BlockingSessionId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BlockedSessionId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BlockingType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Resource).HasMaxLength(255);
            entity.Property(e => e.SqlText).HasMaxLength(4000);
        });

        // Configure Alert
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("Alerts");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AlertId).IsUnique();
            entity.Property(e => e.AlertId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Severity).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
        });

        // Configure SqlHealth
        modelBuilder.Entity<SqlHealth>(entity =>
        {
            entity.ToTable("SqlHealthRecords");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RecordedAt);
        });

        // Configure RemediationRule
        modelBuilder.Entity<RemediationRuleEntity>(entity =>
        {
            entity.ToTable("RemediationRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            // TriggerConditions and Actions are TEXT by default in SQLite
            entity.HasIndex(e => e.Enabled);
        });

        // Configure RemediationExecution
        modelBuilder.Entity<RemediationExecutionEntity>(entity =>
        {
            entity.ToTable("RemediationExecutions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            // TriggerData, ActionsExecuted, and ResultData are TEXT by default in SQLite
            entity.HasIndex(e => e.RuleId);
            entity.HasIndex(e => e.StartTime);
        });

        // Configure Baseline
        modelBuilder.Entity<Baseline>(entity =>
        {
            entity.ToTable("Baselines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MetricName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.MetricType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MetricClass).HasMaxLength(255);
            entity.Property(e => e.Environment).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => new { e.MetricName, e.MetricType, e.MetricClass, e.Environment });
            entity.HasIndex(e => e.BaselineDate);
        });

        // Configure MaintenanceWindow
        modelBuilder.Entity<MaintenanceWindow>(entity =>
        {
            entity.ToTable("MaintenanceWindows");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.RecurrencePattern).HasMaxLength(50);
            entity.Property(e => e.DayOfWeek).HasMaxLength(20);
            entity.Property(e => e.Environment).HasMaxLength(50);
            entity.HasIndex(e => e.Enabled);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.EndTime);
        });

        // Configure BatchJobHistoryAnalysis
        modelBuilder.Entity<BatchJobHistoryAnalysis>(entity =>
        {
            entity.ToTable("BatchJobHistoryAnalyses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Caption).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ErrorReason).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.ErrorCategory).HasMaxLength(100);
            entity.Property(e => e.ErrorSeverity).HasMaxLength(50);
            entity.Property(e => e.ErrorAnalysis).HasMaxLength(4000);
            entity.Property(e => e.ErrorSuggestions).HasMaxLength(4000);
            entity.HasIndex(e => new { e.Caption, e.CreatedDateTime });
            entity.HasIndex(e => e.AnalyzedAt);
        });
    }
}


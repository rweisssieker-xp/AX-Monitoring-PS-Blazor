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
    public DbSet<PerformanceBudget> PerformanceBudgets { get; set; }
    public DbSet<ScheduledReport> ScheduledReports { get; set; }
    public DbSet<ExportTemplate> ExportTemplates { get; set; }
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
    public DbSet<ApplicationSetting> ApplicationSettings { get; set; }

    // Analytics and Performance Tracking
    public DbSet<JobExecutionHistory> JobExecutionHistories { get; set; }
    public DbSet<JobBaseline> JobBaselines { get; set; }
    public DbSet<ErrorCorrelation> ErrorCorrelations { get; set; }
    public DbSet<BusinessImpact> BusinessImpacts { get; set; }
    
    // Alert Escalation and Correlation
    public DbSet<AlertEscalationRule> AlertEscalationRules { get; set; }
    public DbSet<AlertEscalation> AlertEscalations { get; set; }
    public DbSet<AlertCorrelation> AlertCorrelations { get; set; }
    
    // Shared Dashboards
    public DbSet<SharedDashboard> SharedDashboards { get; set; }
    public DbSet<DashboardShare> DashboardShares { get; set; }
    
    // Cost Tracking
    public DbSet<CostTracking> CostTrackings { get; set; }
    public DbSet<CostOptimizationRecommendation> CostOptimizationRecommendations { get; set; }
    public DbSet<CostBudget> CostBudgets { get; set; }

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
            entity.Property(e => e.Metadata).HasMaxLength(4000);
            entity.Property(e => e.AcknowledgedBy).HasMaxLength(100);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
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

        // Configure PerformanceBudget
        modelBuilder.Entity<PerformanceBudget>(entity =>
        {
            entity.ToTable("PerformanceBudgets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Endpoint).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.Endpoint).IsUnique();
        });

        // Configure ScheduledReport
        modelBuilder.Entity<ScheduledReport>(entity =>
        {
            entity.ToTable("ScheduledReports");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ReportType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Schedule).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CronExpression).HasMaxLength(100);
            entity.Property(e => e.Recipients).HasMaxLength(1000);
            entity.HasIndex(e => e.Enabled);
            entity.HasIndex(e => e.NextRun);
        });

        // Configure ExportTemplate
        modelBuilder.Entity<ExportTemplate>(entity =>
        {
            entity.ToTable("ExportTemplates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Format).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FieldsJson).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.HasIndex(e => new { e.EntityType, e.Format });
        });

        // Configure WebhookSubscription
        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.ToTable("WebhookSubscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Secret).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.HasIndex(e => e.Enabled);
            entity.HasIndex(e => e.EventType);
        });

        // Configure ApplicationSetting
        modelBuilder.Entity<ApplicationSetting>(entity =>
        {
            entity.ToTable("ApplicationSettings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // Configure JobExecutionHistory
        modelBuilder.Entity<JobExecutionHistory>(entity =>
        {
            entity.ToTable("JobExecutionHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BatchJobId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.JobName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.AosServer).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.HasIndex(e => e.BatchJobId);
            entity.HasIndex(e => e.JobName);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => new { e.JobName, e.StartTime });
        });

        // Configure JobBaseline
        modelBuilder.Entity<JobBaseline>(entity =>
        {
            entity.ToTable("JobBaselines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobName).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.JobName);
            entity.HasIndex(e => e.CalculatedAt);
        });

        // Configure ErrorCorrelation
        modelBuilder.Entity<ErrorCorrelation>(entity =>
        {
            entity.ToTable("ErrorCorrelations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobNameA).HasMaxLength(500).IsRequired();
            entity.Property(e => e.JobNameB).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Confidence).HasMaxLength(50);
            entity.HasIndex(e => new { e.JobNameA, e.JobNameB });
            entity.HasIndex(e => e.CorrelationCoefficient);
            entity.HasIndex(e => e.CalculatedAt);
        });

        // Configure BusinessImpact
        modelBuilder.Entity<BusinessImpact>(entity =>
        {
            entity.ToTable("BusinessImpacts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.BusinessProcess).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Priority).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EscalationContact).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasIndex(e => e.JobName).IsUnique();
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.ImpactScore);
        });

        // Configure AlertEscalationRule
        modelBuilder.Entity<AlertEscalationRule>(entity =>
        {
            entity.ToTable("AlertEscalationRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.AlertType).HasMaxLength(100);
            entity.Property(e => e.MinSeverity).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FirstEscalationRecipients).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.SecondEscalationRecipients).HasMaxLength(1000);
            entity.Property(e => e.FinalEscalationRecipients).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Enabled);
            entity.HasIndex(e => e.AlertType);
        });

        // Configure AlertEscalation
        modelBuilder.Entity<AlertEscalation>(entity =>
        {
            entity.ToTable("AlertEscalations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Recipients).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => e.AlertId);
            entity.HasIndex(e => e.EscalationRuleId);
            entity.HasIndex(e => e.EscalatedAt);
        });

        // Configure AlertCorrelation
        modelBuilder.Entity<AlertCorrelation>(entity =>
        {
            entity.ToTable("AlertCorrelations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrelationId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Severity).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CorrelationReason).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.CorrelationId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.FirstDetectedAt);
        });

        // Configure SharedDashboard
        modelBuilder.Entity<SharedDashboard>(entity =>
        {
            entity.ToTable("SharedDashboards");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DashboardId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.LayoutJson).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TeamName).HasMaxLength(255);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.HasIndex(e => e.DashboardId).IsUnique();
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.IsPublic);
            entity.HasIndex(e => e.IsTeamWorkspace);
            entity.HasIndex(e => e.TeamName);
        });

        // Configure DashboardShare
        modelBuilder.Entity<DashboardShare>(entity =>
        {
            entity.ToTable("DashboardShares");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SharedWith).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Permission).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SharedBy).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.DashboardId);
            entity.HasIndex(e => e.SharedWith);
            entity.HasIndex(e => new { e.DashboardId, e.SharedWith }).IsUnique();
        });

        // Configure CostTracking
        modelBuilder.Entity<CostTracking>(entity =>
        {
            entity.ToTable("CostTrackings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ResourceId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ResourceName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();
            entity.Property(e => e.CostBreakdown).HasMaxLength(4000);
            entity.Property(e => e.Metadata).HasMaxLength(4000);
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId });
            entity.HasIndex(e => e.PeriodStart);
            entity.HasIndex(e => e.PeriodEnd);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure CostOptimizationRecommendation
        modelBuilder.Entity<CostOptimizationRecommendation>(entity =>
        {
            entity.ToTable("CostOptimizationRecommendations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RecommendationType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ResourceId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Priority).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ImplementedBy).HasMaxLength(100);
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId });
            entity.HasIndex(e => e.IsImplemented);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure CostBudget
        modelBuilder.Entity<CostBudget>(entity =>
        {
            entity.ToTable("CostBudgets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Period).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();
            entity.Property(e => e.ResourceType).HasMaxLength(100);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Period);
        });
    }
}


using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Services;

public interface IBusinessKpiService
{
    Task<Dictionary<string, BusinessKpiResult>> CalculateBusinessKpisAsync(Dictionary<string, object> technicalMetrics);
    Task<BusinessImpactReport> GenerateBusinessImpactReportAsync(Dictionary<string, object> technicalMetrics);
}

public class BusinessKpiService : IBusinessKpiService
{
    private readonly ILogger<BusinessKpiService> _logger;

    public BusinessKpiService(ILogger<BusinessKpiService> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, BusinessKpiResult>> CalculateBusinessKpisAsync(Dictionary<string, object> technicalMetrics)
    {
        return await Task.Run(() =>
        {
            var results = new Dictionary<string, BusinessKpiResult>();

            // System Availability KPI
            var systemAvailability = CalculateSystemAvailability(technicalMetrics);
            results["system_uptime"] = new BusinessKpiResult
            {
                Name = "System Uptime",
                Description = "Percentage of time system is available for business operations",
                Category = "Availability",
                Domain = "General",
                Value = systemAvailability,
                Unit = "%",
                TargetValue = 99.5,
                WarningThreshold = 99.0,
                CriticalThreshold = 95.0,
                Status = DetermineStatus(systemAvailability, 99.5, 99.0, 95.0),
                BusinessImpact = "Direct impact on business operations and revenue",
                CalculatedAt = DateTime.UtcNow
            };

            // Transaction Success Rate
            var transactionSuccessRate = CalculateTransactionSuccessRate(technicalMetrics);
            results["transaction_success_rate"] = new BusinessKpiResult
            {
                Name = "Transaction Success Rate",
                Description = "Percentage of business transactions completed successfully",
                Category = "Reliability",
                Domain = "General",
                Value = transactionSuccessRate,
                Unit = "%",
                TargetValue = 99.0,
                WarningThreshold = 98.0,
                CriticalThreshold = 95.0,
                Status = DetermineStatus(transactionSuccessRate, 99.0, 98.0, 95.0),
                BusinessImpact = "Affects data integrity and business process completion",
                CalculatedAt = DateTime.UtcNow
            };

            // Average Response Time
            var avgResponseTime = CalculateAverageResponseTime(technicalMetrics);
            results["average_response_time"] = new BusinessKpiResult
            {
                Name = "Average Response Time",
                Description = "Average time for system to respond to user requests",
                Category = "Performance",
                Domain = "General",
                Value = avgResponseTime,
                Unit = "ms",
                TargetValue = 200,
                WarningThreshold = 500,
                CriticalThreshold = 1000,
                Status = DetermineStatus(avgResponseTime, 200, 500, 1000, true), // lower is better
                BusinessImpact = "Affects user productivity and satisfaction",
                CalculatedAt = DateTime.UtcNow
            };

            // Batch Completion Rate
            var batchCompletionRate = CalculateBatchCompletionRate(technicalMetrics);
            results["batch_completion_rate"] = new BusinessKpiResult
            {
                Name = "Batch Job Completion Rate",
                Description = "Percentage of batch jobs completed within SLA",
                Category = "Efficiency",
                Domain = "General",
                Value = batchCompletionRate,
                Unit = "%",
                TargetValue = 95.0,
                WarningThreshold = 90.0,
                CriticalThreshold = 80.0,
                Status = DetermineStatus(batchCompletionRate, 95.0, 90.0, 80.0),
                BusinessImpact = "Affects end-of-day processing and reporting",
                CalculatedAt = DateTime.UtcNow
            };

            // Throughput
            var throughput = CalculateThroughput(technicalMetrics);
            results["throughput"] = new BusinessKpiResult
            {
                Name = "System Throughput",
                Description = "Number of transactions processed per hour",
                Category = "Performance",
                Domain = "General",
                Value = throughput,
                Unit = "transactions/hour",
                TargetValue = 1000,
                WarningThreshold = 800,
                CriticalThreshold = 500,
                Status = DetermineStatus(throughput, 1000, 800, 500),
                BusinessImpact = "Indicates system capacity and efficiency",
                CalculatedAt = DateTime.UtcNow
            };

            return results;
        });
    }

    public async Task<BusinessImpactReport> GenerateBusinessImpactReportAsync(Dictionary<string, object> technicalMetrics)
    {
        return await Task.Run(() =>
        {
            var kpis = CalculateBusinessKpisAsync(technicalMetrics).Result;
            
            var overallHealth = CalculateOverallHealth(kpis);
            var slaCompliance = CalculateSlaCompliance(kpis);
            var criticalIssues = IdentifyCriticalIssues(kpis, technicalMetrics);
            var recommendations = GenerateRecommendations(kpis, technicalMetrics);

            return new BusinessImpactReport
            {
                OverallBusinessHealth = overallHealth,
                SlaCompliancePercentage = slaCompliance,
                CriticalIssuesCount = criticalIssues.Count,
                CriticalIssues = criticalIssues,
                Recommendations = recommendations,
                GeneratedAt = DateTime.UtcNow
            };
        });
    }

    private double CalculateSystemAvailability(Dictionary<string, object> metrics)
    {
        // Simplified calculation - in production, use actual uptime data
        var errorRate = metrics.TryGetValue("error_rate", out var er) ? Convert.ToDouble(er) : 0;
        var availability = 100.0 - (errorRate * 0.1); // Assume error rate impacts availability
        return Math.Max(95.0, Math.Min(100.0, availability)); // Clamp between 95-100%
    }

    private double CalculateTransactionSuccessRate(Dictionary<string, object> metrics)
    {
        var errorRate = metrics.TryGetValue("error_rate", out var er) ? Convert.ToDouble(er) : 0;
        return Math.Max(90.0, 100.0 - errorRate); // Clamp minimum at 90%
    }

    private double CalculateAverageResponseTime(Dictionary<string, object> metrics)
    {
        // Simplified - use SQL health and session metrics
        var cpuUsage = metrics.TryGetValue("cpu_usage", out var cpu) ? Convert.ToDouble(cpu) : 0;
        var baseResponseTime = 100; // Base response time in ms
        var adjustedResponseTime = baseResponseTime + (cpuUsage * 2); // CPU impacts response time
        return adjustedResponseTime;
    }

    private double CalculateBatchCompletionRate(Dictionary<string, object> metrics)
    {
        var batchBacklog = metrics.TryGetValue("batch_backlog", out var bb) ? Convert.ToInt32(bb) : 0;
        var errorRate = metrics.TryGetValue("error_rate", out var er) ? Convert.ToDouble(er) : 0;
        
        // Assume high completion rate if low backlog and error rate
        var completionRate = 100.0 - (batchBacklog * 2) - (errorRate * 0.5);
        return Math.Max(80.0, Math.Min(100.0, completionRate));
    }

    private double CalculateThroughput(Dictionary<string, object> metrics)
    {
        var activeSessions = metrics.TryGetValue("active_sessions", out var sessions) ? Convert.ToInt32(sessions) : 0;
        var cpuUsage = metrics.TryGetValue("cpu_usage", out var cpu) ? Convert.ToDouble(cpu) : 0;
        
        // Simplified throughput calculation
        var baseThroughput = activeSessions * 50; // Assume 50 transactions per session per hour
        var efficiencyFactor = (100.0 - cpuUsage) / 100.0; // Lower CPU = higher efficiency
        return baseThroughput * efficiencyFactor;
    }

    private string DetermineStatus(double value, double target, double warning, double critical, bool lowerIsBetter = false)
    {
        if (lowerIsBetter)
        {
            if (value <= target) return "excellent";
            if (value <= warning) return "good";
            if (value <= critical) return "warning";
            return "critical";
        }
        else
        {
            if (value >= target) return "excellent";
            if (value >= warning) return "good";
            if (value >= critical) return "warning";
            return "critical";
        }
    }

    private double CalculateOverallHealth(Dictionary<string, BusinessKpiResult> kpis)
    {
        if (!kpis.Any()) return 0;

        var healthScores = kpis.Values.Select(kpi =>
        {
            return kpi.Status switch
            {
                "excellent" => 100.0,
                "good" => 85.0,
                "warning" => 70.0,
                "critical" => 50.0,
                _ => 0.0
            };
        });

        return healthScores.Average();
    }

    private double CalculateSlaCompliance(Dictionary<string, BusinessKpiResult> kpis)
    {
        if (!kpis.Any()) return 0;

        var compliantKpis = kpis.Values.Count(kpi => kpi.Status == "excellent" || kpi.Status == "good");
        return (compliantKpis * 100.0) / kpis.Count;
    }

    private List<string> IdentifyCriticalIssues(Dictionary<string, BusinessKpiResult> kpis, Dictionary<string, object> technicalMetrics)
    {
        var issues = new List<string>();

        foreach (var kpi in kpis.Values)
        {
            if (kpi.Status == "critical")
            {
                issues.Add($"{kpi.Name} is at critical level ({kpi.Value:F1}{kpi.Unit})");
            }
        }

        // Check technical metrics
        var blockingChains = technicalMetrics.TryGetValue("blocking_chains", out var bc) ? Convert.ToInt32(bc) : 0;
        if (blockingChains > 0)
        {
            issues.Add($"{blockingChains} blocking chains detected - impacting database performance");
        }

        var cpuUsage = technicalMetrics.TryGetValue("cpu_usage", out var cpu) ? Convert.ToDouble(cpu) : 0;
        if (cpuUsage > 90)
        {
            issues.Add($"CPU usage is critically high ({cpuUsage:F1}%) - system performance degraded");
        }

        return issues;
    }

    private List<string> GenerateRecommendations(Dictionary<string, BusinessKpiResult> kpis, Dictionary<string, object> technicalMetrics)
    {
        var recommendations = new List<string>();

        foreach (var kpi in kpis.Values)
        {
            if (kpi.Status == "warning" || kpi.Status == "critical")
            {
                recommendations.Add($"Review {kpi.Name} - Current value ({kpi.Value:F1}{kpi.Unit}) is below target ({kpi.TargetValue:F1}{kpi.Unit})");
            }
        }

        var blockingChains = technicalMetrics.TryGetValue("blocking_chains", out var bc) ? Convert.ToInt32(bc) : 0;
        if (blockingChains > 0)
        {
            recommendations.Add("Investigate blocking chains and optimize long-running queries");
        }

        var errorRate = technicalMetrics.TryGetValue("error_rate", out var er) ? Convert.ToDouble(er) : 0;
        if (errorRate > 5)
        {
            recommendations.Add("Review error logs and investigate root causes of elevated error rate");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("System performance is within normal parameters - continue monitoring");
        }

        return recommendations;
    }
}

public class BusinessKpiResult
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double TargetValue { get; set; }
    public double WarningThreshold { get; set; }
    public double CriticalThreshold { get; set; }
    public string Status { get; set; } = string.Empty; // excellent, good, warning, critical
    public string BusinessImpact { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
}

public class BusinessImpactReport
{
    public double OverallBusinessHealth { get; set; }
    public double SlaCompliancePercentage { get; set; }
    public int CriticalIssuesCount { get; set; }
    public List<string> CriticalIssues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}


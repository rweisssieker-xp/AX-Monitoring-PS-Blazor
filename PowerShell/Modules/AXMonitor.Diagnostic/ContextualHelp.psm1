# AXMonitor.Diagnostic - Contextual Help Module
# Purpose: Provides context-aware help and explanations for performance metrics
# Author: Qwen Code
# Date: 2025-10-27

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Get-ContextualHelp {
    <#
    .SYNOPSIS
    Provides context-aware help for performance metrics
    
    .DESCRIPTION
    This function provides contextual help and explanations based on the current context (metric type, value, time period).
    It returns relevant documentation, troubleshooting tips, and best practices.
    
    .PARAMETER MetricType
    Type of metric being analyzed (batch, sessions, blocking, sqlHealth)
    
    .PARAMETER MetricValue
    Current value of the metric
    
    .PARAMETER TimePeriod
    Time period for the metric (default: "24h")
    
    .PARAMETER Context
    Additional context information (optional)
    
    .EXAMPLE
    Get-ContextualHelp -MetricType "batch" -MetricValue 5.2 -TimePeriod "24h"
    
    .NOTES
    This function provides targeted help based on the specific context of the metric.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [ValidateSet("batch", "sessions", "blocking", "sqlHealth")]
        [string]$MetricType,
        
        [Parameter(Mandatory=$true)]
        [double]$MetricValue,
        
        [Parameter()]
        [string]$TimePeriod = "24h",
        
        [Parameter()]
        [object]$Context
    )
    
    # Initialize result object
    $help = @{
        Status = "Success"
        Message = "Contextual help provided"
        Topic = ""
        Explanation = ""
        TroubleshootingTips = @()
        BestPractices = @()
        RelatedMetrics = @()
        ConfidenceScore = 0.85
    }
    
    try {
        # Set topic based on metric type
        switch ($MetricType) {
            "batch" {
                $help.Topic = "Batch Job Performance"
                
                # Provide explanation based on value
                if ($MetricValue -lt 2) {
                    $help.Explanation = "The batch job execution time is excellent. Values below 2 seconds indicate optimal performance."
                } elseif ($MetricValue -lt 5) {
                    $help.Explanation = "The batch job execution time is good. Values between 2-5 seconds are acceptable for most scenarios."
                } elseif ($MetricValue -lt 10) {
                    $help.Explanation = "The batch job execution time is moderate. Values between 5-10 seconds may indicate potential performance issues that should be monitored."
                } else {
                    $help.Explanation = "The batch job execution time is high. Values above 10 seconds indicate performance issues that require investigation."
                }
                
                # Add troubleshooting tips
                $help.TroubleshootingTips += "Check for blocking queries that may be impacting batch job execution."
                $help.TroubleshootingTips += "Review batch job configuration and ensure it's optimized for your environment."
                $help.TroubleshootingTips += "Consider increasing server resources if batch jobs consistently run slowly."
                
                # Add best practices
                $help.BestPractices += "Schedule batch jobs during off-peak hours to minimize impact on users."
                $help.BestPractices += "Monitor batch job execution times regularly to identify trends."
                $help.BestPractices += "Implement proper error handling in batch jobs to prevent failures."
                
                # Add related metrics
                $help.RelatedMetrics += "Sessions per AOS"
                $help.RelatedMetrics += "Database CPU usage"
                $help.RelatedMetrics += "Blocking chains"
            }
            
            "sessions" {
                $help.Topic = "User Session Management"
                
                # Provide explanation based on value
                if ($MetricValue -lt 50) {
                    $help.Explanation = "The number of active sessions is low. This is typical for small or medium-sized environments."
                } elseif ($MetricValue -lt 100) {
                    $help.Explanation = "The number of active sessions is moderate. This is typical for medium-sized environments."
                } elseif ($MetricValue -lt 200) {
                    $help.Explanation = "The number of active sessions is high. This may indicate resource pressure on the system."
                } else {
                    $help.Explanation = "The number of active sessions is very high. This may indicate potential performance issues and should be investigated."
                }
                
                # Add troubleshooting tips
                $help.TroubleshootingTips += "Check for long-running transactions that may be consuming resources."
                $help.TroubleshootingTips += "Review session timeout settings to ensure they're appropriate for your environment."
                $help.TroubleshootingTips += "Consider implementing connection pooling to reduce overhead."
                
                # Add best practices
                $help.BestPractices += "Monitor session counts regularly to identify unusual patterns."
                $help.BestPractices += "Implement proper session management to prevent resource leaks."
                $help.BestPractices += "Use load balancing to distribute sessions across multiple AOS instances."
                
                # Add related metrics
                $help.RelatedMetrics += "Long-running transactions"
                $help.RelatedMetrics += "CPU usage per AOS"
                $help.RelatedMetrics += "Memory usage per AOS"
            }
            
            "blocking" {
                $help.Topic = "SQL Blocking Analysis"
                
                # Provide explanation based on value
                if ($MetricValue -eq 0) {
                    $help.Explanation = "There are no active blocking chains. This indicates optimal database performance with no contention."
                } elseif ($MetricValue -lt 3) {
                    $help.Explanation = "There are a few blocking chains. This is normal for most systems but should be monitored."
                } elseif ($MetricValue -lt 10) {
                    $help.Explanation = "There are multiple blocking chains. This may indicate performance issues that should be investigated."
                } else {
                    $help.Explanation = "There are many blocking chains. This indicates serious performance issues that require immediate attention."
                }
                
                # Add troubleshooting tips
                $help.TroubleshootingTips += "Identify the root cause SQL text of blocking queries."
                $help.TroubleshootingTips += "Look for long-running transactions that may be causing blocking."
                $help.TroubleshootingTips += "Consider optimizing queries that are frequently involved in blocking."
                
                # Add best practices
                $help.BestPractices += "Implement proper indexing to reduce query execution time."
                $help.BestPractices += "Use transaction isolation levels appropriately to minimize blocking."
                $help.BestPractices += "Monitor blocking chains regularly to identify patterns."
                
                # Add related metrics
                $help.RelatedMetrics += "Wait statistics"
                $help.RelatedMetrics += "CPU usage"
                $help.RelatedMetrics += "IO operations"
            }
            
            "sqlHealth" {
                $help.Topic = "SQL Server Health Monitoring"
                
                # Provide explanation based on value
                if ($MetricValue -lt 50) {
                    $help.Explanation = "The SQL Server health is excellent. Values below 50% indicate optimal resource utilization."
                } elseif ($MetricValue -lt 75) {
                    $help.Explanation = "The SQL Server health is good. Values between 50-75% indicate acceptable resource utilization."
                } elseif ($MetricValue -lt 90) {
                    $help.Explanation = "The SQL Server health is moderate. Values between 75-90% may indicate potential performance issues."
                } else {
                    $help.Explanation = "The SQL Server health is poor. Values above 90% indicate critical performance issues that require immediate attention."
                }
                
                # Add troubleshooting tips
                $help.TroubleshootingTips += "Check for high CPU usage that may be impacting performance."
                $help.TroubleshootingTips += "Review memory usage and consider increasing available memory if needed."
                $help.TroubleshootingTips += "Look for excessive IO operations that may be causing bottlenecks."
                
                # Add best practices
                $help.BestPractices += "Implement proper indexing to reduce query execution time."
                $help.BestPractices += "Monitor SQL Server health regularly to identify trends."
                $help.BestPractices += "Use performance monitoring tools to identify and resolve issues proactively."
                
                # Add related metrics
                $help.RelatedMetrics += "CPU usage"
                $help.RelatedMetrics += "Memory usage"
                $help.RelatedMetrics += "Disk I/O operations"
            }
        }
        
        # Add general tips based on time period
        if ($TimePeriod -eq "7d") {
            $help.TroubleshootingTips += "Consider comparing current values with historical data from previous weeks."
            $help.BestPractices += "Implement trend analysis to identify long-term performance patterns."
        } elseif ($TimePeriod -eq "30d") {
            $help.TroubleshootingTips += "Look for seasonal patterns that may be affecting performance."
            $help.BestPractices += "Use monthly reports to identify long-term trends and plan capacity."
        }
        
        # Add confidence score based on context
        if ($Context) {
            $help.ConfidenceScore = 0.90
        }
        
    } catch {
        $help.Status = "Error"
        $help.Message = "Failed to provide contextual help: $($_.Exception.Message)"
        $help.ConfidenceScore = 0.0
    }
    
    return $help
}

function Get-MetricExplanation {
    <#
    .SYNOPSIS
    Gets a detailed explanation of a specific performance metric
    
    .DESCRIPTION
    This function provides a detailed explanation of a specific performance metric including its purpose, how it's calculated, and what constitutes good/bad values.
    
    .PARAMETER MetricName
    Name of the metric to explain
    
    .EXAMPLE
    Get-MetricExplanation -MetricName "BatchBacklog"
    
    .NOTES
    This function provides comprehensive documentation for each metric.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$MetricName
    )
    
    # Initialize result object
    $explanation = @{
        Status = "Success"
        Message = "Metric explanation provided"
        MetricName = $MetricName
        Definition = ""
        Calculation = ""
        GoodValueRange = ""
        BadValueRange = ""
        Impact = ""
        RelatedMetrics = @()
        ConfidenceScore = 0.85
    }
    
    try {
        # Provide explanation based on metric name
        switch ($MetricName) {
            "BatchBacklog" {
                $explanation.Definition = "The number of batch jobs waiting to be executed."
                $explanation.Calculation = "Count of batch jobs with status 'Waiting'."
                $explanation.GoodValueRange = "0-5"
                $explanation.BadValueRange = "> 10"
                $explanation.Impact = "High backlog can delay critical business processes and impact user productivity."
                $explanation.RelatedMetrics += "BatchJobExecutionTime"
                $explanation.RelatedMetrics += "ActiveSessions"
                $explanation.RelatedMetrics += "CPUUsage"
            }
            
            "BatchJobExecutionTime" {
                $explanation.Definition = "The average time taken to execute batch jobs."
                $explanation.Calculation = "Average duration of completed batch jobs over the specified time period."
                $explanation.GoodValueRange = "< 2 seconds"
                $explanation.BadValueRange = "> 10 seconds"
                $explanation.Impact = "Long execution times can delay processing and impact business operations."
                $explanation.RelatedMetrics += "BatchBacklog"
                $explanation.RelatedMetrics += "CPUUsage"
                $explanation.RelatedMetrics += "DatabaseIO"
            }
            
            "ActiveSessions" {
                $explanation.Definition = "The number of currently active user sessions."
                $explanation.Calculation = "Count of active sessions across all AOS instances."
                $explanation.GoodValueRange = "< 100"
                $explanation.BadValueRange = "> 200"
                $explanation.Impact = "High session counts can indicate resource pressure on the system."
                $explanation.RelatedMetrics += "LongRunningTransactions"
                $explanation.RelatedMetrics += "CPUUsagePerAOS"
                $explanation.RelatedMetrics += "MemoryUsagePerAOS"
            }
            
            "BlockingChains" {
                $explanation.Definition = "The number of active blocking chains in the database."
                $explanation.Calculation = "Count of blocking relationships between sessions."
                $explanation.GoodValueRange = "0"
                $explanation.BadValueRange = "> 5"
                $explanation.Impact = "Blocking chains can cause delays and performance degradation for users."
                $explanation.RelatedMetrics += "WaitStatistics"
                $explanation.RelatedMetrics += "RootCauseSQL"
                $explanation.RelatedMetrics += "BlockedSessions"
            }
            
            "CPUUsage" {
                $explanation.Definition = "The percentage of CPU resources being used by SQL Server."
                $explanation.Calculation = "Average CPU usage over the specified time period."
                $explanation.GoodValueRange = "< 50%"
                $explanation.BadValueRange = "> 80%"
                $explanation.Impact = "High CPU usage can indicate performance bottlenecks and slow response times."
                $explanation.RelatedMetrics += "QueryExecutionTime"
                $explanation.RelatedMetrics += "DatabaseIO"
                $explanation.RelatedMetrics += "MemoryUsage"
            }
            
            "MemoryUsage" {
                $explanation.Definition = "The percentage of memory resources being used by SQL Server."
                $explanation.Calculation = "Average memory usage over the specified time period."
                $explanation.GoodValueRange = "< 70%"
                $explanation.BadValueRange = "> 90%"
                $explanation.Impact = "High memory usage can lead to paging and performance degradation."
                $explanation.RelatedMetrics += "CPUUsage"
                $explanation.RelatedMetrics += "PageFaults"
                $explanation.RelatedMetrics += "BufferPoolHitRatio"
            }
            
            "DatabaseIO" {
                $explanation.Definition = "The number of input/output operations per second on the database."
                $explanation.Calculation = "Average IOPS over the specified time period."
                $explanation.GoodValueRange = "< 1000 IOPS"
                $explanation.BadValueRange = "> 5000 IOPS"
                $explanation.Impact = "High I/O operations can indicate disk bottlenecks and slow performance."
                $explanation.RelatedMetrics += "DiskLatency"
                $explanation.RelatedMetrics += "ReadWriteRatio"
                $explanation.RelatedMetrics += "TempDBUsage"
            }
            
            default {
                $explanation.Status = "Error"
                $explanation.Message = "Unknown metric: $MetricName"
                $explanation.ConfidenceScore = 0.0
            }
        }
        
    } catch {
        $explanation.Status = "Error"
        $explanation.Message = "Failed to get metric explanation: $($_.Exception.Message)"
        $explanation.ConfidenceScore = 0.0
    }
    
    return $explanation
}

function Get-TroubleshootingGuide {
    <#
    .SYNOPSIS
    Gets a troubleshooting guide for common performance issues
    
    .DESCRIPTION
    This function provides a comprehensive troubleshooting guide for common performance issues including step-by-step instructions and diagnostic commands.
    
    .PARAMETER IssueType
    Type of issue to troubleshoot (default: "Performance")
    Valid values: "Performance", "Availability", "Connectivity", "Configuration"
    
    .EXAMPLE
    Get-TroubleshootingGuide -IssueType "Performance"
    
    .NOTES
    This function provides practical guidance for resolving common issues.
    #>
    param(
        [Parameter()]
        [ValidateSet("Performance", "Availability", "Connectivity", "Configuration")]
        [string]$IssueType = "Performance"
    )
    
    # Initialize result object
    $guide = @{
        Status = "Success"
        Message = "Troubleshooting guide provided"
        IssueType = $IssueType
        Steps = @()
        DiagnosticCommands = @()
        CommonCauses = @()
        ResolutionTips = @()
        ConfidenceScore = 0.85
    }
    
    try {
        # Provide troubleshooting guide based on issue type
        switch ($IssueType) {
            "Performance" {
                $guide.Steps += "1. Identify the specific performance bottleneck (CPU, memory, I/O, network)."
                $guide.Steps += "2. Check for blocking queries and long-running transactions."
                $guide.Steps += "3. Review index usage and optimize queries."
                $guide.Steps += "4. Monitor resource utilization and adjust configuration as needed."
                $guide.Steps += "5. Implement caching and other performance optimizations."
                
                $guide.DiagnosticCommands += "SELECT * FROM sys.dm_exec_requests WHERE status = 'running'"
                $guide.DiagnosticCommands += "SELECT * FROM sys.dm_os_waiting_tasks WHERE wait_type LIKE '%BLOCK%'"
                $guide.DiagnosticCommands += "SELECT * FROM sys.dm_db_index_usage_stats"
                
                $guide.CommonCauses += "Inefficient queries without proper indexing"
                $guide.CommonCauses += "Blocking caused by long-running transactions"
                $guide.CommonCauses += "Insufficient hardware resources"
                $guide.CommonCauses += "Poorly configured database parameters"
                
                $guide.ResolutionTips += "Optimize queries by adding missing indexes."
                $guide.ResolutionTips += "Implement proper transaction management to reduce blocking."
                $guide.ResolutionTips += "Increase hardware resources if necessary."
                $guide.ResolutionTips += "Adjust database configuration parameters for better performance."
            }
            
            "Availability" {
                $guide.Steps += "1. Verify the service is running and accessible."
                $guide.Steps += "2. Check for network connectivity issues."
                $guide.Steps += "3. Review service logs for errors."
                $guide.Steps += "4. Test failover and redundancy mechanisms."
                $guide.Steps += "5. Implement monitoring and alerting for availability issues."
                
                $guide.DiagnosticCommands += "Get-Service -Name 'AXMonitorService'"
                $guide.DiagnosticCommands += "Test-NetConnection -ComputerName localhost -Port 8080"
                $guide.DiagnosticCommands += "Get-EventLog -LogName Application -Source 'AXMonitor' -Newest 10"
                
                $guide.CommonCauses += "Service not started or stopped unexpectedly"
                $guide.CommonCauses += "Network connectivity issues"
                $guide.CommonCauses += "Configuration errors"
                $guide.CommonCauses += "Resource exhaustion"
                
                $guide.ResolutionTips += "Ensure the service is configured to start automatically."
                $guide.ResolutionTips += "Verify network settings and firewall rules."
                $guide.ResolutionTips += "Review service configuration and fix any errors."
                $guide.ResolutionTips += "Implement redundancy and failover mechanisms."
            }
            
            "Connectivity" {
                $guide.Steps += "1. Verify network connectivity between components."
                $guide.Steps += "2. Check firewall settings and port configurations."
                $guide.Steps += "3. Test database connectivity."
                $guide.Steps += "4. Review authentication and authorization settings."
                $guide.Steps += "5. Implement connection pooling and retry mechanisms."
                
                $guide.DiagnosticCommands += "Test-NetConnection -ComputerName sqlserver01 -Port 1433"
                $guide.DiagnosticCommands += "Invoke-Sqlcmd -Query 'SELECT GETDATE()' -ServerInstance sqlserver01\INST"
                $guide.DiagnosticCommands += "Get-NetFirewallRule -DisplayName 'AX Monitor'"
                
                $guide.CommonCauses += "Firewall blocking required ports"
                $guide.CommonCauses += "Incorrect connection strings or credentials"
                $guide.CommonCauses += "Network routing issues"
                $guide.CommonCauses += "Authentication/authorization failures"
                
                $guide.ResolutionTips += "Configure firewall rules to allow required traffic."
                $guide.ResolutionTips += "Verify connection strings and credentials."
                $guide.ResolutionTips += "Check network routing and DNS settings."
                $guide.ResolutionTips += "Review authentication and authorization settings."
            }
            
            "Configuration" {
                $guide.Steps += "1. Review configuration files for errors or inconsistencies."
                $guide.Steps += "2. Validate environment variables and settings."
                $guide.Steps += "3. Check for missing or incorrect dependencies."
                $guide.Steps += "4. Test configuration changes in a non-production environment."
                $guide.Steps += "5. Implement configuration management and version control."
                
                $guide.DiagnosticCommands += "Get-Content -Path '.env.dev'"
                $guide.DiagnosticCommands += "Get-ChildItem -Path 'config.yaml'"
                $guide.DiagnosticCommands += "Get-Module -ListAvailable -Name Pode"
                
                $guide.CommonCauses += "Missing or incorrect environment variables"
                $guide.CommonCauses += "Invalid configuration file syntax"
                $guide.CommonCauses += "Missing dependencies or incompatible versions"
                $guide.CommonCauses += "Configuration drift between environments"
                
                $guide.ResolutionTips += "Validate configuration files using appropriate tools."
                $guide.ResolutionTips += "Use configuration management tools to ensure consistency."
                $guide.ResolutionTips += "Implement automated testing for configuration changes."
                $guide.ResolutionTips += "Document configuration requirements and dependencies."
            }
        }
        
        # Add confidence score based on issue type
        $guide.ConfidenceScore = 0.90
        
    } catch {
        $guide.Status = "Error"
        $guide.Message = "Failed to get troubleshooting guide: $($_.Exception.Message)"
        $guide.ConfidenceScore = 0.0
    }
    
    return $guide
}

# Export functions
Export-ModuleMember -Function Get-ContextualHelp, Get-MetricExplanation, Get-TroubleshootingGuide
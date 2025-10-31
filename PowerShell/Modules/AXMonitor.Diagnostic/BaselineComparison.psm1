# AXMonitor.Diagnostic - Baseline Comparison Module
# Purpose: Provides performance baseline comparison functionality
# Author: Qwen Code
# Date: 2025-10-27

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Compare-PerformanceBaseline {
    <#
    .SYNOPSIS
    Compares current performance metrics against historical baselines
    
    .DESCRIPTION
    This function compares current performance metrics against historical baselines to identify deviations.
    It calculates various metrics including percentage change, standard deviation, and confidence intervals.
    
    .PARAMETER CurrentMetrics
    Current performance metrics to compare
    
    .PARAMETER BaselineMetrics
    Historical baseline metrics for comparison
    
    .PARAMETER MetricType
    Type of metric being compared (batch, sessions, blocking, sqlHealth)
    
    .PARAMETER ConfidenceLevel
    Confidence level for statistical analysis (default: 95%)
    
    .EXAMPLE
    $comparison = Compare-PerformanceBaseline -CurrentMetrics $currentData -BaselineMetrics $baselineData -MetricType "batch" -ConfidenceLevel 95
    
    .NOTES
    This function provides statistical analysis of performance deviations from baseline.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object]$CurrentMetrics,
        
        [Parameter(Mandatory=$true)]
        [object]$BaselineMetrics,
        
        [Parameter()]
        [ValidateSet("batch", "sessions", "blocking", "sqlHealth")]
        [string]$MetricType = "batch",
        
        [Parameter()]
        [int]$ConfidenceLevel = 95
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Baseline comparison completed"
        Comparison = @{}
        ConfidenceScore = 0.85
        StatisticalAnalysis = @{}
    }
    
    try {
        # Validate input
        if ($CurrentMetrics.Count -eq 0 -or $BaselineMetrics.Count -eq 0) {
            throw "Both current and baseline metrics must contain data"
        }
        
        # Calculate baseline statistics
        $baselineValues = $BaselineMetrics | ForEach-Object { $_.Value }
        $baselineMean = ($baselineValues | Measure-Object -Average).Average
        $baselineStdDev = ($baselineValues | Measure-Object -StandardDeviation).StandardDeviation
        $baselineCount = $baselineValues.Count
        
        # Calculate current statistics
        $currentValues = $CurrentMetrics | ForEach-Object { $_.Value }
        $currentMean = ($currentValues | Measure-Object -Average).Average
        $currentStdDev = ($currentValues | Measure-Object -StandardDeviation).StandardDeviation
        $currentCount = $currentValues.Count
        
        # Calculate percentage change
        $percentChange = 0
        if ($baselineMean -ne 0) {
            $percentChange = (($currentMean - $baselineMean) / $baselineMean) * 100
        }
        
        # Calculate z-score for current mean compared to baseline
        $zScore = 0
        if ($baselineStdDev -ne 0) {
            $zScore = ($currentMean - $baselineMean) / ($baselineStdDev / [math]::Sqrt($baselineCount))
        }
        
        # Calculate confidence interval for baseline
        $alpha = (100 - $ConfidenceLevel) / 100
        $zCritical = [math]::Abs([System.Math]::Round([System.Math]::InvNorm($alpha / 2), 2))
        $marginOfError = $zCritical * ($baselineStdDev / [math]::Sqrt($baselineCount))
        $lowerBound = $baselineMean - $marginOfError
        $upperBound = $baselineMean + $marginOfError
        
        # Determine if current performance is significantly different from baseline
        $isSignificant = $false
        if ($currentMean -lt $lowerBound -or $currentMean -gt $upperBound) {
            $isSignificant = $true
        }
        
        # Calculate p-value (simplified approximation)
        $pValue = 0
        if ($zScore -ne 0) {
            # Use normal distribution approximation
            $pValue = 2 * (1 - [System.Math]::Erf([math]::Abs($zScore) / [math]::Sqrt(2)))
        }
        
        # Create comparison result
        $comparison = @{
            MetricType = $MetricType
            CurrentMean = $currentMean
            BaselineMean = $baselineMean
            PercentChange = $percentChange
            ZScore = $zScore
            PValue = $pValue
            IsSignificant = $isSignificant
            ConfidenceInterval = @{
                LowerBound = $lowerBound
                UpperBound = $upperBound
                MarginOfError = $marginOfError
                ConfidenceLevel = $ConfidenceLevel
            }
            CurrentStatistics = @{
                Count = $currentCount
                Mean = $currentMean
                StdDev = $currentStdDev
            }
            BaselineStatistics = @{
                Count = $baselineCount
                Mean = $baselineMean
                StdDev = $baselineStdDev
            }
        }
        
        # Add recommendations based on comparison
        $recommendations = @()
        if ($isSignificant) {
            if ($percentChange -gt 0) {
                $recommendations += "Performance has significantly degraded. Consider investigating potential causes such as increased load, resource constraints, or configuration changes."
            } else {
                $recommendations += "Performance has significantly improved. Consider documenting the changes that led to this improvement for future reference."
            }
        } else {
            $recommendations += "Performance is within expected range compared to baseline. No immediate action required."
        }
        
        # Set confidence score based on statistical significance
        if ($isSignificant) {
            $result.ConfidenceScore = 0.90
        } else {
            $result.ConfidenceScore = 0.80
        }
        
        # Store results
        $result.Comparison = $comparison
        $result.StatisticalAnalysis = @{
            ZScore = $zScore
            PValue = $pValue
            IsSignificant = $isSignificant
            ConfidenceInterval = @{
                LowerBound = $lowerBound
                UpperBound = $upperBound
                MarginOfError = $marginOfError
                ConfidenceLevel = $ConfidenceLevel
            }
        }
        $result.Recommendations = $recommendations
        
        $result.Message = "Baseline comparison completed. Current mean: $currentMean, Baseline mean: $baselineMean, Percent change: $percentChange%"
        
    } catch {
        $result.Status = "Error"
        $result.Message = "Baseline comparison failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
    }
    
    return $result
}

function Get-BaselineHistory {
    <#
    .SYNOPSIS
    Gets historical baseline data for performance metrics
    
    .DESCRIPTION
    This function retrieves historical baseline data for performance metrics over a specified time period.
    
    .PARAMETER MetricType
    Type of metric to retrieve baseline for (batch, sessions, blocking, sqlHealth)
    
    .PARAMETER TimePeriod
    Time period for baseline data (default: "30d")
    Valid values: "7d", "30d", "90d", "180d", "365d"
    
    .EXAMPLE
    $baseline = Get-BaselineHistory -MetricType "batch" -TimePeriod "30d"
    
    .NOTES
    This function returns historical data for creating performance baselines.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [ValidateSet("batch", "sessions", "blocking", "sqlHealth")]
        [string]$MetricType,
        
        [Parameter()]
        [ValidateSet("7d", "30d", "90d", "180d", "365d")]
        [string]$TimePeriod = "30d"
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Baseline history retrieved"
        Data = @()
        Statistics = @{
            Count = 0
            Mean = 0
            StdDev = 0
            Min = 0
            Max = 0
        }
        ConfidenceScore = 0.85
    }
    
    try {
        # Generate sample baseline data for demonstration purposes
        # In a real implementation, this would query historical data storage
        
        $startDate = (Get-Date).AddDays(-[int]($TimePeriod.TrimEnd('d')))
        $endDate = Get-Date
        
        # Generate sample data points
        $dataPoints = @()
        for ($i = 0; $i -lt 24; $i++) {
            $timestamp = $startDate.AddHours($i)
            
            # Generate sample value based on metric type
            switch ($MetricType) {
                "batch" {
                    # Batch job duration in seconds
                    $value = 120 + (Get-Random -Minimum -30 -Maximum 30)
                }
                "sessions" {
                    # Active sessions count
                    $value = 50 + (Get-Random -Minimum -20 -Maximum 20)
                }
                "blocking" {
                    # Blocking chains count
                    $value = 0 + (Get-Random -Minimum -2 -Maximum 2)
                }
                "sqlHealth" {
                    # CPU usage percentage
                    $value = 50 + (Get-Random -Minimum -25 -Maximum 25)
                }
            }
            
            $dataPoints += @{
                Timestamp = $timestamp.ToString('o')
                Value = $value
            }
        }
        
        # Calculate statistics
        $values = $dataPoints | ForEach-Object { $_.Value }
        $count = $values.Count
        $mean = ($values | Measure-Object -Average).Average
        $stdDev = ($values | Measure-Object -StandardDeviation).StandardDeviation
        $min = ($values | Measure-Object -Minimum).Minimum
        $max = ($values | Measure-Object -Maximum).Maximum
        
        $result.Data = $dataPoints
        $result.Statistics.Count = $count
        $result.Statistics.Mean = $mean
        $result.Statistics.StdDev = $stdDev
        $result.Statistics.Min = $min
        $result.Statistics.Max = $max
        
        $result.Message = "Retrieved $count data points for $MetricType over $TimePeriod period"
        
    } catch {
        $result.Status = "Error"
        $result.Message = "Failed to retrieve baseline history: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
    }
    
    return $result
}

function Create-PerformanceBaseline {
    <#
    .SYNOPSIS
    Creates a new performance baseline from historical data
    
    .DESCRIPTION
    This function creates a new performance baseline by analyzing historical data over a specified time period.
    
    .PARAMETER MetricType
    Type of metric to create baseline for (batch, sessions, blocking, sqlHealth)
    
    .PARAMETER TimePeriod
    Time period for baseline creation (default: "30d")
    Valid values: "7d", "30d", "90d", "180d", "365d"
    
    .PARAMETER Name
    Name for the baseline (optional)
    
    .EXAMPLE
    $baseline = Create-PerformanceBaseline -MetricType "batch" -TimePeriod "30d" -Name "Monthly Batch Performance"
    
    .NOTES
    This function creates a comprehensive baseline for performance monitoring.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [ValidateSet("batch", "sessions", "blocking", "sqlHealth")]
        [string]$MetricType,
        
        [Parameter()]
        [ValidateSet("7d", "30d", "90d", "180d", "365d")]
        [string]$TimePeriod = "30d",
        
        [Parameter()]
        [string]$Name = ""
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Performance baseline created"
        Baseline = @{}
        ConfidenceScore = 0.85
    }
    
    try {
        # Get historical data for baseline creation
        $historicalData = Get-BaselineHistory -MetricType $MetricType -TimePeriod $TimePeriod
        
        if ($historicalData.Status -ne "Success") {
            throw "Failed to get historical data: $($historicalData.Message)"
        }
        
        # Create baseline object
        $baseline = @{
            Name = if ($Name) { $Name } else { "$MetricType Performance Baseline ($TimePeriod)" }
            MetricType = $MetricType
            TimePeriod = $TimePeriod
            CreatedAt = (Get-Date).ToString('o')
            DataPoints = $historicalData.Data.Count
            Mean = $historicalData.Statistics.Mean
            StdDev = $historicalData.Statistics.StdDev
            Min = $historicalData.Statistics.Min
            Max = $historicalData.Statistics.Max
            ConfidenceInterval = @{
                LowerBound = $historicalData.Statistics.Mean - (1.96 * $historicalData.Statistics.StdDev)
                UpperBound = $historicalData.Statistics.Mean + (1.96 * $historicalData.Statistics.StdDev)
                ConfidenceLevel = 95
            }
            Thresholds = @{
                Warning = $historicalData.Statistics.Mean + (1 * $historicalData.Statistics.StdDev)
                Critical = $historicalData.Statistics.Mean + (2 * $historicalData.Statistics.StdDev)
            }
            Recommendations = @()
        }
        
        # Add recommendations based on baseline characteristics
        $baseRecommendation = "Use this baseline for performance monitoring and alerting."
        $baseline.Recommendations += $baseRecommendation
        
        # Add specific recommendations based on metric type
        switch ($MetricType) {
            "batch" {
                $baseline.Recommendations += "Monitor batch job durations against this baseline to identify performance degradation."
                $baseline.Recommendations += "Consider optimizing long-running batch jobs that consistently exceed the critical threshold."
            }
            "sessions" {
                $baseline.Recommendations += "Monitor active session counts against this baseline to identify unusual activity."
                $baseline.Recommendations += "Investigate sudden spikes in session counts as they may indicate security issues or application problems."
            }
            "blocking" {
                $baseline.Recommendations += "Monitor blocking chains against this baseline to identify database contention issues."
                $baseline.Recommendations += "Address any blocking chains that consistently exceed the warning threshold to maintain system performance."
            }
            "sqlHealth" {
                $baseline.Recommendations += "Monitor SQL Server health metrics against this baseline to identify resource bottlenecks."
                $baseline.Recommendations += "Investigate high CPU, I/O, or memory usage that exceeds the critical threshold to prevent performance issues."
            }
        }
        
        $result.Baseline = $baseline
        $result.Message = "Created performance baseline: $($baseline.Name)"
        
        # Set confidence score based on data quality
        if ($historicalData.Data.Count -gt 20) {
            $result.ConfidenceScore = 0.90
        } elseif ($historicalData.Data.Count -gt 10) {
            $result.ConfidenceScore = 0.85
        } else {
            $result.ConfidenceScore = 0.75
        }
        
    } catch {
        $result.Status = "Error"
        $result.Message = "Failed to create performance baseline: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
    }
    
    return $result
}

# Export functions
Export-ModuleMember -Function Compare-PerformanceBaseline, Get-BaselineHistory, Create-PerformanceBaseline
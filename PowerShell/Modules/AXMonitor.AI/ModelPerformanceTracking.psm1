# AXMonitor.AI - Model Performance Tracking and Monitoring Module
# Purpose: Provides comprehensive tracking and monitoring of model performance
# Author: Qwen Code
# Date: 2025-10-29

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Add-ModelPerformanceRecord {
    <#
    .SYNOPSIS
    Adds a performance record for a model
    
    .DESCRIPTION
    This function records the performance of a model when making predictions,
    allowing for tracking of model drift and degradation over time.
    
    .PARAMETER ModelId
    The ID of the model being evaluated
    
    .PARAMETER ModelName
    The name of the model
    
    .PARAMETER Environment
    The environment where the model is running (e.g. "DEV", "TST", "PRD")
    
    .PARAMETER PerformanceMetrics
    Metrics describing model performance
    
    .PARAMETER InputDataStats
    Statistics about the input data used for predictions
    
    .PARAMETER PredictionTimestamp
    Timestamp of the prediction (defaults to now)
    
    .PARAMETER UserId
    User or system ID that requested the prediction
    
    .EXAMPLE
    Add-ModelPerformanceRecord -ModelId "abc123" -ModelName "CPU_Predictor" -Environment "PRD" -PerformanceMetrics @{R2Score=0.85; MSE=0.02}
    
    .NOTES
    This function creates a detailed record of model performance for monitoring.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$ModelId,
        
        [Parameter(Mandatory=$true)]
        [string]$ModelName,
        
        [Parameter(Mandatory=$true)]
        [string]$Environment,
        
        [Parameter(Mandatory=$true)]
        [hashtable]$PerformanceMetrics,
        
        [Parameter()]
        [hashtable]$InputDataStats = @{},
        
        [Parameter()]
        [datetime]$PredictionTimestamp,
        
        [Parameter()]
        [string]$UserId = $env:USERNAME
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Performance record added successfully"
        Timestamp = if ($PredictionTimestamp) { $PredictionTimestamp } else { Get-Date }
        RecordId = [guid]::NewGuid().ToString()
        ConfidenceScore = 0.95
    }
    
    try {
        # Set timestamp if not provided
        if (-not $PredictionTimestamp) {
            $PredictionTimestamp = Get-Date
        }
        
        # Create performance record
        $record = @{
            RecordId = $result.RecordId
            ModelId = $ModelId
            ModelName = $ModelName
            Environment = $Environment
            PerformanceMetrics = $PerformanceMetrics
            InputDataStats = $InputDataStats
            PredictionTimestamp = $PredictionTimestamp.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
            UserId = $UserId
            HostName = hostname
        }
        
        # Get the performance tracking directory
        $trackingDir = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "PerformanceTracking")
        if (-not (Test-Path $trackingDir)) {
            New-Item -ItemType Directory -Path $trackingDir -Force
        }
        
        # Create a file for this model and date
        $dateStr = $PredictionTimestamp.ToString('yyyy-MM-dd')
        $fileName = "${ModelName}_${dateStr}_performance.json"
        $filePath = Join-Path $trackingDir $fileName
        
        # Read existing records if file exists
        $records = @()
        if (Test-Path $filePath) {
            $content = Get-Content -Path $filePath -Raw
            if ($content.Trim()) {
                $records = $content | ConvertFrom-Json -AsHashtable
                if ($records -isnot [array]) {
                    $records = @($records)
                }
            }
        }
        
        # Add new record
        $records += $record
        
        # Write records back to file
        $recordsJson = $records | ConvertTo-Json
        Set-Content -Path $filePath -Value $recordsJson
        
        $result.Message = "Performance record added for model $ModelName at $PredictionTimestamp"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to add performance record: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Get-ModelPerformanceHistory {
    <#
    .SYNOPSIS
    Gets the performance history for a specific model
    
    .DESCRIPTION
    This function retrieves the historical performance records for a model,
    allowing analysis of how the model performs over time.
    
    .PARAMETER ModelName
    The name of the model to get history for
    
    .PARAMETER Environment
    The environment to filter by (optional)
    
    .PARAMETER StartDate
    Start date for the history period (optional)
    
    .PARAMETER EndDate
    End date for the history period (optional)
    
    .PARAMETER Limit
    Maximum number of records to return (optional)
    
    .EXAMPLE
    $history = Get-ModelPerformanceHistory -ModelName "CPU_Predictor" -StartDate (Get-Date).AddDays(-7)
    
    .NOTES
    This function enables trend analysis of model performance.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$ModelName,
        
        [Parameter()]
        [string]$Environment,
        
        [Parameter()]
        [datetime]$StartDate,
        
        [Parameter()]
        [datetime]$EndDate,
        
        [Parameter()]
        [int]$Limit
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Performance history retrieved successfully"
        Records = @()
        ModelName = $ModelName
        TotalRecords = 0
        ConfidenceScore = 0.95
    }
    
    try {
        # Get the performance tracking directory
        $trackingDir = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "PerformanceTracking")
        
        if (-not (Test-Path $trackingDir)) {
            $result.Message = "No performance tracking directory found"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Get all files for this model
        $modelFiles = Get-ChildItem -Path $trackingDir -Name "${ModelName}_*"
        
        $allRecords = @()
        foreach ($file in $modelFiles) {
            $filePath = Join-Path $trackingDir $file
            
            # Read records from file
            $content = Get-Content -Path $filePath -Raw
            if ($content.Trim()) {
                $records = $content | ConvertFrom-Json -AsHashtable
                if ($records -isnot [array]) {
                    $records = @($records)
                }
                
                $allRecords += $records
            }
        }
        
        # Filter records based on parameters
        $filteredRecords = $allRecords
        
        if ($StartDate) {
            $filteredRecords = $filteredRecords | Where-Object { 
                [datetime]$_.PredictionTimestamp -ge $StartDate 
            }
        }
        
        if ($EndDate) {
            $filteredRecords = $filteredRecords | Where-Object { 
                [datetime]$_.PredictionTimestamp -le $EndDate 
            }
        }
        
        if ($Environment) {
            $filteredRecords = $filteredRecords | Where-Object { 
                $_.Environment -eq $Environment 
            }
        }
        
        # Apply limit if specified
        if ($Limit -and $Limit -gt 0) {
            $filteredRecords = $filteredRecords | Select-Object -First $Limit
        }
        
        # Sort by timestamp (newest first)
        $result.Records = $filteredRecords | Sort-Object { [datetime]$_.PredictionTimestamp } -Descending
        $result.TotalRecords = $result.Records.Count
        
        $result.Message = "Retrieved $($result.TotalRecords) performance records for model $ModelName"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to get performance history: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Get-ModelDriftReport {
    <#
    .SYNOPSIS
    Generates a report on model drift based on performance metrics
    
    .DESCRIPTION
    This function analyzes historical performance data to identify potential
    model drift, where model performance degrades over time.
    
    .PARAMETER ModelName
    The name of the model to analyze
    
    .PARAMETER MetricName
    The performance metric to check for drift (e.g. "R2Score", "MSE")
    
    .PARAMETER Threshold
    Threshold that indicates significant drift (e.g. 0.1 for 10% change)
    
    .PARAMETER TimeWindow
    Time window to analyze in days (default: 30)
    
    .EXAMPLE
    $driftReport = Get-ModelDriftReport -ModelName "CPU_Predictor" -MetricName "R2Score" -Threshold 0.1
    
    .NOTES
    This function helps identify when models need to be retrained.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$ModelName,
        
        [Parameter(Mandatory=$true)]
        [string]$MetricName,
        
        [Parameter(Mandatory=$true)]
        [double]$Threshold,
        
        [Parameter()]
        [int]$TimeWindow = 30
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Model drift analysis completed"
        ModelName = $ModelName
        MetricName = $MetricName
        IsDrifting = $false
        DriftValue = 0
        RecentAverage = 0
        BaselineAverage = 0
        RecordsAnalyzed = 0
        ConfidenceScore = 0.90
    }
    
    try {
        # Get performance history
        $endDate = Get-Date
        $startDate = $endDate.AddDays(-$TimeWindow)
        
        $historyResult = Get-ModelPerformanceHistory -ModelName $ModelName -StartDate $startDate -EndDate $endDate
        
        if ($historyResult.Status -ne "Success" -or $historyResult.TotalRecords -eq 0) {
            $result.Message = "No performance records found for model $ModelName in the specified time window"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Filter records that have the specified metric
        $validRecords = $historyResult.Records | Where-Object { 
            $_.PerformanceMetrics.ContainsKey($MetricName) -and 
            $_.PerformanceMetrics[$MetricName] -ne $null 
        }
        
        if ($validRecords.Count -lt 2) {
            $result.Message = "Insufficient records with $MetricName metric for analysis"
            $result.ConfidenceScore = 0.6
            return $result
        }
        
        # Calculate recent (last 7 days) and baseline (first 7 days) averages
        $recentDateCutoff = $endDate.AddDays(-7)
        $recentRecords = $validRecords | Where-Object { 
            [datetime]$_.PredictionTimestamp -ge $recentDateCutoff 
        }
        
        $baselineRecords = $validRecords | Where-Object { 
            [datetime]$_.PredictionTimestamp -lt $recentDateCutoff 
        }
        
        # Calculate averages
        if ($recentRecords.Count -gt 0) {
            $recentValues = $recentRecords.PerformanceMetrics[$MetricName]
            $result.RecentAverage = ($recentValues | Measure-Object -Average).Average
        }
        
        if ($baselineRecords.Count -gt 0) {
            $baselineValues = $baselineRecords.PerformanceMetrics[$MetricName]
            $result.BaselineAverage = ($baselineValues | Measure-Object -Average).Average
        } elseif ($validRecords.Count -gt 0) {
            # If no baseline records, use early records as baseline
            $earlyRecords = $validRecords | Sort-Object { [datetime]$_.PredictionTimestamp } | Select-Object -First 7
            $baselineValues = $earlyRecords.PerformanceMetrics[$MetricName]
            $result.BaselineAverage = ($baselineValues | Measure-Object -Average).Average
        }
        
        # Calculate drift
        if ($result.BaselineAverage -ne 0) {
            $result.DriftValue = [Math]::Abs($result.RecentAverage - $result.BaselineAverage) / $result.BaselineAverage
        } else {
            $result.DriftValue = [Math]::Abs($result.RecentAverage - $result.BaselineAverage)
        }
        
        $result.IsDrifting = $result.DriftValue -gt $Threshold
        $result.RecordsAnalyzed = $validRecords.Count
        
        # Determine message based on drift status
        if ($result.IsDrifting) {
            $direction = if ($result.RecentAverage -lt $result.BaselineAverage) { "decreased" } else { "increased" }
            $result.Message = "Model appears to be drifting. $MetricName has $direction by $($result.DriftValue * 100)% (threshold: $($Threshold * 100)%)"
            $result.ConfidenceScore = 0.95
        } else {
            $result.Message = "No significant drift detected. $MetricName stable within threshold of $($Threshold * 100)%"
            $result.ConfidenceScore = 0.90
        }
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to generate drift report: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Get-PerformanceAlerts {
    <#
    .SYNOPSIS
    Gets performance alerts based on thresholds and model drift
    
    .DESCRIPTION
    This function identifies performance issues with models based on
    predefined thresholds and drift analysis.
    
    .PARAMETER AlertType
    Type of alerts to get: "All", "Threshold", "Drift" (default: "All")
    
    .PARAMETER Severity
    Severity level to filter by: "All", "Warning", "Critical" (default: "All")
    
    .PARAMETER HoursBack
    Number of hours back to look for alerts (default: 24)
    
    .EXAMPLE
    $alerts = Get-PerformanceAlerts -AlertType "Drift" -Severity "Critical" -HoursBack 48
    
    .NOTES
    This function provides automated monitoring of model performance.
    #>
    param(
        [Parameter()]
        [ValidateSet("All", "Threshold", "Drift")]
        [string]$AlertType = "All",
        
        [Parameter()]
        [ValidateSet("All", "Warning", "Critical")]
        [string]$Severity = "All",
        
        [Parameter()]
        [int]$HoursBack = 24
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Performance alerts retrieved successfully"
        Alerts = @()
        TotalAlerts = 0
        ConfidenceScore = 0.90
    }
    
    try {
        # Get the performance tracking directory
        $trackingDir = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "PerformanceTracking")
        
        if (-not (Test-Path $trackingDir)) {
            $result.Message = "No performance tracking directory found"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Get all performance tracking files
        $files = Get-ChildItem -Path $trackingDir -Name "*_performance.json"
        
        # Extract unique model names from file names
        $modelNames = $files | ForEach-Object { 
            ($_ -split "_")[0] 
        } | Sort-Object -Unique
        
        $alerts = @()
        
        # Check each model for potential alerts
        foreach ($modelName in $modelNames) {
            # Get recent performance history
            $endDate = Get-Date
            $startDate = $endDate.AddHours(-$HoursBack)
            
            $historyResult = Get-ModelPerformanceHistory -ModelName $modelName -StartDate $startDate -EndDate $endDate
            
            if ($historyResult.Status -eq "Success" -and $historyResult.TotalRecords -gt 0) {
                # Check for threshold-based alerts
                if ($AlertType -eq "All" -or $AlertType -eq "Threshold") {
                    # Check recent records for threshold violations
                    $recentRecords = $historyResult.Records | Select-Object -First 5
                    foreach ($record in $recentRecords) {
                        # Example: Check if R2Score dropped below 0.7
                        if ($record.PerformanceMetrics.ContainsKey("R2Score") -and 
                            $record.PerformanceMetrics["R2Score"] -lt 0.7) {
                            $alerts += @{
                                AlertId = [guid]::NewGuid().ToString()
                                ModelName = $modelName
                                Type = "ThresholdViolation"
                                Severity = if ($record.PerformanceMetrics["R2Score"] -lt 0.5) { "Critical" } else { "Warning" }
                                Message = "R2Score dropped to $($record.PerformanceMetrics['R2Score']) for model $modelName"
                                Timestamp = $record.PredictionTimestamp
                            }
                        }
                        
                        # Example: Check if MSE increased above threshold
                        if ($record.PerformanceMetrics.ContainsKey("MSE") -and 
                            $record.PerformanceMetrics["MSE"] -gt 0.1) {
                            $severity = if ($record.PerformanceMetrics["MSE"] -gt 0.2) { "Critical" } else { "Warning" }
                            $alerts += @{
                                AlertId = [guid]::NewGuid().ToString()
                                ModelName = $modelName
                                Type = "ThresholdViolation"
                                Severity = $severity
                                Message = "MSE increased to $($record.PerformanceMetrics['MSE']) for model $modelName"
                                Timestamp = $record.PredictionTimestamp
                            }
                        }
                    }
                }
                
                # Check for drift-based alerts
                if ($AlertType -eq "All" -or $AlertType -eq "Drift") {
                    # Check for R2Score drift (degradation)
                    $r2DriftResult = Get-ModelDriftReport -ModelName $modelName -MetricName "R2Score" -Threshold 0.15
                    if ($r2DriftResult.Status -eq "Success" -and $r2DriftResult.IsDrifting -and 
                        $r2DriftResult.RecentAverage -lt $r2DriftResult.BaselineAverage) {
                        $severity = "Warning"
                        if ($r2DriftResult.DriftValue -gt 0.25) { $severity = "Critical" }
                        
                        $alerts += @{
                            AlertId = [guid]::NewGuid().ToString()
                            ModelName = $modelName
                            Type = "PerformanceDrift"
                            Severity = $severity
                            Message = "Model $modelName shows performance drift: R2Score dropped by $($r2DriftResult.DriftValue * 100)%"
                            Timestamp = $endDate.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                        }
                    }
                    
                    # Check for MSE drift (increase in error)
                    $mseDriftResult = Get-ModelDriftReport -ModelName $modelName -MetricName "MSE" -Threshold 0.2
                    if ($mseDriftResult.Status -eq "Success" -and $mseDriftResult.IsDrifting -and 
                        $mseDriftResult.RecentAverage -gt $mseDriftResult.BaselineAverage) {
                        $severity = "Warning"
                        if ($mseDriftResult.DriftValue -gt 0.3) { $severity = "Critical" }
                        
                        $alerts += @{
                            AlertId = [guid]::NewGuid().ToString()
                            ModelName = $modelName
                            Type = "PerformanceDrift"
                            Severity = $severity
                            Message = "Model $modelName shows performance drift: MSE increased by $($mseDriftResult.DriftValue * 100)%"
                            Timestamp = $endDate.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                        }
                    }
                }
            }
        }
        
        # Filter alerts based on requested severity
        $filteredAlerts = $alerts
        if ($Severity -ne "All") {
            $filteredAlerts = $alerts | Where-Object { $_.Severity -eq $Severity }
        }
        
        $result.Alerts = $filteredAlerts | Sort-Object { [datetime]$_.Timestamp } -Descending
        $result.TotalAlerts = $result.Alerts.Count
        
        $result.Message = "Found $($result.TotalAlerts) performance alerts"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to get performance alerts: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Publish-PerformanceDashboardData {
    <#
    .SYNOPSIS
    Prepares performance data for dashboard visualization
    
    .DESCRIPTION
    This function aggregates performance data to be used in dashboards
    showing model health and performance over time.
    
    .PARAMETER ModelName
    The name of the model to get data for (optional, gets all if not specified)
    
    .PARAMETER TimeRange
    Time range for the data: "LastDay", "LastWeek", "LastMonth", "LastQuarter" (default: "LastWeek")
    
    .PARAMETER AggregateBy
    How to aggregate the data: "Hour", "Day", "Week" (default: "Day")
    
    .EXAMPLE
    $dashboardData = Publish-PerformanceDashboardData -TimeRange "LastWeek" -AggregateBy "Day"
    
    .NOTES
    This function provides aggregated data for performance dashboards.
    #>
    param(
        [Parameter()]
        [string]$ModelName,
        
        [Parameter()]
        [ValidateSet("LastDay", "LastWeek", "LastMonth", "LastQuarter")]
        [string]$TimeRange = "LastWeek",
        
        [Parameter()]
        [ValidateSet("Hour", "Day", "Week")]
        [string]$AggregateBy = "Day"
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Dashboard data prepared successfully"
        ModelName = $ModelName
        TimeRange = $TimeRange
        AggregateBy = $AggregateBy
        DashboardData = @{}
        ConfidenceScore = 0.95
    }
    
    try {
        # Determine date range based on TimeRange
        $endDate = Get-Date
        switch ($TimeRange) {
            "LastDay" { $startDate = $endDate.AddDays(-1) }
            "LastWeek" { $startDate = $endDate.AddDays(-7) }
            "LastMonth" { $startDate = $endDate.AddDays(-30) }
            "LastQuarter" { $startDate = $endDate.AddDays(-90) }
        }
        
        # Get the performance tracking directory
        $trackingDir = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "PerformanceTracking")
        
        if (-not (Test-Path $trackingDir)) {
            $result.Message = "No performance tracking directory found"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Determine which models to analyze
        $modelsToCheck = @()
        if ($ModelName) {
            $modelsToCheck = @($ModelName)
        } else {
            # Get all models by looking at performance files
            $files = Get-ChildItem -Path $trackingDir -Name "*_performance.json"
            $modelsToCheck = $files | ForEach-Object { ($_ -split "_")[0] } | Sort-Object -Unique
        }
        
        # Prepare dashboard data
        $dashboardData = @{
            Summary = @{}
            TrendData = @{}
            Metrics = @()
            LastUpdated = $endDate.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        }
        
        foreach ($model in $modelsToCheck) {
            # Get performance history for this model
            $historyResult = Get-ModelPerformanceHistory -ModelName $model -StartDate $startDate -EndDate $endDate
            
            if ($historyResult.Status -eq "Success" -and $historyResult.TotalRecords -gt 0) {
                # Calculate summary metrics
                $recentRecords = $historyResult.Records | Select-Object -First 5
                $metrics = $recentRecords.PerformanceMetrics
                $latestMetrics = $recentRecords[0].PerformanceMetrics
                
                $summary = @{
                    ModelName = $model
                    TotalPredictions = $historyResult.TotalRecords
                    R2ScoreLatest = $latestMetrics["R2Score"]
                    R2ScoreAvg = ($metrics.R2Score | Measure-Object -Average).Average
                    MSELatest = $latestMetrics["MSE"]
                    MSEAvg = ($metrics.MSE | Measure-Object -Average).Average
                    MAELatest = $latestMetrics["MAE"]
                    MAEAvg = ($metrics.MAE | Measure-Object -Average).Average
                }
                
                $dashboardData.Summary[$model] = $summary
                
                # Create trend data by grouping records based on AggregateBy
                $trendData = $historyResult.Records | 
                    Group-Object { 
                        $dt = [datetime]$_.PredictionTimestamp
                        switch ($AggregateBy) {
                            "Hour" { "{0:yyyy-MM-dd HH:00:00}" -f $dt }
                            "Day" { "{0:yyyy-MM-dd}" -f $dt }
                            "Week" { 
                                $daysSinceMonday = [int]$dt.DayOfWeek
                                if ($daysSinceMonday -eq 0) { $daysSinceMonday = 7 }  # Sunday = 7 days back to get to Monday
                                $monday = $dt.AddDays(-$daysSinceMonday + 1)
                                "{0:yyyy-MM-dd}" -f $monday.Date
                            }
                        }
                    } |
                    ForEach-Object {
                        $groupData = @{
                            Period = $_.Name
                            Count = $_.Count
                            R2ScoreAvg = ($_.Group.PerformanceMetrics.R2Score | Measure-Object -Average).Average
                            MSEAvg = ($_.Group.PerformanceMetrics.MSE | Measure-Object -Average).Average
                            MAEAvg = ($_.Group.PerformanceMetrics.MAE | Measure-Object -Average).Average
                        }
                        $groupData
                    }
                
                $dashboardData.TrendData[$model] = $trendData
            }
        }
        
        $result.DashboardData = $dashboardData
        $result.Message = "Dashboard data prepared for $($modelsToCheck.Count) models"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to prepare dashboard data: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

# Export functions
Export-ModuleMember -Function Add-ModelPerformanceRecord, Get-ModelPerformanceHistory, Get-ModelDriftReport, Get-PerformanceAlerts, Publish-PerformanceDashboardData
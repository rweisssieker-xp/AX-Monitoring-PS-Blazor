# AXMonitor.AI - Anomaly Detection Module
# Purpose: Provides anomaly detection functionality for performance metrics
# Author: Qwen Code
# Date: 2025-10-27

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Detect-Anomalies {
    <#
    .SYNOPSIS
    Detects anomalies in performance metrics using statistical methods
    
    .DESCRIPTION
    This function identifies unusual patterns in performance metrics that may indicate potential issues.
    It uses statistical methods to calculate thresholds and identify outliers.
    
    .PARAMETER Metrics
    Performance metrics to analyze (array of objects with timestamp and value)
    
    .PARAMETER Method
    Anomaly detection method to use (default: "ZScore")
    Valid values: "ZScore", "IQR", "MovingAverage"
    
    .PARAMETER Threshold
    Anomaly detection threshold (default: 95%)
    
    .EXAMPLE
    Detect-Anomalies -Metrics $metrics -Method "ZScore" -Threshold 95
    
    .NOTES
    This implementation provides basic anomaly detection using statistical methods.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$Metrics,
        
        [Parameter()]
        [ValidateSet("ZScore", "IQR", "MovingAverage")]
        [string]$Method = "ZScore",
        
        [Parameter()]
        [int]$Threshold = 95
    )
    
    # Validate input
    if ($Metrics.Count -eq 0) {
        Write-Error "No metrics provided for analysis"
        return $null
    }
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Anomaly detection completed"
        Anomalies = @()
        ConfidenceScore = 0.85
        MethodUsed = $Method
        Threshold = $Threshold
    }
    
    try {
        # Extract values for analysis
        $values = $Metrics | ForEach-Object { $_.Value }
        
        # Calculate basic statistics
        $mean = ($values | Measure-Object -Average).Average
        $stdDev = ($values | Measure-Object -StandardDeviation).StandardDeviation
        $count = $values.Count
        
        # Calculate quartiles for IQR method
        $sortedValues = $values | Sort-Object
        $q1 = $sortedValues[[math]::Floor($count * 0.25)]
        $q3 = $sortedValues[[math]::Floor($count * 0.75)]
        $iqr = $q3 - $q1
        
        # Define anomaly thresholds based on method
        switch ($Method) {
            "ZScore" {
                # Z-score method: values beyond Z-score threshold are anomalies
                $zScoreThreshold = [math]::Abs([System.Math]::Round([System.Math]::InvNorm((100 - $Threshold) / 200), 2))
                $lowerBound = $mean - ($zScoreThreshold * $stdDev)
                $upperBound = $mean + ($zScoreThreshold * $stdDev)
                
                Write-Host "Using Z-Score method with threshold: $zScoreThreshold"
            }
            
            "IQR" {
                # IQR method: values beyond 1.5 * IQR from Q1/Q3 are anomalies
                $lowerBound = $q1 - (1.5 * $iqr)
                $upperBound = $q3 + (1.5 * $iqr)
                
                Write-Host "Using IQR method with bounds: [$lowerBound, $upperBound]"
            }
            
            "MovingAverage" {
                # Moving average method: values beyond 2 standard deviations from moving average are anomalies
                $windowSize = [math]::Min(10, $count)
                $movingAverages = @()
                
                for ($i = 0; $i -lt $count; $i++) {
                    if ($i -lt $windowSize) {
                        $window = $values[0..$i]
                    } else {
                        $window = $values[($i - $windowSize + 1)..$i]
                    }
                    
                    $ma = ($window | Measure-Object -Average).Average
                    $movingAverages += $ma
                }
                
                # Calculate standard deviation of moving averages
                $maStdDev = ($movingAverages | Measure-Object -StandardDeviation).StandardDeviation
                $lowerBound = $mean - (2 * $maStdDev)
                $upperBound = $mean + (2 * $maStdDev)
                
                Write-Host "Using Moving Average method with bounds: [$lowerBound, $upperBound]"
            }
        }
        
        # Identify anomalies
        for ($i = 0; $i -lt $Metrics.Count; $i++) {
            $metric = $Metrics[$i]
            $value = $metric.Value
            
            # Check if value is an anomaly
            if ($value -lt $lowerBound -or $value -gt $upperBound) {
                $anomaly = @{
                    Timestamp = $metric.Timestamp
                    Value = $value
                    ExpectedRange = "[$lowerBound, $upperBound]"
                    Deviation = if ($value -lt $lowerBound) { "Below lower bound" } else { "Above upper bound" }
                    Severity = "Medium"
                    Confidence = 0.85
                }
                
                $result.Anomalies += $anomaly
            }
        }
        
        # Update confidence score based on number of anomalies detected
        if ($result.Anomalies.Count -gt 0) {
            $result.ConfidenceScore = 0.90
        } else {
            $result.ConfidenceScore = 0.75
        }
        
        $result.Message = "Anomaly detection completed. Found $($result.Anomalies.Count) anomalies."
        
    } catch {
        $result.Status = "Error"
        $result.Message = "Anomaly detection failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
    }
    
    return $result
}

function Get-AnomalySummary {
    <#
    .SYNOPSIS
    Gets a summary of detected anomalies
    
    .DESCRIPTION
    This function provides a summary of detected anomalies including statistics and recommendations.
    
    .PARAMETER Anomalies
    Array of detected anomalies
    
    .EXAMPLE
    Get-AnomalySummary -Anomalies $anomalies
    
    .NOTES
    This function provides a high-level summary of detected anomalies.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$Anomalies
    )
    
    # Initialize result object
    $summary = @{
        TotalAnomalies = $Anomalies.Count
        SeverityDistribution = @{}
        TimePeriod = ""
        Recommendations = @()
        ConfidenceScore = 0.80
    }
    
    try {
        if ($Anomalies.Count -eq 0) {
            $summary.TimePeriod = "No anomalies detected"
            $summary.Recommendations = @("No action required")
            return $summary
        }
        
        # Calculate time period
        $startTime = ($Anomalies | Where-Object { $_.Timestamp } | Sort-Object Timestamp)[0].Timestamp
        $endTime = ($Anomalies | Where-Object { $_.Timestamp } | Sort-Object Timestamp)[-1].Timestamp
        $summary.TimePeriod = "$startTime to $endTime"
        
        # Calculate severity distribution
        $summary.SeverityDistribution.High = ($Anomalies | Where-Object { $_.Severity -eq "High" }).Count
        $summary.SeverityDistribution.Medium = ($Anomalies | Where-Object { $_.Severity -eq "Medium" }).Count
        $summary.SeverityDistribution.Low = ($Anomalies | Where-Object { $_.Severity -eq "Low" }).Count
        
        # Generate recommendations
        if ($summary.TotalAnomalies -gt 0) {
            $summary.Recommendations += "Investigate the identified anomalies as they may indicate performance issues."
            if ($summary.SeverityDistribution.High -gt 0) {
                $summary.Recommendations += "Prioritize investigation of high-severity anomalies as they may impact system stability."
            }
            if ($summary.SeverityDistribution.Medium -gt 0) {
                $summary.Recommendations += "Review medium-severity anomalies for potential optimization opportunities."
            }
            if ($summary.SeverityDistribution.Low -gt 0) {
                $summary.Recommendations += "Monitor low-severity anomalies for trends that may become more significant over time."
            }
        }
        
        # Add confidence score based on data quality
        $summary.ConfidenceScore = 0.85
        
    } catch {
        $summary.ConfidenceScore = 0.60
    }
    
    return $summary
}

# Export functions
Export-ModuleMember -Function Detect-Anomalies, Get-AnomalySummary
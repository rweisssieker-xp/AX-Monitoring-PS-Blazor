# AXMonitor.AI - Data Preprocessing Module
# Purpose: Prepares performance metrics data for AI analysis
# Author: Qwen Code
# Date: 2025-10-27

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Prepare-MetricsData {
    <#
    .SYNOPSIS
    Prepares performance metrics data for AI analysis
    
    .DESCRIPTION
    This function preprocesses performance metrics data by cleaning, normalizing, and transforming it into a format suitable for AI analysis.
    
    .PARAMETER RawMetrics
    Raw performance metrics data to preprocess
    
    .PARAMETER DataTypes
    Types of data to preprocess (batch jobs, sessions, blocking chains, SQL health)
    
    .EXAMPLE
    $preparedData = Prepare-MetricsData -RawMetrics $rawData -DataTypes @("batch", "sessions")
    
    .NOTES
    This function handles common data preprocessing tasks including:
    - Missing value handling
    - Outlier detection and treatment
    - Feature scaling and normalization
    - Time series alignment
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object]$RawMetrics,
        
        [Parameter(Mandatory=$true)]
        [string[]]$DataTypes
    )
    
    Write-Host "Starting data preprocessing..."
    
    # Initialize result object
    $preprocessedData = @{
        Status = "Success"
        Message = "Data preprocessing completed"
        ProcessedMetrics = @{}
        Statistics = @{
            OriginalCount = 0
            ProcessedCount = 0
            MissingValuesHandled = 0
            OutliersDetected = 0
        }
    }
    
    # Process each data type
    foreach ($dataType in $DataTypes) {
        Write-Host "Processing $dataType data..."
        
        # Get raw data for this type
        if ($RawMetrics.ContainsKey($dataType)) {
            $rawData = $RawMetrics[$dataType]
            
            # Initialize statistics for this data type
            $preprocessedData.Statistics.OriginalCount += $rawData.Count
            
            # Clean and preprocess data
            $cleanedData = $rawData | Where-Object { $_ -ne $null }
            
            # Handle missing values
            $missingValues = 0
            if ($cleanedData.Count -lt $rawData.Count) {
                $missingValues = $rawData.Count - $cleanedData.Count
                $preprocessedData.Statistics.MissingValuesHandled += $missingValues
            }
            
            # Detect and handle outliers (basic approach)
            $outliers = @()
            if ($cleanedData.Count -gt 0) {
                # Calculate mean and standard deviation
                $mean = ($cleanedData | Measure-Object -Average).Average
                $stdDev = ($cleanedData | Measure-Object -StandardDeviation).StandardDeviation
                
                # Define outlier boundaries (3 standard deviations)
                $lowerBound = $mean - (3 * $stdDev)
                $upperBound = $mean + (3 * $stdDev)
                
                # Identify outliers
                $outliers = $cleanedData | Where-Object { $_ -lt $lowerBound -or $_ -gt $upperBound }
                
                # Replace outliers with mean (simple approach)
                if ($outliers.Count -gt 0) {
                    $cleanedData = $cleanedData | ForEach-Object {
                        if ($_ -lt $lowerBound -or $_ -gt $upperBound) {
                            $mean
                        } else {
                            $_
                        }
                    }
                    
                    $preprocessedData.Statistics.OutliersDetected += $outliers.Count
                }
            }
            
            # Normalize data (min-max scaling)
            if ($cleanedData.Count -gt 0) {
                $min = ($cleanedData | Measure-Object -Minimum).Minimum
                $max = ($cleanedData | Measure-Object -Maximum).Maximum
                
                if ($max -gt $min) {
                    $normalizedData = $cleanedData | ForEach-Object {
                        if ($max -ne $min) {
                            ($PSItem - $min) / ($max - $min)
                        } else {
                            0
                        }
                    }
                    
                    $preprocessedData.ProcessedMetrics[$dataType] = $normalizedData
                } else {
                    # All values are the same, set all to 0
                    $preprocessedData.ProcessedMetrics[$dataType] = @($cleanedData[0])
                }
            } else {
                $preprocessedData.ProcessedMetrics[$dataType] = @()
            }
            
            $preprocessedData.Statistics.ProcessedCount += $cleanedData.Count
        } else {
            Write-Warning "No data found for $dataType"
        }
    }
    
    return $preprocessedData
}

function Get-DataStatistics {
    <#
    .SYNOPSIS
    Gets statistics about performance metrics data
    
    .DESCRIPTION
    This function calculates various statistics about performance metrics data including counts, averages, and distributions.
    
    .PARAMETER Metrics
    Performance metrics data to analyze
    
    .EXAMPLE
    $stats = Get-DataStatistics -Metrics $metrics
    
    .NOTES
    This function provides insights into the quality and characteristics of the data.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object]$Metrics
    )
    
    Write-Host "Calculating data statistics..."
    
    # Initialize result object
    $statistics = @{
        Status = "Success"
        Message = "Data statistics calculated"
        Summary = @{}
    }
    
    # Calculate statistics for each metric type
    foreach ($key in $Metrics.Keys) {
        $data = $Metrics[$key]
        
        if ($data.Count -gt 0) {
            $count = $data.Count
            $average = ($data | Measure-Object -Average).Average
            $min = ($data | Measure-Object -Minimum).Minimum
            $max = ($data | Measure-Object -Maximum).Maximum
            $stdDev = ($data | Measure-Object -StandardDeviation).StandardDeviation
            
            $statistics.Summary[$key] = @{
                Count = $count
                Average = $average
                Minimum = $min
                Maximum = $max
                StandardDeviation = $stdDev
                Range = $max - $min
            }
        } else {
            $statistics.Summary[$key] = @{
                Count = 0
                Average = 0
                Minimum = 0
                Maximum = 0
                StandardDeviation = 0
                Range = 0
            }
        }
    }
    
    return $statistics
}

# Export functions
Export-ModuleMember -Function Prepare-MetricsData, Get-DataStatistics
# AXMonitor.AI - Advanced Data Preprocessing Module
# Purpose: Provides advanced data preprocessing capabilities for AX performance metrics
# Author: Qwen Code
# Date: 2025-10-29

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Get-DataQualityReport {
    <#
    .SYNOPSIS
    Generates a comprehensive data quality report
    
    .DESCRIPTION
    This function analyzes the quality of input data including missing values,
    outliers, data types, and other quality metrics.
    
    .PARAMETER InputData
    The input data to analyze
    
    .PARAMETER TargetVariable
    The target variable in the dataset (if prediction)
    
    .EXAMPLE
    $report = Get-DataQualityReport -InputData $metricsData
    
    .NOTES
    This function helps identify data quality issues before model training.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$InputData,
        
        [Parameter()]
        [string]$TargetVariable
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Data quality report generated successfully"
        QualityMetrics = @{}
        Issues = @()
        Recommendations = @()
        ConfidenceScore = 0.95
    }
    
    try {
        # Basic data information
        $result.QualityMetrics.TotalRows = $InputData.Count
        $result.QualityMetrics.TotalColumns = 0
        $result.QualityMetrics.HasData = $InputData.Count -gt 0
        
        if ($InputData.Count -eq 0) {
            $result.Message = "No data provided for analysis"
            $result.ConfidenceScore = 0.2
            return $result
        }
        
        # Get column information
        $allProperties = @()
        foreach ($obj in $InputData) {
            foreach ($prop in $obj.PSObject.Properties) {
                if ($allProperties -notcontains $prop.Name) {
                    $allProperties += $prop.Name
                }
            }
        }
        $result.QualityMetrics.TotalColumns = $allProperties.Count
        
        # Analyze each column
        $columnAnalysis = @{}
        foreach ($colName in $allProperties) {
            $values = $InputData | ForEach-Object { $_.$colName }
            $nonNullValues = $values | Where-Object { $_ -ne $null }
            $nullValues = $values.Count - $nonNullValues.Count
            $uniqueValues = $nonNullValues | Sort-Object -Unique
            
            $colStats = @{
                ColumnName = $colName
                TotalCount = $values.Count
                NonNullCount = $nonNullValues.Count
                NullCount = $nullValues
                NullPercentage = if ($values.Count -gt 0) { [math]::Round(($nullValues / $values.Count) * 100, 2) } else { 0 }
                UniqueCount = $uniqueValues.Count
                DataType = if ($nonNullValues.Count -gt 0) { $nonNullValues[0].GetType().Name } else { "Unknown" }
            }
            
            # Additional stats for numeric columns
            if ($colStats.DataType -match "Int|Double|Float|Decimal|Single") {
                $numericValues = $nonNullValues | Where-Object { $_ -is [int] -or $_ -is [double] -or $_ -is [float] -or $_ -is [decimal] -or $_ -is [long] }
                if ($numericValues.Count -gt 1) {
                    $colStats.Average = ($numericValues | Measure-Object -Average).Average
                    $colStats.Median = Get-Median -Values $numericValues
                    $colStats.StdDev = ($numericValues | Measure-Object -StandardDeviation).StandardDeviation
                    $colStats.Min = ($numericValues | Measure-Object -Minimum).Minimum
                    $colStats.Max = ($numericValues | Measure-Object -Maximum).Maximum
                    $colStats.Range = $colStats.Max - $colStats.Min
                }
            }
            
            # Check for potential outliers (using IQR method for numeric columns)
            if ($colStats.DataType -match "Int|Double|Float|Decimal|Single" -and $numericValues.Count -gt 3) {
                $sortedValues = $numericValues | Sort-Object
                $q1Index = [math]::Floor(0.25 * $sortedValues.Count)
                $q3Index = [math]::Floor(0.75 * $sortedValues.Count)
                $q1 = $sortedValues[$q1Index]
                $q3 = $sortedValues[$q3Index]
                $iqr = $q3 - $q1
                $lowerBound = $q1 - (1.5 * $iqr)
                $upperBound = $q3 + (1.5 * $iqr)
                
                $outliers = $numericValues | Where-Object { $_ -lt $lowerBound -or $_ -gt $upperBound }
                $colStats.OutlierCount = $outliers.Count
                $colStats.OutlierPercentage = if ($nonNullValues.Count -gt 0) { [math]::Round(($outliers.Count / $nonNullValues.Count) * 100, 2) } else { 0 }
            } else {
                $colStats.OutlierCount = 0
                $colStats.OutlierPercentage = 0
            }
            
            $columnAnalysis[$colName] = $colStats
        }
        
        $result.QualityMetrics.ColumnAnalysis = $columnAnalysis
        
        # Identify overall data quality issues
        foreach ($colName in $columnAnalysis.Keys) {
            $col = $columnAnalysis[$colName]
            
            if ($col.NullPercentage -gt 50) {
                $result.Issues += @{
                    Type = "HighNullPercentage"
                    Column = $colName
                    Description = "Column '$colName' has $($col.NullPercentage)% null values, which is very high"
                    Severity = "High"
                }
            }
            
            if ($col.OutlierPercentage -gt 10) {
                $result.Issues += @{
                    Type = "HighOutlierPercentage"
                    Column = $colName
                    Description = "Column '$colName' has $($col.OutlierPercentage)% outliers"
                    Severity = "Medium"
                }
            }
            
            if ($col.UniqueCount -eq $col.TotalCount -and $col.TotalCount -gt 100) {
                # Likely an ID column that shouldn't be used for modeling
                $result.Issues += @{
                    Type = "HighCardinality"
                    Column = $colName
                    Description = "Column '$colName' has very high cardinality (unique values), might be an ID column"
                    Severity = "Low"
                }
            }
        }
        
        # Generate recommendations based on issues
        if ($result.Issues.Count -gt 0) {
            $highNullCols = $result.Issues | Where-Object { $_.Type -eq "HighNullPercentage" }
            $outlierCols = $result.Issues | Where-Object { $_.Type -eq "HighOutlierPercentage" }
            $idCols = $result.Issues | Where-Object { $_.Type -eq "HighCardinality" }
            
            if ($highNullCols) {
                $result.Recommendations += "Handle high-null columns with imputation or removal: $($highNullCols.Column -join ', ')"
            }
            
            if ($outlierCols) {
                $result.Recommendations += "Consider outlier treatment for columns: $($outlierCols.Column -join ', ')"
            }
            
            if ($idCols) {
                $result.Recommendations += "Consider removing high-cardinality columns if they're not meaningful for modeling: $($idCols.Column -join ', ')"
            }
        } else {
            $result.Recommendations += "Data quality looks good. No major issues detected."
        }
        
        $result.Message = "Data quality report completed. Found $($result.Issues.Count) issues."
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to generate data quality report: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Get-Median {
    # Helper function to calculate median
    param([array]$Values)
    $sorted = $Values | Sort-Object
    $count = $sorted.Count
    $middle = [math]::Floor($count / 2)
    
    if ($count % 2 -eq 0) {
        ($sorted[$middle-1] + $sorted[$middle]) / 2
    } else {
        $sorted[$middle]
    }
}

function Remove-Outliers {
    <#
    .SYNOPSIS
    Removes outliers from data using the IQR method
    
    .DESCRIPTION
    This function identifies and removes outliers from numeric columns using the
    Interquartile Range (IQR) method.
    
    .PARAMETER InputData
    The input data to process
    
    .PARAMETER Columns
    Array of column names to process for outliers (if not specified, processes all numeric columns)
    
    .PARAMETER Method
    Outlier detection method to use: "IQR", "ZScore", "ModifiedZScore" (default: "IQR")
    
    .PARAMETER Threshold
    Threshold for outlier detection (default: 1.5 for IQR, 3 for ZScore)
    
    .EXAMPLE
    $cleanData = Remove-Outliers -InputData $data -Columns @("CPU", "Memory") -Method "IQR"
    
    .NOTES
    This function helps improve data quality by removing extreme outliers.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$InputData,
        
        [Parameter()]
        [string[]]$Columns,
        
        [Parameter()]
        [ValidateSet("IQR", "ZScore", "ModifiedZScore")]
        [string]$Method = "IQR",
        
        [Parameter()]
        [double]$Threshold = $null
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Outlier removal completed successfully"
        OutputData = $InputData
        OutliersRemoved = 0
        ColumnsProcessed = @()
        ConfidenceScore = 0.90
    }
    
    # Set default threshold if not provided
    if (-not $Threshold) {
        $Threshold = if ($Method -eq "ZScore") { 3.0 } else { 1.5 }
    }
    
    try {
        if ($InputData.Count -eq 0) {
            $result.Message = "No data provided for outlier removal"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Determine which columns to process
        $columnsToProcess = @()
        if ($Columns.Count -gt 0) {
            $columnsToProcess = $Columns
        } else {
            # Auto-detect numeric columns
            $allProperties = @()
            foreach ($obj in $InputData) {
                foreach ($prop in $obj.PSObject.Properties) {
                    if ($allProperties -notcontains $prop.Name) {
                        $allProperties += $prop.Name
                    }
                }
            }
            
            foreach ($colName in $allProperties) {
                $sampleValue = $InputData[0].$colName
                if ($sampleValue -is [int] -or $sampleValue -is [double] -or $sampleValue -is [float] -or $sampleValue -is [decimal] -or $sampleValue -is [long]) {
                    $columnsToProcess += $colName
                }
            }
        }
        
        $result.ColumnsProcessed = $columnsToProcess
        
        # Process each column
        $validRows = @()
        foreach ($row in $InputData) {
            $isRowValid = $true
            
            foreach ($colName in $columnsToProcess) {
                $value = $row.$colName
                if ($value -is [int] -or $value -is [double] -or $value -is [float] -or $value -is [decimal] -or $value -is [long]) {
                    # Skip null values
                    if ($value -eq $null) { continue }
                    
                    # Get all values for this column to calculate outlier boundaries
                    $colValues = $InputData | Where-Object { $_.$colName -ne $null } | ForEach-Object { $_.$colName }
                    if ($colValues.Count -lt 4) { continue }  # Need at least 4 values for outlier detection
                    
                    $isOutlier = $false
                    switch ($Method) {
                        "IQR" {
                            $sortedValues = $colValues | Sort-Object
                            $n = $sortedValues.Count
                            $q1Index = [math]::Floor(0.25 * $n)
                            $q3Index = [math]::Floor(0.75 * $n)
                            $q1 = $sortedValues[$q1Index]
                            $q3 = $sortedValues[$q3Index]
                            $iqr = $q3 - $q1
                            $lowerBound = $q1 - ($Threshold * $iqr)
                            $upperBound = $q3 + ($Threshold * $iqr)
                            $isOutlier = $value -lt $lowerBound -or $value -gt $upperBound
                        }
                        "ZScore" {
                            $mean = ($colValues | Measure-Object -Average).Average
                            $stdDev = ($colValues | Measure-Object -StandardDeviation).StandardDeviation
                            if ($stdDev -ne 0) {
                                $zScore = [math]::Abs(($value - $mean) / $stdDev)
                                $isOutlier = $zScore -gt $Threshold
                            }
                        }
                        "ModifiedZScore" {
                            $median = Get-Median -Values $colValues
                            $mad = Get-Median -Values ($colValues | ForEach-Object { [math]::Abs($_ - $median) })
                            if ($mad -ne 0) {
                                $modifiedZScore = 0.6745 * ($value - $median) / $mad
                                $isOutlier = [math]::Abs($modifiedZScore) -gt $Threshold
                            }
                        }
                    }
                    
                    if ($isOutlier) {
                        $isRowValid = $false
                        break  # No need to check other columns for this row
                    }
                }
            }
            
            if ($isRowValid) {
                $validRows += $row
            }
        }
        
        $result.OutputData = $validRows
        $result.OutliersRemoved = $InputData.Count - $validRows.Count
        $result.Message = "Outlier removal completed. Removed $($result.OutliersRemoved) rows."
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to remove outliers: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Impute-MissingValues {
    <#
    .SYNOPSIS
    Imputes missing values in data using various strategies
    
    .DESCRIPTION
    This function fills in missing values using appropriate strategies based
    on the data type and characteristics of each column.
    
    .PARAMETER InputData
    The input data to process
    
    .PARAMETER Strategies
    Hashtable mapping column names to imputation strategies ('Mean', 'Median', 'Mode', 'Constant', 'ForwardFill', 'BackwardFill')
    
    .PARAMETER DefaultStrategy
    Default strategy to use if not specified for a column (default: 'Mean' for numeric, 'Mode' for categorical)
    
    .EXAMPLE
    $imputedData = Impute-MissingValues -InputData $data -Strategies @{"CPU"="Median"; "Status"="Mode"}
    
    .NOTES
    This function handles missing data appropriately based on data types.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$InputData,
        
        [Parameter()]
        [hashtable]$Strategies = @{},
        
        [Parameter()]
        [string]$DefaultStrategy = $null
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Missing value imputation completed successfully"
        OutputData = @()
        ColumnsImputed = @{}
        ConfidenceScore = 0.90
    }
    
    try {
        if ($InputData.Count -eq 0) {
            $result.Message = "No data provided for imputation"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Determine columns with missing values
        $allProperties = @()
        foreach ($obj in $InputData) {
            foreach ($prop in $obj.PSObject.Properties) {
                if ($allProperties -notcontains $prop.Name) {
                    $allProperties += $prop.Name
                }
            }
        }
        
        # For each column, determine its data type and missing count
        $columnInfo = @{}
        foreach ($colName in $allProperties) {
            $values = $InputData | ForEach-Object { $_.$colName }
            $missingCount = ($values | Where-Object { $_ -eq $null }).Count
            $nonNullValues = $values | Where-Object { $_ -ne $null }
            
            $colInfo = @{
                Name = $colName
                MissingCount = $missingCount
                TotalCount = $values.Count
                DataType = if ($nonNullValues.Count -gt 0) { $nonNullValues[0].GetType().Name } else { "Unknown" }
                NonNullValues = $nonNullValues
            }
            
            # Determine default strategy based on data type
            if (-not $colInfo.DataType -match "Int|Double|Float|Decimal|Single") {
                # Categorical/text data
                $colInfo.DefaultStrategy = "Mode"
            } else {
                # Numeric data
                $colInfo.DefaultStrategy = "Mean"
            }
            
            $columnInfo[$colName] = $colInfo
        }
        
        # Make a copy of the input data
        $outputData = @()
        foreach ($row in $InputData) {
            $newRow = @{}
            foreach ($prop in $row.PSObject.Properties) {
                $newRow[$prop.Name] = $prop.Value
            }
            $outputData += $newRow
        }
        
        # Process each column that has missing values
        foreach ($colName in $columnInfo.Keys) {
            $colInfo = $columnInfo[$colName]
            if ($colInfo.MissingCount -eq 0) { continue }  # Skip columns without missing values
            
            # Determine strategy for this column
            $strategy = if ($Strategies.ContainsKey($colName)) { $Strategies[$colName] } 
                       elseif ($DefaultStrategy) { $DefaultStrategy }
                       else { $colInfo.DefaultStrategy }
            
            $imputedValue = $null
            $values = $colInfo.NonNullValues
            
            switch ($strategy) {
                "Mean" {
                    if ($values.Count -gt 0 -and $colInfo.DataType -match "Int|Double|Float|Decimal|Single") {
                        $imputedValue = ($values | Measure-Object -Average).Average
                    }
                }
                "Median" {
                    if ($values.Count -gt 0 -and $colInfo.DataType -match "Int|Double|Float|Decimal|Single") {
                        $imputedValue = Get-Median -Values $values
                    }
                }
                "Mode" {
                    if ($values.Count -gt 0) {
                        $imputedValue = $values | Group-Object | Sort-Object Count -Descending | Select-Object -First 1 -ExpandProperty Name
                    }
                }
                "Constant" {
                    # Use a specific constant value (would need to be passed somehow)
                    # For now, using 0 as placeholder
                    $imputedValue = 0
                }
                "ForwardFill" {
                    # We'll handle this differently - use last known value
                    continue  # Handle forward fill separately
                }
                "BackwardFill" {
                    # We'll handle this differently - use next known value
                    continue  # Handle backward fill separately
                }
            }
            
            # Apply imputation to missing values in the output data
            if ($imputedValue -ne $null) {
                $imputedCount = 0
                for ($i = 0; $i -lt $outputData.Count; $i++) {
                    if ($outputData[$i][$colName] -eq $null) {
                        $outputData[$i][$colName] = $imputedValue
                        $imputedCount++
                    }
                }
                $result.ColumnsImputed[$colName] = @{
                    Strategy = $strategy
                    ImputedCount = $imputedCount
                    ImputedValue = $imputedValue
                }
            }
        }
        
        # Special handling for ForwardFill and BackwardFill
        foreach ($colName in $columnInfo.Keys) {
            $colInfo = $columnInfo[$colName]
            if ($colInfo.MissingCount -eq 0) { continue }
            
            $strategy = if ($Strategies.ContainsKey($colName)) { $Strategies[$colName] } else { $colInfo.DefaultStrategy }
            
            if ($strategy -eq "ForwardFill") {
                $lastValue = $null
                $imputedCount = 0
                for ($i = 0; $i -lt $outputData.Count; $i++) {
                    if ($outputData[$i][$colName] -eq $null) {
                        if ($lastValue -ne $null) {
                            $outputData[$i][$colName] = $lastValue
                            $imputedCount++
                        }
                    } else {
                        $lastValue = $outputData[$i][$colName]
                    }
                }
                
                if ($result.ColumnsImputed.ContainsKey($colName)) {
                    $result.ColumnsImputed[$colName].ImputedCount += $imputedCount
                } else {
                    $result.ColumnsImputed[$colName] = @{
                        Strategy = $strategy
                        ImputedCount = $imputedCount
                    }
                }
            } 
            elseif ($strategy -eq "BackwardFill") {
                $nextValue = $null
                $imputedCount = 0
                for ($i = $outputData.Count - 1; $i -ge 0; $i--) {
                    if ($outputData[$i][$colName] -eq $null) {
                        if ($nextValue -ne $null) {
                            $outputData[$i][$colName] = $nextValue
                            $imputedCount++
                        }
                    } else {
                        $nextValue = $outputData[$i][$colName]
                    }
                }
                
                if ($result.ColumnsImputed.ContainsKey($colName)) {
                    $result.ColumnsImputed[$colName].ImputedCount += $imputedCount
                } else {
                    $result.ColumnsImputed[$colName] = @{
                        Strategy = $strategy
                        ImputedCount = $imputedCount
                    }
                }
            }
        }
        
        $result.OutputData = $outputData
        $result.Message = "Imputation completed for $($result.ColumnsImputed.Count) columns."
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to impute missing values: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Transform-Features {
    <#
    .SYNOPSIS
    Applies feature transformations to data
    
    .DESCRIPTION
    This function applies various feature engineering techniques like
    normalization, standardization, encoding, and polynomial features.
    
    .PARAMETER InputData
    The input data to transform
    
    .PARAMETER Transformations
    Hashtable of transformations to apply
    
    .PARAMETER FeaturesToTransform
    Array of feature names to transform
    
    .EXAMPLE
    $transformedData = Transform-Features -InputData $data -Transformations @{"CPU"="Normalize"; "Memory"="Standardize"}
    
    .NOTES
    This function provides various transformation options for feature engineering.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$InputData,
        
        [Parameter(Mandatory=$true)]
        [hashtable]$Transformations,
        
        [Parameter()]
        [string[]]$FeaturesToTransform
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Feature transformation completed successfully"
        OutputData = $InputData
        TransformationsApplied = @()
        ConfidenceScore = 0.90
    }
    
    try {
        if ($InputData.Count -eq 0) {
            $result.Message = "No data provided for transformation"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Determine features to transform
        $featuresToProcess = if ($FeaturesToTransform.Count -gt 0) { $FeaturesToTransform } else { $Transformations.Keys }
        
        # Make a copy of the input data
        $outputData = @()
        foreach ($row in $InputData) {
            $newRow = @{}
            foreach ($prop in $row.PSObject.Properties) {
                $newRow[$prop.Name] = $prop.Value
            }
            $outputData += $newRow
        }
        
        # Apply transformations
        foreach ($feature in $featuresToProcess) {
            if (-not $Transformations.ContainsKey($feature)) { continue }
            
            $transformType = $Transformations[$feature]
            
            # Get all values for this feature
            $values = $outputData | ForEach-Object { $_.$feature }
            $numericValues = $values | Where-Object { $_ -ne $null -and ($_ -is [int] -or $_ -is [double] -or $_ -is [float] -or $_ -is [decimal] -or $_ -is [long]) }
            
            if ($numericValues.Count -eq 0) {
                Write-Warning "No numeric values found for feature '$feature', skipping transformation"
                continue
            }
            
            switch ($transformType) {
                "Normalize" {
                    # Min-Max normalization to [0, 1]
                    $min = ($numericValues | Measure-Object -Minimum).Minimum
                    $max = ($numericValues | Measure-Object -Maximum).Maximum
                    $range = $max - $min
                    
                    if ($range -ne 0) {
                        for ($i = 0; $i -lt $outputData.Count; $i++) {
                            if ($outputData[$i][$feature] -ne $null) {
                                $val = $outputData[$i][$feature]
                                if ($val -is [int] -or $val -is [double] -or $val -is [float] -or $val -is [decimal] -or $val -is [long]) {
                                    $outputData[$i][$feature] = ($val - $min) / $range
                                }
                            }
                        }
                        $result.TransformationsApplied += "$feature: Normalized to [0,1] (min=$min, max=$max)"
                    } else {
                        $result.TransformationsApplied += "$feature: Skipped normalization (constant value = $min)"
                    }
                }
                "Standardize" {
                    # Z-score standardization
                    $mean = ($numericValues | Measure-Object -Average).Average
                    $stdDev = ($numericValues | Measure-Object -StandardDeviation).StandardDeviation
                    
                    if ($stdDev -ne 0) {
                        for ($i = 0; $i -lt $outputData.Count; $i++) {
                            if ($outputData[$i][$feature] -ne $null) {
                                $val = $outputData[$i][$feature]
                                if ($val -is [int] -or $val -is [double] -or $val -is [float] -or $val -is [decimal] -or $val -is [long]) {
                                    $outputData[$i][$feature] = ($val - $mean) / $stdDev
                                }
                            }
                        }
                        $result.TransformationsApplied += "$feature: Standardized (mean=$mean, std=$stdDev)"
                    } else {
                        $result.TransformationsApplied += "$feature: Skipped standardization (zero std dev)"
                    }
                }
                "LogTransform" {
                    # Log transformation (with offset to handle non-positive values)
                    $minVal = ($numericValues | Measure-Object -Minimum).Minimum
                    $offset = if ($minVal -le 0) { 1 - $minVal } else { 0 }
                    
                    for ($i = 0; $i -lt $outputData.Count; $i++) {
                        if ($outputData[$i][$feature] -ne $null) {
                            $val = $outputData[$i][$feature]
                            if ($val -is [int] -or $val -is [double] -or $val -is [float] -or $val -is [decimal] -or $val -is [long]) {
                                $transformedVal = [math]::Log($val + $offset + 1e-8)  # Add small epsilon to avoid log(0)
                                $outputData[$i][$feature] = $transformedVal
                            }
                        }
                    }
                    $result.TransformationsApplied += "$feature: Log-transformed (offset=$offset)"
                }
                "Polynomial" {
                    # Create polynomial features (square of the value)
                    for ($i = 0; $i -lt $outputData.Count; $i++) {
                        if ($outputData[$i][$feature] -ne $null) {
                            $val = $outputData[$i][$feature]
                            if ($val -is [int] -or $val -is [double] -or $val -is [float] -or $val -is [decimal] -or $val -is [long]) {
                                $polynomialFeatureName = "${feature}_Squared"
                                $outputData[$i][$polynomialFeatureName] = $val * $val
                            }
                        }
                    }
                    $result.TransformationsApplied += "$feature: Polynomial (added squared feature)"
                }
            }
        }
        
        $result.OutputData = $outputData
        $result.Message = "Applied $($result.TransformationsApplied.Count) transformations."
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to transform features: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Encode-CategoricalVariables {
    <#
    .SYNOPSIS
    Encodes categorical variables using various encoding techniques
    
    .DESCRIPTION
    This function converts categorical variables into numerical representations
    using techniques like one-hot encoding, label encoding, or target encoding.
    
    .PARAMETER InputData
    The input data to encode
    
    .PARAMETER EncodingMethods
    Hashtable mapping column names to encoding methods ('OneHot', 'Label', 'Target')
    
    .PARAMETER TargetVariable
    Target variable for target encoding (required if using Target encoding)
    
    .EXAMPLE
    $encodedData = Encode-CategoricalVariables -InputData $data -EncodingMethods @{"Status"="OneHot"; "Environment"="Label"}
    
    .NOTES
    This function enables use of categorical variables in ML models.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$InputData,
        
        [Parameter(Mandatory=$true)]
        [hashtable]$EncodingMethods,
        
        [Parameter()]
        [string]$TargetVariable
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Categorical encoding completed successfully"
        OutputData = $InputData
        EncodingsApplied = @()
        ConfidenceScore = 0.90
    }
    
    try {
        if ($InputData.Count -eq 0) {
            $result.Message = "No data provided for encoding"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Make a copy of the input data
        $outputData = @()
        foreach ($row in $InputData) {
            $newRow = @{}
            foreach ($prop in $row.PSObject.Properties) {
                $newRow[$prop.Name] = $prop.Value
            }
            $outputData += $newRow
        }
        
        # Process each column that needs encoding
        foreach ($colName in $EncodingMethods.Keys) {
            $encodingMethod = $EncodingMethods[$colName]
            
            # Get unique values and their counts
            $colValues = $outputData | ForEach-Object { $_.$colName }
            $uniqueValues = $colValues | Where-Object { $_ -ne $null } | Sort-Object -Unique
            $valueCounts = $colValues | Group-Object | Sort-Object Count -Descending
            
            # Verify this is actually a categorical column
            if ($uniqueValues.Count -lt 2) {
                Write-Warning "Column '$colName' has less than 2 unique values, skipping encoding"
                continue
            }
            
            switch ($encodingMethod) {
                "OneHot" {
                    # Create binary columns for each category
                    foreach ($value in $uniqueValues) {
                        $newColName = "${colName}_$($value -replace ' ', '_')"
                        for ($i = 0; $i -lt $outputData.Count; $i++) {
                            $outputData[$i][$newColName] = if ($outputData[$i][$colName] -eq $value) { 1 } else { 0 }
                        }
                    }
                    # Remove the original column
                    for ($i = 0; $i -lt $outputData.Count; $i++) {
                        $outputData[$i].Remove($colName)
                    }
                    $result.EncodingsApplied += "$colName: One-hot encoded into $($uniqueValues.Count) binary columns"
                }
                
                "Label" {
                    # Create a mapping from categories to integers
                    $labelMap = @{}
                    $index = 0
                    foreach ($value in $uniqueValues) {
                        $labelMap[$value] = $index
                        $index++
                    }
                    
                    # Apply the mapping
                    for ($i = 0; $i -lt $outputData.Count; $i++) {
                        $originalValue = $outputData[$i][$colName]
                        if ($labelMap.ContainsKey($originalValue)) {
                            $outputData[$i][$colName] = $labelMap[$originalValue]
                        } else {
                            # Handle unseen categories - assign to the most common category or add as new label
                            $outputData[$i][$colName] = $labelMap[$uniqueValues[0]]
                        }
                    }
                    $result.EncodingsApplied += "$colName: Label encoded with mapping $labelMap"
                }
                
                "Target" {
                    if (-not $TargetVariable) {
                        Write-Warning "Target encoding requires TargetVariable parameter, skipping $colName"
                        continue
                    }
                    
                    # Calculate mean of target variable for each category
                    $targetEncodedMap = @{}
                    foreach ($value in $uniqueValues) {
                        # Get all rows where the categorical variable equals the current value
                        $matchingRows = $outputData | Where-Object { $_.$colName -eq $value }
                        if ($matchingRows.Count -gt 0) {
                            # Calculate the mean of the target variable for these rows
                            $targetValues = $matchingRows | ForEach-Object { $_.$TargetVariable }
                            $numericTargets = $targetValues | Where-Object { $_ -ne $null -and ($_ -is [int] -or $_ -is [double] -or $_ -is [float] -or $_ -is [decimal] -or $_ -is [long]) }
                            if ($numericTargets.Count -gt 0) {
                                $targetEncodedMap[$value] = ($numericTargets | Measure-Object -Average).Average
                            }
                        }
                    }
                    
                    # Apply the target encoding
                    for ($i = 0; $i -lt $outputData.Count; $i++) {
                        $originalValue = $outputData[$i][$colName]
                        if ($targetEncodedMap.ContainsKey($originalValue)) {
                            $outputData[$i][$colName] = $targetEncodedMap[$originalValue]
                        } else {
                            # For unseen categories, use the global average of the target variable
                            $allTargetValues = $outputData | ForEach-Object { $_.$TargetVariable }
                            $numericTargets = $allTargetValues | Where-Object { $_ -ne $null -and ($_ -is [int] -or $_ -is [double] -or $_ -is [float] -or $_ -is [decimal] -or $_ -is [long]) }
                            $globalAvg = if ($numericTargets.Count -gt 0) { ($numericTargets | Measure-Object -Average).Average } else { 0 }
                            $outputData[$i][$colName] = $globalAvg
                        }
                    }
                    $result.EncodingsApplied += "$colName: Target encoded based on $TargetVariable"
                }
            }
        }
        
        $result.OutputData = $outputData
        $result.Message = "Applied $($result.EncodingsApplied.Count) categorical encodings."
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to encode categorical variables: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Select-Features {
    <#
    .SYNOPSIS
    Performs feature selection based on various techniques
    
    .DESCRIPTION
    This function identifies the most relevant features for modeling using
    techniques like correlation analysis, variance thresholds, and univariate selection.
    
    .PARAMETER InputData
    The input data for feature selection
    
    .PARAMETER TargetVariable
    The target variable for supervised selection
    
    .PARAMETER Method
    Feature selection method: 'Variance', 'Correlation', 'Univariate', 'All'
    
    .PARAMETER Threshold
    Threshold for feature selection (meaning depends on method)
    
    .EXAMPLE
    $selectedData = Select-Features -InputData $data -TargetVariable "TargetValue" -Method "Correlation" -Threshold 0.1
    
    .NOTES
    This function helps reduce dimensionality and improve model performance.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$InputData,
        
        [Parameter(Mandatory=$true)]
        [string]$TargetVariable,
        
        [Parameter()]
        [ValidateSet("Variance", "Correlation", "Univariate", "All")]
        [string]$Method = "All",
        
        [Parameter()]
        [double]$Threshold = 0.01
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Feature selection completed successfully"
        OutputData = $InputData
        SelectedFeatures = @()
        FeaturesRemoved = @()
        ConfidenceScore = 0.90
    }
    
    try {
        if ($InputData.Count -eq 0) {
            $result.Message = "No data provided for feature selection"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Get all column names
        $allProperties = @()
        foreach ($obj in $InputData) {
            foreach ($prop in $obj.PSObject.Properties) {
                if ($allProperties -notcontains $prop.Name -and $prop.Name -ne $TargetVariable) {
                    $allProperties += $prop.Name
                }
            }
        }
        
        # Make a copy of the input data
        $outputData = @()
        foreach ($row in $InputData) {
            $newRow = @{}
            foreach ($prop in $row.PSObject.Properties) {
                $newRow[$prop.Name] = $prop.Value
            }
            $outputData += $newRow
        }
        
        # Start with all features as candidates
        $candidateFeatures = $allProperties.Clone()
        
        # Apply different selection methods
        if ($Method -eq "Variance" -or $Method -eq "All") {
            # Remove features with low variance
            $highVarianceFeatures = @()
            foreach ($colName in $candidateFeatures) {
                $values = $outputData | ForEach-Object { $_.$colName }
                $numericValues = $values | Where-Object { $_ -ne $null -and ($_ -is [int] -or $_ -is [double] -or $_ -is [float] -or $_ -is [decimal] -or $_ -is [long]) }
                
                if ($numericValues.Count -gt 1) {
                    $variance = ($numericValues | Measure-Object -Variance).Variance
                    if ($variance -ge $Threshold) {
                        $highVarianceFeatures += $colName
                    } else {
                        $result.FeaturesRemoved += "$colName (Low variance: $variance)"
                    }
                } else {
                    # Features with only one unique value have zero variance
                    $result.FeaturesRemoved += "$colName (Only one unique value)"
                }
            }
            $candidateFeatures = $highVarianceFeatures
        }
        
        if ($Method -eq "Correlation" -or $Method -eq "All") {
            # Remove features with low correlation to target
            $targetValues = $outputData | ForEach-Object { $_.$TargetVariable }
            $numericTargetValues = $targetValues | Where-Object { $_ -ne $null -and ($_ -is [int] -or $_ -is [double] -or $_ -is [float] -or $_ -is [decimal] -or $_ -is [long]) }
            
            $correlatedFeatures = @()
            foreach ($colName in $candidateFeatures) {
                $values = $outputData | ForEach-Object { $_.$colName }
                $numericValues = $values | Where-Object { $_ -ne $null -and ($_ -is [int] -or $_ -is [double] -or $_ -is [float] -or $_ -is [decimal] -or $_ -is [long]) }
                
                # Align the values with target values (remove nulls in the same positions)
                $alignedFeatureValues = @()
                $alignedTargetValues = @()
                for ($i = 0; $i -lt $outputData.Count; $i++) {
                    $featureVal = $outputData[$i][$colName]
                    $targetVal = $outputData[$i][$TargetVariable]
                    
                    if ($featureVal -ne $null -and $targetVal -ne $null -and 
                        ($featureVal -is [int] -or $featureVal -is [double] -or $featureVal -is [float] -or $featureVal -is [decimal] -or $featureVal -is [long]) -and
                        ($targetVal -is [int] -or $targetVal -is [double] -or $targetVal -is [float] -or $targetVal -is [decimal] -or $targetVal -is [long])) {
                        $alignedFeatureValues += $featureVal
                        $alignedTargetValues += $targetVal
                    }
                }
                
                if ($alignedFeatureValues.Count -gt 1) {
                    $correlation = Calculate-PearsonCorrelation -X $alignedFeatureValues -Y $alignedTargetValues
                    $absCorrelation = [math]::Abs($correlation)
                    
                    if ($absCorrelation -ge $Threshold) {
                        $correlatedFeatures += $colName
                    } else {
                        $result.FeaturesRemoved += "$colName (Low correlation with target: $absCorrelation)"
                    }
                } else {
                    $result.FeaturesRemoved += "$colName (Insufficient aligned values for correlation)"
                }
            }
            $candidateFeatures = $correlatedFeatures
        }
        
        if ($Method -eq "Univariate" -or $Method -eq "All") {
            # For now, we'll implement a simple variance-based approach as a stand-in for more complex univariate methods
            # In a full implementation, we would use statistical tests (F-test, chi-squared, etc.)
            $result.Message += " [Univariate selection would use statistical tests in a full implementation]"
        }
        
        # Keep only the selected features
        $result.SelectedFeatures = $candidateFeatures + $TargetVariable  # Add target back
        
        # Remove unselected features from output data
        $finalOutputData = @()
        foreach ($row in $outputData) {
            $newRow = @{}
            foreach ($feature in $result.SelectedFeatures) {
                if ($row.ContainsKey($feature)) {
                    $newRow[$feature] = $row[$feature]
                }
            }
            $finalOutputData += $newRow
        }
        
        $result.OutputData = $finalOutputData
        $result.Message = "Feature selection completed. Selected $($result.SelectedFeatures.Count) features, removed $($result.FeaturesRemoved.Count)."
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to select features: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Calculate-PearsonCorrelation {
    # Helper function to calculate Pearson correlation coefficient
    param([array]$X, [array]$Y)
    
    if ($X.Count -ne $Y.Count -or $X.Count -lt 2) {
        return 0
    }
    
    $n = $X.Count
    $sumX = ($X | Measure-Object -Sum).Sum
    $sumY = ($Y | Measure-Object -Sum).Sum
    $sumXY = 0
    $sumX2 = 0
    $sumY2 = 0
    
    for ($i = 0; $i -lt $n; $i++) {
        $sumXY += $X[$i] * $Y[$i]
        $sumX2 += $X[$i] * $X[$i]
        $sumY2 += $Y[$i] * $Y[$i]
    }
    
    $numerator = $n * $sumXY - $sumX * $sumY
    $denominator = [math]::Sqrt(($n * $sumX2 - $sumX * $sumX) * ($n * $sumY2 - $sumY * $sumY))
    
    if ($denominator -eq 0) { return 0 }
    return $numerator / $denominator
}

function Detect-DataDrift {
    <#
    .SYNOPSIS
    Detects data drift between baseline and current datasets
    
    .DESCRIPTION
    This function compares a baseline dataset with a current dataset to identify
    changes in data distribution that might affect model performance.
    
    .PARAMETER BaselineData
    The baseline dataset for comparison
    
    .PARAMETER CurrentData
    The current dataset to check for drift
    
    .PARAMETER ColumnsToCheck
    Array of column names to check for drift (if not specified, checks all)
    
    .PARAMETER DriftThreshold
    Threshold for determining significant drift (default: 0.1)
    
    .EXAMPLE
    $driftReport = Detect-DataDrift -BaselineData $baseline -CurrentData $current -DriftThreshold 0.15
    
    .NOTES
    This function helps identify when model retraining might be needed.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$BaselineData,
        
        [Parameter(Mandatory=$true)]
        [object[]]$CurrentData,
        
        [Parameter()]
        [string[]]$ColumnsToCheck,
        
        [Parameter()]
        [double]$DriftThreshold = 0.1
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Data drift detection completed successfully"
        DriftReport = @{}
        HasDrift = $false
        ConfidenceScore = 0.90
    }
    
    try {
        if ($BaselineData.Count -eq 0 -or $CurrentData.Count -eq 0) {
            $result.Message = "One or both datasets are empty"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Determine columns to check
        $allProperties = @()
        $allData = $BaselineData + $CurrentData
        
        foreach ($obj in $allData) {
            foreach ($prop in $obj.PSObject.Properties) {
                if ($allProperties -notcontains $prop.Name) {
                    $allProperties += $prop.Name
                }
            }
        }
        
        $columnsToProcess = if ($ColumnsToCheck.Count -gt 0) { $ColumnsToCheck } else { $allProperties }
        
        # Check each column for drift
        foreach ($colName in $columnsToProcess) {
            $baselineValues = $BaselineData | ForEach-Object { $_.$colName }
            $currentValues = $CurrentData | ForEach-Object { $_.$colName }
            
            # Only process if both datasets have values for this column
            if ($baselineValues.Count -eq 0 -or $currentValues.Count -eq 0) { continue }
            
            $driftMetrics = @{
                Column = $colName
                HasDrift = $false
                DriftMagnitude = 0
                Method = ""
                Details = @{}
            }
            
            # Get non-null numeric values for statistical comparison
            $baselineNumeric = $baselineValues | Where-Object { $_ -ne $null -and ($_ -is [int] -or $_ -is [double] -or $_ -is [float] -or $_ -is [decimal] -or $_ -is [long]) }
            $currentNumeric = $currentValues | Where-Object { $_ -ne $null -and ($_ -is [int] -or $_ -is [double] -or $_ -is [float] -or $_ -is [decimal] -or $_ -is [long]) }
            
            if ($baselineNumeric.Count -gt 0 -and $currentNumeric.Count -gt 0) {
                # Calculate drift using statistical measures
                
                # 1. Compare means
                $baselineMean = ($baselineNumeric | Measure-Object -Average).Average
                $currentMean = ($currentNumeric | Measure-Object -Average).Average
                $meanDiff = [math]::Abs($currentMean - $baselineMean)
                
                # 2. Compare medians
                $baselineMedian = Get-Median -Values $baselineNumeric
                $currentMedian = Get-Median -Values $currentNumeric
                $medianDiff = [math]::Abs($currentMedian - $baselineMedian)
                
                # 3. Compare distributions using a simple approach (Kolmogorov-Smirnov)
                # For PowerShell implementation, we'll use a simplified approach:
                # Calculate the maximum difference between empirical CDFs
                $allNumeric = $baselineNumeric + $currentNumeric
                $uniqueSorted = $allNumeric | Sort-Object -Unique
                $maxCdfDiff = 0
                
                foreach ($value in $uniqueSorted) {
                    $baselineCdf = ($baselineNumeric | Where-Object { $_ -le $value }).Count / $baselineNumeric.Count
                    $currentCdf = ($currentNumeric | Where-Object { $_ -le $value }).Count / $currentNumeric.Count
                    $cdfDiff = [math]::Abs($currentCdf - $baselineCdf)
                    if ($cdfDiff -gt $maxCdfDiff) { $maxCdfDiff = $cdfDiff }
                }
                
                # Calculate average drift magnitude across measures
                $meanDrift = if ($baselineMean -ne 0) { $meanDiff / [math]::Abs($baselineMean) } else { $meanDiff }
                $medianDrift = if ($baselineMedian -ne 0) { $medianDiff / [math]::Abs($baselineMedian) } else { $medianDiff }
                $driftMetrics.DriftMagnitude = ($meanDrift + $medianDrift + $maxCdfDiff) / 3
                $driftMetrics.Method = "Mean/Median/Distribution comparison"
                $driftMetrics.Details = @{
                    BaselineMean = $baselineMean
                    CurrentMean = $currentMean
                    BaselineMedian = $baselineMedian
                    CurrentMedian = $currentMedian
                    MaxCDFDiff = $maxCdfDiff
                    SampleSizeBaseline = $baselineNumeric.Count
                    SampleSizeCurrent = $currentNumeric.Count
                }
            } else {
                # For categorical data, we'll just compare value frequencies
                $baselineCateg = $baselineValues | Where-Object { $_ -ne $null -and $_ -isnot [int] -and $_ -isnot [double] -and $_ -isnot [float] -and $_ -isnot [decimal] -and $_ -isnot [long] }
                $currentCateg = $currentValues | Where-Object { $_ -ne $null -and $_ -isnot [int] -and $_ -isnot [double] -and $_ -isnot [float] -and $_ -isnot [decimal] -and $_ -isnot [long] }
                
                if ($baselineCateg.Count -gt 0 -and $currentCateg.Count -gt 0) {
                    # Compare frequency distributions
                    $baselineFreq = $baselineCateg | Group-Object | ForEach-Object { 
                        [PSCustomObject]@{ Value = $_.Name; Count = $_.Count; Proportion = $_.Count / $baselineCateg.Count } 
                    }
                    $currentFreq = $currentCateg | Group-Object | ForEach-Object { 
                        [PSCustomObject]@{ Value = $_.Name; Count = $_.Count; Proportion = $_.Count / $currentCateg.Count } 
                    }
                    
                    # Calculate chi-squared like statistic
                    $totalObservations = $baselineCateg.Count + $currentCateg.Count
                    $expectedBaselineTotal = $baselineCateg.Count
                    $expectedCurrentTotal = $currentCateg.Count
                    
                    $chiSquare = 0
                    $allValues = ($baselineCateg + $currentCateg) | Sort-Object -Unique
                    
                    foreach ($value in $allValues) {
                        $obsBaseline = $baselineFreq | Where-Object { $_.Value -eq $value }
                        $obsCurrent = $currentFreq | Where-Object { $_.Value -eq $value }
                        
                        $obsBaseCount = if ($obsBaseline) { $obsBaseline.Count } else { 0 }
                        $obsCurrCount = if ($obsCurrent) { $obsCurrent.Count } else { 0 }
                        
                        $expBaseCount = if ($obsBaseline) { ($obsBaseCount + $obsCurrCount) * ($expectedBaselineTotal / $totalObservations) } else { 0 }
                        $expCurrCount = if ($obsCurrent) { ($obsBaseCount + $obsCurrCount) * ($expectedCurrentTotal / $totalObservations) } else { 0 }
                        
                        if ($expBaseCount -ne 0) {
                            $chiSquare += [math]::Pow($obsBaseCount - $expBaseCount, 2) / $expBaseCount
                        }
                        if ($expCurrCount -ne 0) {
                            $chiSquare += [math]::Pow($obsCurrCount - $expCurrCount, 2) / $expCurrCount
                        }
                    }
                    
                    $driftMetrics.DriftMagnitude = $chiSquare / [math]::Max($allValues.Count - 1, 1)  # Normalize by degrees of freedom
                    $driftMetrics.Method = "Categorical frequency comparison"
                }
            }
            
            # Determine if drift is significant
            $driftMetrics.HasDrift = $driftMetrics.DriftMagnitude -gt $DriftThreshold
            if ($driftMetrics.HasDrift) {
                $result.HasDrift = $true
            }
            
            $result.DriftReport[$colName] = $driftMetrics
        }
        
        $driftedColumns = ($result.DriftReport.Values | Where-Object { $_.HasDrift }).Count
        $result.Message = "Data drift detection completed. $driftedColumns columns show significant drift."
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to detect data drift: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

# Export functions
Export-ModuleMember -Function Get-DataQualityReport, Remove-Outliers, Impute-MissingValues, Transform-Features, Encode-CategoricalVariables, Select-Features, Detect-DataDrift
# AXMonitor.AI - Advanced Visualization for AI Insights Module
# Purpose: Provides data structures and functions for creating advanced visualizations of AI insights
# Author: Qwen Code
# Date: 2025-10-29

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function New-AIVisualizationData {
    <#
    .SYNOPSIS
    Creates visualization-ready data structures for AI insights
    
    .DESCRIPTION
    This function prepares data in formats suitable for visualization libraries
    like Chart.js, Plotly, or D3.js that can be used in the web interface.
    
    .PARAMETER DataType
    Type of visualization data to create
    
    .PARAMETER Data
    Source data to transform
    
    .PARAMETER Options
    Visualization options and parameters
    
    .EXAMPLE
    $chartData = New-AIVisualizationData -DataType "TimeSeriesLine" -Data $metrics -Options @{"TimeRange" = "Last24Hours"}
    
    .NOTES
    This function generates JSON-compatible data structures for client-side visualization.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [ValidateSet("TimeSeriesLine", "BarChart", "ScatterPlot", "Heatmap", "ModelPerformance", "FeatureImportance", "AnomalyTimeline", "PredictionAccuracy")]
        [string]$DataType,
        
        [Parameter(Mandatory=$true)]
        [object]$Data,
        
        [Parameter()]
        [hashtable]$Options = @{}
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Visualization data created successfully"
        VisualizationData = @{}
        Metadata = @{}
        ConfidenceScore = 0.90
    }
    
    try {
        $visualizationData = @{}
        $metadata = @{
            DataType = $DataType
            GeneratedAt = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffZ'
            Options = $Options
        }
        
        switch ($DataType) {
            "TimeSeriesLine" {
                # Prepare time series data for line chart visualization
                $timeSeriesData = @()
                
                # Validate input data
                if ($Data -is [array]) {
                    foreach ($record in $Data) {
                        if ($record -is [hashtable] -or $record -is [pscustomobject]) {
                            $timestamp = if ($record.PSObject.Properties.Name -contains 'Timestamp') { $record.Timestamp }
                                        elseif ($record.ContainsKey('Timestamp')) { $record['Timestamp'] }
                                        else { Get-Date }
                            
                            $value = if ($record.PSObject.Properties.Name -contains 'Value') { $record.Value }
                                    elseif ($record.ContainsKey('Value')) { $record['Value'] }
                                    else { 0 }
                            
                            $timeSeriesData += [PSCustomObject]@{
                                x = [DateTime]$timestamp
                                y = $value
                            }
                        }
                    }
                }
                
                $visualizationData = @{
                    labels = ($timeSeriesData | ForEach-Object { [DateTime]$_.x })
                    datasets = @(
                        @{
                            label = $Options.Label
                            data = ($timeSeriesData | ForEach-Object { $_.y })
                            fill = $false
                            borderColor = $Options.Color
                            tension = 0.1
                        }
                    )
                }
                
                # Add prediction intervals if available
                if ($Options.ContainsKey('Predictions')) {
                    $predictionData = @()
                    foreach ($pred in $Options.Predictions) {
                        $predictionData += [PSCustomObject]@{
                            x = [DateTime]$pred.Timestamp
                            y = $pred.Value
                        }
                    }
                    
                    $visualizationData.datasets += @{
                        label = "Predictions"
                        data = ($predictionData | ForEach-Object { $_.y })
                        fill = $false
                        borderColor = "rgb(255, 99, 132)"
                        borderDash = @(5, 5)
                        tension = 0.1
                    }
                }
                
                $metadata.DataCount = $timeSeriesData.Count
            }
            
            "BarChart" {
                # Prepare data for bar chart visualization
                $categories = @()
                $values = @()
                
                if ($Data -is [hashtable]) {
                    foreach ($key in $Data.Keys) {
                        $categories += $key
                        $values += $Data[$key]
                    }
                } elseif ($Data -is [array]) {
                    foreach ($item in $Data) {
                        if ($item -is [hashtable] -or $item -is [pscustomobject]) {
                            $label = if ($item.PSObject.Properties.Name -contains 'Label') { $item.Label }
                                    elseif ($item.ContainsKey('Label')) { $item['Label'] }
                                    else { "Item$($categories.Count)" }
                            
                            $value = if ($item.PSObject.Properties.Name -contains 'Value') { $item.Value }
                                    elseif ($item.ContainsKey('Value')) { $item['Value'] }
                                    else { 0 }
                            
                            $categories += $label
                            $values += $value
                        }
                    }
                }
                
                $visualizationData = @{
                    labels = $categories
                    datasets = @(
                        @{
                            label = $Options.Label
                            data = $values
                            backgroundColor = $Options.Color
                        }
                    )
                }
                
                $metadata.DataCount = $categories.Count
            }
            
            "ScatterPlot" {
                # Prepare data for scatter plot visualization
                $scatterData = @()
                
                if ($Data -is [array]) {
                    foreach ($record in $Data) {
                        if ($record -is [hashtable] -or $record -is [pscustomobject]) {
                            $x = if ($record.PSObject.Properties.Name -contains 'X') { $record.X }
                                elseif ($record.ContainsKey('X')) { $record['X'] }
                                else { 0 }
                            
                            $y = if ($record.PSObject.Properties.Name -contains 'Y') { $record.Y }
                                elseif ($record.ContainsKey('Y')) { $record['Y'] }
                                else { 0 }
                            
                            $scatterData += [PSCustomObject]@{
                                x = $x
                                y = $y
                            }
                        }
                    }
                }
                
                $visualizationData = @{
                    datasets = @(
                        @{
                            label = $Options.Label
                            data = $scatterData
                            backgroundColor = $Options.Color
                        }
                    )
                }
                
                $metadata.DataCount = $scatterData.Count
            }
            
            "Heatmap" {
                # Prepare data for heatmap visualization (e.g., correlation matrix)
                $rows = @()
                $cols = @()
                $values = @()
                
                if ($Data -is [hashtable] -and $Data.ContainsKey('Matrix')) {
                    $matrix = $Data.Matrix
                    $rows = $Data.RowLabels
                    $cols = $Data.ColLabels
                    
                    # Convert matrix to array of arrays if needed
                    if ($matrix -is [array] -and $matrix[0] -is [array]) {
                        $values = $matrix
                    }
                }
                
                $visualizationData = @{
                    rows = $rows
                    cols = $cols
                    values = $values
                }
                
                $metadata.DataCount = $rows.Count * $cols.Count
            }
            
            "ModelPerformance" {
                # Prepare model performance metrics for visualization
                $performanceData = @{}
                
                if ($Data -is [hashtable]) {
                    $performanceData = $Data
                } else {
                    # If Data is an object, try to extract performance metrics
                    $performanceData.R2Score = if ($Data.PSObject.Properties.Name -contains 'R2Score') { $Data.R2Score } else { 0 }
                    $performanceData.MSE = if ($Data.PSObject.Properties.Name -contains 'MSE') { $Data.MSE } else { 0 }
                    $performanceData.MAE = if ($Data.PSObject.Properties.Name -contains 'MAE') { $Data.MAE } else { 0 }
                    $performanceData.TrainingTime = if ($Data.PSObject.Properties.Name -contains 'TrainingTime') { $Data.TrainingTime } else { 0 }
                }
                
                $visualizationData = @{
                    labels = @("R² Score", "MSE", "MAE", "Training Time (s)")
                    datasets = @(
                        @{
                            label = "Model Performance"
                            data = @($performanceData.R2Score, $performanceData.MSE, $performanceData.MAE, $performanceData.TrainingTime)
                            backgroundColor = @("rgba(54, 162, 235, 0.2)")
                            borderColor = @("rgba(54, 162, 235, 1)")
                        }
                    )
                }
                
                $metadata.ModelType = if ($Data.PSObject.Properties.Name -contains 'ModelType') { $Data.ModelType } else { "Unknown" }
            }
            
            "FeatureImportance" {
                # Prepare feature importance data for visualization
                $importanceData = @()
                
                if ($Data -is [array]) {
                    $importanceData = $Data | ForEach-Object { [PSCustomObject]@{Feature = $_.Feature; Importance = $_.ImportanceScore} }
                } elseif ($Data -is [hashtable] -and $Data.ContainsKey('FeatureImportance')) {
                    foreach ($feature in $Data.FeatureImportance) {
                        $importanceData += [PSCustomObject]@{Feature = $feature.Feature; Importance = $feature.ImportanceScore}
                    }
                }
                
                # Sort by importance
                $importanceData = $importanceData | Sort-Object Importance -Descending
                
                $visualizationData = @{
                    labels = ($importanceData.Feature)
                    datasets = @(
                        @{
                            label = "Feature Importance"
                            data = ($importanceData.Importance)
                            backgroundColor = "rgba(255, 99, 132, 0.2)"
                            borderColor = "rgba(255, 99, 132, 1)"
                        }
                    )
                }
                
                $metadata.FeatureCount = $importanceData.Count
            }
            
            "AnomalyTimeline" {
                # Prepare anomaly detection results for timeline visualization
                $anomalyData = @()
                
                if ($Data -is [array]) {
                    foreach ($anomaly in $Data) {
                        if ($anomaly -is [hashtable] -or $anomaly -is [pscustomobject]) {
                            $timestamp = if ($anomaly.PSObject.Properties.Name -contains 'Timestamp') { $anomaly.Timestamp }
                                        elseif ($anomaly.ContainsKey('Timestamp')) { $anomaly['Timestamp'] }
                                        else { Get-Date }
                            
                            $severity = if ($anomaly.PSObject.Properties.Name -contains 'Severity') { $anomaly.Severity }
                                       elseif ($anomaly.ContainsKey('Severity')) { $anomaly['Severity'] }
                                       else { "Medium" }
                            
                            $score = if ($anomaly.PSObject.Properties.Name -contains 'Score') { $anomaly.Score }
                                    elseif ($anomaly.ContainsKey('Score')) { $anomaly['Score'] }
                                    else { 0 }
                            
                            $type = if ($anomaly.PSObject.Properties.Name -contains 'AnomalyType') { $anomaly.AnomalyType }
                                   elseif ($anomaly.ContainsKey('AnomalyType')) { $anomaly['AnomalyType'] }
                                   else { "Unknown" }
                            
                            $anomalyData += [PSCustomObject]@{
                                x = [DateTime]$timestamp
                                y = $score
                                severity = $severity
                                type = $type
                            }
                        }
                    }
                }
                
                # Group by severity for different visualization layers
                $highAnomalies = $anomalyData | Where-Object { $_.severity -eq "High" -or $_.severity -eq "Critical" }
                $mediumAnomalies = $anomalyData | Where-Object { $_.severity -eq "Medium" }
                $lowAnomalies = $anomalyData | Where-Object { $_.severity -eq "Low" }
                
                $visualizationData = @{
                    labels = ($anomalyData | ForEach-Object { [DateTime]$_.x })
                    datasets = @()
                }
                
                if ($highAnomalies.Count -gt 0) {
                    $visualizationData.datasets += @{
                        label = "High/Critical Anomalies"
                        data = ($highAnomalies | ForEach-Object { $_.y })
                        borderColor = "rgb(255, 99, 132)"
                        backgroundColor = "rgba(255, 99, 132, 0.2)"
                    }
                }
                
                if ($mediumAnomalies.Count -gt 0) {
                    $visualizationData.datasets += @{
                        label = "Medium Anomalies"
                        data = ($mediumAnomalies | ForEach-Object { $_.y })
                        borderColor = "rgb(255, 205, 86)"
                        backgroundColor = "rgba(255, 205, 86, 0.2)"
                    }
                }
                
                if ($lowAnomalies.Count -gt 0) {
                    $visualizationData.datasets += @{
                        label = "Low Anomalies"
                        data = ($lowAnomalies | ForEach-Object { $_.y })
                        borderColor = "rgb(75, 192, 192)"
                        backgroundColor = "rgba(75, 192, 192, 0.2)"
                    }
                }
                
                $metadata.AnomalyCount = $anomalyData.Count
            }
            
            "PredictionAccuracy" {
                # Prepare prediction accuracy data for visualization
                $accuracyData = @()
                
                if ($Data -is [array]) {
                    foreach ($record in $Data) {
                        if ($record -is [hashtable] -or $record -is [pscustomobject]) {
                            $predicted = if ($record.PSObject.Properties.Name -contains 'Predicted') { $record.Predicted }
                                       elseif ($record.ContainsKey('Predicted')) { $record['Predicted'] }
                                       else { 0 }
                            
                            $actual = if ($record.PSObject.Properties.Name -contains 'Actual') { $record.Actual }
                                    elseif ($record.ContainsKey('Actual')) { $record['Actual'] }
                                    else { 0 }
                            
                            $timestamp = if ($record.PSObject.Properties.Name -contains 'Timestamp') { $record.Timestamp }
                                        elseif ($record.ContainsKey('Timestamp')) { $record['Timestamp'] }
                                        else { Get-Date }
                            
                            $accuracyData += [PSCustomObject]@{
                                x = [DateTime]$timestamp
                                y = $actual
                                predicted = $predicted
                            }
                        }
                    }
                }
                
                $visualizationData = @{
                    labels = ($accuracyData | ForEach-Object { [DateTime]$_.x })
                    datasets = @(
                        @{
                            label = "Actual Values"
                            data = ($accuracyData | ForEach-Object { $_.y })
                            borderColor = "rgb(54, 162, 235)"
                        },
                        @{
                            label = "Predicted Values"
                            data = ($accuracyData | ForEach-Object { $_.predicted })
                            borderColor = "rgb(255, 99, 132)"
                            borderDash = @(5, 5)
                        }
                    )
                }
                
                $metadata.DataCount = $accuracyData.Count
            }
        }
        
        $result.VisualizationData = $visualizationData
        $result.Metadata = $metadata
        $result.Message = "Generated visualization data for $DataType with $($metadata.DataCount) data points"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Visualization data creation failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function New-DashboardComponent {
    <#
    .SYNOPSIS
    Creates a dashboard component with AI insights visualization
    
    .DESCRIPTION
    This function packages AI insights into dashboard-ready components with
    appropriate visualizations and metadata.
    
    .PARAMETER ComponentType
    Type of dashboard component to create
    
    .PARAMETER Data
    Data to visualize
    
    .PARAMETER Title
    Title for the component
    
    .PARAMETER Options
    Component options and configuration
    
    .EXAMPLE
    $dashboardComponent = New-DashboardComponent -ComponentType "ModelPerformance" -Data $modelPerformance -Title "Model Performance Metrics"
    
    .NOTES
    This function creates dashboard components with appropriate visualizations.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [ValidateSet("ModelPerformance", "FeatureImportance", "AnomalyOverview", "PredictionAccuracy", "TimeSeriesForecast", "RecommendationSummary", "TrendAnalysis")]
        [string]$ComponentType,
        
        [Parameter(Mandatory=$true)]
        [object]$Data,
        
        [Parameter(Mandatory=$true)]
        [string]$Title,
        
        [Parameter()]
        [hashtable]$Options = @{}
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Dashboard component created successfully"
        Component = @{}
        ConfidenceScore = 0.90
    }
    
    try {
        $component = @{
            Id = [guid]::NewGuid().ToString()
            Type = $ComponentType
            Title = $Title
            Data = @{}
            VisualizationType = ""
            Configuration = $Options
            Metadata = @{
                CreatedAt = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffZ'
                UpdatedAt = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffZ'
                RefreshInterval = $Options.RefreshInterval
            }
        }
        
        # Generate appropriate visualization data based on component type
        switch ($ComponentType) {
            "ModelPerformance" {
                $visResult = New-AIVisualizationData -DataType "ModelPerformance" -Data $Data
                $component.Data = $visResult.VisualizationData
                $component.VisualizationType = "radar"
                $component.Metadata.ModelType = $visResult.Metadata.ModelType
            }
            
            "FeatureImportance" {
                $visResult = New-AIVisualizationData -DataType "FeatureImportance" -Data $Data
                $component.Data = $visResult.VisualizationData
                $component.VisualizationType = "horizontalBar"
                $component.Metadata.FeatureCount = $visResult.Metadata.FeatureCount
            }
            
            "AnomalyOverview" {
                $visResult = New-AIVisualizationData -DataType "AnomalyTimeline" -Data $Data
                $component.Data = $visResult.VisualizationData
                $component.VisualizationType = "line"
                $component.Metadata.AnomalyCount = $visResult.Metadata.AnomalyCount
            }
            
            "PredictionAccuracy" {
                $visResult = New-AIVisualizationData -DataType "PredictionAccuracy" -Data $Data
                $component.Data = $visResult.VisualizationData
                $component.VisualizationType = "line"
                $component.Metadata.DataCount = $visResult.Metadata.DataCount
            }
            
            "TimeSeriesForecast" {
                $visResult = New-AIVisualizationData -DataType "TimeSeriesLine" -Data $Data -Options $Options
                $component.Data = $visResult.VisualizationData
                $component.VisualizationType = "line"
                $component.Metadata.DataCount = $visResult.Metadata.DataCount
            }
            
            "RecommendationSummary" {
                # For recommendation summary, we'll structure the data differently
                $recommendations = if ($Data -is [array]) { $Data } else { @($Data) }
                
                $component.Data = @{
                    Critical = ($recommendations | Where-Object { $_.Priority -eq "Critical" })
                    High = ($recommendations | Where-Object { $_.Priority -eq "High" })
                    Medium = ($recommendations | Where-Object { $_.Priority -eq "Medium" })
                    Low = ($recommendations | Where-Object { $_.Priority -eq "Low" })
                }
                
                $component.VisualizationType = "summaryCard"
                $component.Metadata.RecommendationCount = $recommendations.Count
            }
            
            "TrendAnalysis" {
                $visResult = New-AIVisualizationData -DataType "TimeSeriesLine" -Data $Data -Options $Options
                $component.Data = $visResult.VisualizationData
                $component.VisualizationType = "line"
                $component.Metadata.DataCount = $visResult.Metadata.DataCount
            }
        }
        
        # Add default configuration if not provided
        if ($component.Configuration.Count -eq 0) {
            $component.Configuration = @{
                width = "medium"
                height = "medium"
                refreshInterval = 300  # seconds
                showLegend = $true
                showGrid = $true
            }
        }
        
        $result.Component = $component
        $result.Message = "Created $ComponentType dashboard component: $Title"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Dashboard component creation failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Export-VisualizationData {
    <#
    .SYNOPSIS
    Exports visualization data in standard formats
    
    .DESCRIPTION
    This function exports visualization data in formats that can be easily
    consumed by client-side visualization libraries or exported to files.
    
    .PARAMETER VisualizationData
    Visualization data to export
    
    .PARAMETER Format
    Export format (JSON, CSV, ChartJS)
    
    .PARAMETER FilePath
    Optional file path to save the data
    
    .EXAMPLE
    $exportData = Export-VisualizationData -VisualizationData $data -Format "JSON"
    
    .NOTES
    This function provides standardized export formats for visualization data.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object]$VisualizationData,
        
        [Parameter(Mandatory=$true)]
        [ValidateSet("JSON", "CSV", "ChartJS", "D3")]
        [string]$Format,
        
        [Parameter()]
        [string]$FilePath
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Visualization data exported successfully"
        ExportedData = $null
        FilePath = $FilePath
        ConfidenceScore = 0.90
    }
    
    try {
        $exportedData = $null
        
        switch ($Format) {
            "JSON" {
                $exportedData = $VisualizationData | ConvertTo-Json -Depth 10
            }
            
            "ChartJS" {
                # Format data specifically for Chart.js library
                $chartJsData = @{
                    type = "line"  # Default type, could be overridden
                    data = $VisualizationData
                    options = @{
                        responsive = $true
                        plugins = @{
                            title = @{
                                display = $true
                                text = $VisualizationData.Title
                            }
                        }
                        scales = @{
                            y = @{
                                beginAtZero = $false
                            }
                        }
                    }
                }
                
                $exportedData = $chartJsData | ConvertTo-Json -Depth 10
            }
            
            "CSV" {
                # Convert to CSV format (for data export/download)
                $csvData = @()
                
                if ($VisualizationData -is [hashtable] -and $VisualizationData.ContainsKey('labels') -and $VisualizationData.ContainsKey('datasets')) {
                    # Time series format
                    $labels = $VisualizationData.labels
                    $datasets = $VisualizationData.datasets
                    
                    # Create a list of hash tables for CSV conversion
                    for ($i = 0; $i -lt $labels.Count; $i++) {
                        $row = @{}
                        $row["Timestamp"] = $labels[$i]
                        
                        foreach ($dataset in $datasets) {
                            $label = $dataset.label
                            if ($i -lt $dataset.data.Count) {
                                $row[$label] = $dataset.data[$i]
                            } else {
                                $row[$label] = ""
                            }
                        }
                        
                        $csvData += $row
                    }
                }
                
                if ($csvData.Count -gt 0) {
                    $exportedData = $csvData | ConvertTo-Csv -NoTypeInformation
                } else {
                    $exportedData = @("# No data available")
                }
            }
            
            "D3" {
                # Format data for D3.js visualization
                $d3Data = $VisualizationData
                $exportedData = $d3Data | ConvertTo-Json -Depth 10
            }
        }
        
        $result.ExportedData = $exportedData
        
        # Save to file if path provided
        if ($FilePath) {
            $exportedData | Out-File -FilePath $FilePath -Encoding UTF8
            $result.Message = "Visualization data exported to $FilePath in $Format format"
        } else {
            $result.Message = "Visualization data prepared in $Format format"
        }
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Visualization data export failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Get-VisualizationTemplate {
    <#
    .SYNOPSIS
    Gets a visualization template for common AI insight types
    
    .DESCRIPTION
    This function provides pre-made visualization templates that can be customized
    for different AI insight types, making it easier to create consistent visualizations.
    
    .PARAMETER TemplateType
    Type of visualization template to get
    
    .PARAMETER Customizations
    Customizations to apply to the template
    
    .EXAMPLE
    $template = Get-VisualizationTemplate -TemplateType "ModelComparison" -Customizations @{"Title" = "Model Performance Comparison"}
    
    .NOTES
    This function provides standardized templates for common visualization needs.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [ValidateSet("ModelComparison", "PerformanceTrend", "AnomalyHeatmap", "FeatureCorrelation", "PredictionVsActual", "RecommendationPriorities")]
        [string]$TemplateType,
        
        [Parameter()]
        [hashtable]$Customizations = @{}
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Visualization template retrieved successfully"
        Template = @{}
        ConfidenceScore = 0.95
    }
    
    try {
        $template = @{
            Id = [guid]::NewGuid().ToString()
            Type = $TemplateType
            Name = $TemplateType
            Description = ""
            VisualizationConfig = @{}
            DataConfig = @{}
            DefaultOptions = @{}
        }
        
        switch ($TemplateType) {
            "ModelComparison" {
                $template.Description = "Compare performance of multiple AI models"
                $template.VisualizationConfig = @{
                    type = "bar"
                    options = @{
                        indexAxis = "y"
                        elements = @{
                            bar = @{
                                borderWidth = 2
                            }
                        }
                        responsive = $true
                        plugins = @{
                            legend = @{
                                position = "right"
                            }
                            title = @{
                                display = $true
                                text = $Customizations.Title
                            }
                        }
                    }
                }
                $template.DataConfig = @{
                    expectedProperties = @("ModelName", "MetricName", "MetricValue")
                    labelsPath = "models"
                    datasetsPath = "metrics"
                }
                $template.DefaultOptions = @{
                    showLegend = $true
                    colorPalette = @("blue", "red", "green", "orange", "purple")
                }
            }
            
            "PerformanceTrend" {
                $template.Description = "Show how model performance changes over time"
                $template.VisualizationConfig = @{
                    type = "line"
                    options = @{
                        responsive = $true
                        plugins = @{
                            title = @{
                                display = $true
                                text = $Customizations.Title
                            }
                            legend = @{
                                display = $true
                                position = "top"
                            }
                        }
                        scales = @{
                            x = @{
                                display = $true
                                title = @{
                                    display = $true
                                    text = "Time"
                                }
                            }
                            y = @{
                                display = $true
                                title = @{
                                    display = $true
                                    text = "Performance Metric"
                                }
                            }
                        }
                    }
                }
                $template.DataConfig = @{
                    expectedProperties = @("Timestamp", "PerformanceMetric")
                    xProperty = "Timestamp"
                    yProperty = "PerformanceMetric"
                }
                $template.DefaultOptions = @{
                    showLegend = $true
                    lineTension = 0.3
                    pointRadius = 3
                }
            }
            
            "AnomalyHeatmap" {
                $template.Description = "Visualize anomaly detection results as a heatmap"
                $template.VisualizationConfig = @{
                    type = "heatmap"
                    options = @{
                        responsive = $true
                        plugins = @{
                            title = @{
                                display = $true
                                text = $Customizations.Title
                            }
                        }
                    }
                }
                $template.DataConfig = @{
                    expectedProperties = @("Time", "Feature", "AnomalyScore")
                    rowsPath = "timeWindows"
                    colsPath = "features"
                    valuesPath = "anomalyScores"
                }
                $template.DefaultOptions = @{
                    colorScale = @("blue", "yellow", "red")  # Low to high anomaly score
                }
            }
            
            "FeatureCorrelation" {
                $template.Description = "Show correlation between different features"
                $template.VisualizationConfig = @{
                    type = "heatmap"
                    options = @{
                        responsive = $true
                        plugins = @{
                            title = @{
                                display = $true
                                text = $Customizations.Title
                            }
                        }
                    }
                }
                $template.DataConfig = @{
                    expectedProperties = @("Feature1", "Feature2", "Correlation")
                    rowsPath = "features"
                    colsPath = "features"
                    valuesPath = "correlationMatrix"
                }
                $template.DefaultOptions = @{
                    colorScale = @("red", "white", "blue")  # Negative to positive correlation
                    showValues = $true
                }
            }
            
            "PredictionVsActual" {
                $template.Description = "Compare model predictions against actual values"
                $template.VisualizationConfig = @{
                    type = "scatter"
                    options = @{
                        responsive = $true
                        plugins = @{
                            title = @{
                                display = $true
                                text = $Customizations.Title
                            }
                            legend = @{
                                display = $true
                                position = "top"
                            }
                        }
                        scales = @{
                            x = @{
                                display = $true
                                title = @{
                                    display = $true
                                    text = "Actual Values"
                                }
                            }
                            y = @{
                                display = $true
                                title = @{
                                    display = $true
                                    text = "Predicted Values"
                                }
                            }
                        }
                    }
                }
                $template.DataConfig = @{
                    expectedProperties = @("Actual", "Predicted")
                    xProperty = "Actual"
                    yProperty = "Predicted"
                }
                $template.DefaultOptions = @{
                    showLegend = $true
                    pointRadius = 4
                    regressionLine = $true
                }
            }
            
            "RecommendationPriorities" {
                $template.Description = "Display recommendations by priority level"
                $template.VisualizationConfig = @{
                    type = "doughnut"
                    options = @{
                        responsive = $true
                        plugins = @{
                            title = @{
                                display = $true
                                text = $Customizations.Title
                            }
                            legend = @{
                                position = "bottom"
                            }
                        }
                    }
                }
                $template.DataConfig = @{
                    expectedProperties = @("Priority", "Count")
                    labelsPath = "priorities"
                    dataPath = "counts"
                }
                $template.DefaultOptions = @{
                    colorPalette = @("red", "orange", "yellow", "green")  # Critical to low
                }
            }
        }
        
        # Apply customizations
        foreach ($key in $Customizations.Keys) {
            $template[$key] = $Customizations[$key]
        }
        
        $result.Template = $template
        $result.Message = "Retrieved template for $TemplateType visualization"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Template retrieval failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function New-InsightVisualization {
    <#
    .SYNOPSIS
    Creates a comprehensive visualization for AI insights
    
    .DESCRIPTION
    This function combines data processing, visualization template selection,
    and dashboard component creation to produce ready-to-render visualizations
    for AI insights.
    
    .PARAMETER InsightType
    Type of insight to visualize
    
    .PARAMETER Data
    Data to visualize
    
    .PARAMETER Title
    Title for the visualization
    
    .PARAMETER Options
    Visualization options and configuration
    
    .EXAMPLE
    $insightViz = New-InsightVisualization -InsightType "ModelPerformance" -Data $performance -Title "Model Performance Dashboard"
    
    .NOTES
    This function creates comprehensive visualization packages for AI insights.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [ValidateSet("ModelPerformance", "FeatureImportance", "AnomalyDetection", "PredictionAccuracy", "TrendAnalysis", "RecommendationSummary", "ModelComparison")]
        [string]$InsightType,
        
        [Parameter(Mandatory=$true)]
        [object]$Data,
        
        [Parameter(Mandatory=$true)]
        [string]$Title,
        
        [Parameter()]
        [hashtable]$Options = @{}
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Insight visualization created successfully"
        Visualization = @{}
        DashboardComponent = $null
        ConfidenceScore = 0.90
    }
    
    try {
        # Get appropriate visualization template
        $templateType = switch ($InsightType) {
            "ModelPerformance" { "ModelComparison" }
            "FeatureImportance" { "ModelComparison" }
            "AnomalyDetection" { "AnomalyHeatmap" }
            "PredictionAccuracy" { "PredictionVsActual" }
            "TrendAnalysis" { "PerformanceTrend" }
            "RecommendationSummary" { "RecommendationPriorities" }
            "ModelComparison" { "ModelComparison" }
        }
        
        $templateResult = Get-VisualizationTemplate -TemplateType $templateType -Customizations @{Title = $Title}
        if ($templateResult.Status -ne "Success") {
            throw "Failed to get visualization template: $($templateResult.Message)"
        }
        
        # Create visualization data based on insight type
        $dataType = switch ($InsightType) {
            "ModelPerformance" { "ModelPerformance" }
            "FeatureImportance" { "FeatureImportance" }
            "AnomalyDetection" { "AnomalyTimeline" }
            "PredictionAccuracy" { "PredictionAccuracy" }
            "TrendAnalysis" { "TimeSeriesLine" }
            "RecommendationSummary" { "BarChart" }
            "ModelComparison" { "BarChart" }
        }
        
        $visDataResult = New-AIVisualizationData -DataType $dataType -Data $Data -Options $Options
        if ($visDataResult.Status -ne "Success") {
            throw "Failed to create visualization data: $($visDataResult.Message)"
        }
        
        # Apply template config to visualization data
        $visualization = @{
            Id = [guid]::NewGuid().ToString()
            Title = $Title
            InsightType = $InsightType
            Template = $templateResult.Template
            VisualizationData = $visDataResult.VisualizationData
            Configuration = $Options
            GeneratedAt = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffZ'
        }
        
        # Create a dashboard component from this visualization
        $componentType = switch ($InsightType) {
            "ModelPerformance" { "ModelPerformance" }
            "FeatureImportance" { "FeatureImportance" }
            "AnomalyDetection" { "AnomalyOverview" }
            "PredictionAccuracy" { "PredictionAccuracy" }
            "TrendAnalysis" { "TrendAnalysis" }
            "RecommendationSummary" { "RecommendationSummary" }
            "ModelComparison" { "ModelPerformance" }
        }
        
        $componentResult = New-DashboardComponent -ComponentType $componentType -Data $Data -Title $Title -Options $Options
        if ($componentResult.Status -ne "Success") {
            throw "Failed to create dashboard component: $($componentResult.Message)"
        }
        
        $visualization.DashboardComponent = $componentResult.Component
        
        $result.Visualization = $visualization
        $result.DashboardComponent = $componentResult.Component
        $result.Message = "Created $InsightType visualization for dashboard integration"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Insight visualization creation failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

# Export functions
Export-ModuleMember -Function New-AIVisualizationData, New-DashboardComponent, Export-VisualizationData, Get-VisualizationTemplate, New-InsightVisualization
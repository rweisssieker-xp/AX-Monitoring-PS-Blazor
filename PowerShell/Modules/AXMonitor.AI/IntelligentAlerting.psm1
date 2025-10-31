# AXMonitor.AI - Intelligent Alert Correlation and Suppression Module
# Purpose: Provides intelligent alert correlation and suppression for AX 2012 R3 monitoring
# Author: Qwen Code
# Date: 2025-10-29

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Correlate-Alerts {
    <#
    .SYNOPSIS
    Correlates related alerts based on similarity and timing
    
    .DESCRIPTION
    This function groups related alerts together based on their characteristics,
    timing, and underlying causes to reduce alert noise and identify root causes.
    
    .PARAMETER Alerts
    Array of alerts to correlate
    
    .PARAMETER CorrelationWindow
    Time window in minutes to consider alerts as potentially related (default: 30)
    
    .PARAMETER CorrelationMetrics
    Metrics to use for determining alert similarity
    
    .PARAMETER RootCauseAnalysis
    Whether to perform basic root cause analysis during correlation
    
    .EXAMPLE
    $correlatedAlerts = Correlate-Alerts -Alerts $alerts -CorrelationWindow 30
    
    .NOTES
    This function helps reduce alert fatigue by grouping related alerts.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$Alerts,
        
        [Parameter()]
        [int]$CorrelationWindow = 30,
        
        [Parameter()]
        [string[]]$CorrelationMetrics = @("AlertType", "Source", "Description"),
        
        [Parameter()]
        [bool]$RootCauseAnalysis = $true
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Alert correlation completed successfully"
        CorrelatedGroups = @()
        UnmatchedAlerts = @()
        Statistics = @{}
        ConfidenceScore = 0.85
    }
    
    try {
        if ($Alerts.Count -eq 0) {
            $result.Message = "No alerts provided for correlation"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Sort alerts by timestamp
        $sortedAlerts = $Alerts | Sort-Object { [datetime]$_.Timestamp }
        
        # Create clusters of related alerts
        $processedIndexes = @{}
        $correlationGroups = @()
        
        for ($i = 0; $i -lt $sortedAlerts.Count; $i++) {
            if ($processedIndexes.ContainsKey($i)) { continue }
            
            $currentAlert = $sortedAlerts[$i]
            $currentTime = [datetime]$currentAlert.Timestamp
            
            # Find potentially related alerts within the correlation window
            $relatedAlerts = @($currentAlert)
            $relatedIndexes = @($i)
            
            for ($j = $i + 1; $j -lt $sortedAlerts.Count; $j++) {
                if ($processedIndexes.ContainsKey($j)) { continue }
                
                $otherAlert = $sortedAlerts[$j]
                $otherTime = [datetime]$otherAlert.Timestamp
                $timeDiff = [math]::Abs(($otherTime - $currentTime).TotalMinutes)
                
                # Check if within correlation window
                if ($timeDiff -gt $CorrelationWindow) { break }  # Since sorted, no need to continue
                
                # Calculate similarity between alerts
                $similarity = Calculate-AlertSimilarity -Alert1 $currentAlert -Alert2 $otherAlert -Metrics $CorrelationMetrics
                
                # If similar enough, add to the cluster
                if ($similarity -gt 0.6) {  # Threshold for similarity
                    $relatedAlerts += $otherAlert
                    $relatedIndexes += $j
                }
            }
            
            # Mark these alerts as processed
            foreach ($idx in $relatedIndexes) {
                $processedIndexes[$idx] = $true
            }
            
            # Create a correlation group if multiple alerts are related
            if ($relatedAlerts.Count -gt 1) {
                $correlationGroup = [PSCustomObject]@{
                    GroupId = [guid]::NewGuid().ToString()
                    PrimaryAlert = $currentAlert
                    RelatedAlerts = $relatedAlerts[1..($relatedAlerts.Count-1)]
                    TotalAlerts = $relatedAlerts.Count
                    TimeRange = @{
                        Start = ($relatedAlerts | Sort-Object { [datetime]$_.Timestamp })[0].Timestamp
                        End = ($relatedAlerts | Sort-Object { [datetime]$_.Timestamp } -Descending)[0].Timestamp
                    }
                    Duration = [math]::Round([datetime]($relatedAlerts | Sort-Object { [datetime]$_.Timestamp } -Descending)[0].Timestamp) - [datetime]($relatedAlerts | Sort-Object { [datetime]$_.Timestamp })[0].Timestamp).TotalMinutes, 2
                    SimilarityScore = ($relatedAlerts | ForEach-Object { 
                        if ($_ -eq $currentAlert) { 1.0 } 
                        else { Calculate-AlertSimilarity -Alert1 $currentAlert -Alert2 $_ -Metrics $CorrelationMetrics } 
                    } | Measure-Object -Average).Average
                    PotentialRootCause = if ($RootCauseAnalysis) { Analyze-PotentialRootCause -Alerts $relatedAlerts } else { $null }
                    Severity = Get-MergedSeverity -Alerts $relatedAlerts
                }
                
                $correlationGroups += $correlationGroup
            } else {
                # If no related alerts, add to unmatched
                $result.UnmatchedAlerts += $currentAlert
            }
        }
        
        $result.CorrelatedGroups = $correlationGroups
        
        # Calculate statistics
        $result.Statistics = @{
            TotalAlerts = $Alerts.Count
            CorrelatedGroups = $correlationGroups.Count
            UnmatchedAlerts = $result.UnmatchedAlerts.Count
            AlertsCorrelated = ($Alerts.Count - $result.UnmatchedAlerts.Count)
            CorrelationRate = if ($Alerts.Count -gt 0) { [math]::Round(($result.Statistics.AlertsCorrelated / $Alerts.Count) * 100, 2) } else { 0 }
        }
        
        $result.Message = "Correlated $($result.Statistics.AlertsCorrelated) of $($result.Statistics.TotalAlerts) alerts into $($result.Statistics.CorrelatedGroups) groups"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Alert correlation failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Calculate-AlertSimilarity {
    <#
    .SYNOPSIS
    Calculates similarity between two alerts based on specified metrics
    
    .DESCRIPTION
    This function computes a similarity score between two alerts based on
    configurable metrics like type, source, description, etc.
    
    .PARAMETER Alert1
    First alert to compare
    
    .PARAMETER Alert2
    Second alert to compare
    
    .PARAMETER Metrics
    Metrics to use for similarity calculation
    
    .EXAMPLE
    $similarity = Calculate-AlertSimilarity -Alert1 $alert1 -Alert2 $alert2 -Metrics @("AlertType", "Source")
    
    .NOTES
    This function uses a weighted approach to calculate alert similarity.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object]$Alert1,
        
        [Parameter(Mandatory=$true)]
        [object]$Alert2,
        
        [Parameter(Mandatory=$true)]
        [string[]]$Metrics
    )
    
    $totalScore = 0
    $metricsConsidered = 0
    
    foreach ($metric in $Metrics) {
        $value1 = $Alert1.$metric
        $value2 = $Alert2.$metric
        
        if ($value1 -eq $null -and $value2 -eq $null) {
            $metricScore = 1.0  # Both null, consider similar
        } elseif ($value1 -eq $null -or $value2 -eq $null) {
            $metricScore = 0.0  # One null, not similar
        } elseif ($value1 -eq $value2) {
            $metricScore = 1.0  # Exact match
        } elseif ($value1 -is [string] -and $value2 -is [string]) {
            # For strings, calculate a basic similarity measure
            $metricScore = Calculate-StringSimilarity -String1 $value1 -String2 $value2
        } else {
            $metricScore = 0.0  # Not equal and not strings
        }
        
        $totalScore += $metricScore
        $metricsConsidered++
    }
    
    # Return average similarity across all metrics
    return if ($metricsConsidered -gt 0) { $totalScore / $metricsConsidered } else { 0 }
}

function Calculate-StringSimilarity {
    <#
    .SYNOPSIS
    Calculates similarity between two strings
    
    .DESCRIPTION
    This function computes a similarity score between two strings using
    a simple longest common substring approach.
    
    .PARAMETER String1
    First string to compare
    
    .PARAMETER String2
    Second string to compare
    
    .EXAMPLE
    $similarity = Calculate-StringSimilarity -String1 "CPU high" -String2 "High CPU usage"
    
    .NOTES
    This is a simplified similarity calculation for demonstration purposes.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$String1,
        
        [Parameter(Mandatory=$true)]
        [string]$String2
    )
    
    # Convert to lowercase and remove extra whitespace
    $s1 = $String1.ToLower() -replace '\s+', ' '
    $s2 = $String2.ToLower() -replace '\s+', ' '
    
    # If exact match after cleaning
    if ($s1 -eq $s2) { return 1.0 }
    
    # Calculate similarity using a simple token-based approach
    $tokens1 = $s1 -split '\s+'
    $tokens2 = $s2 -split '\s+'
    
    # Find common tokens
    $commonTokens = 0
    foreach ($token1 in $tokens1) {
        if ($tokens2 -contains $token1) {
            $commonTokens++
        }
    }
    
    # Jaccard similarity
    $allTokens = ($tokens1 + $tokens2) | Sort-Object -Unique
    $jaccard = if ($allTokens.Count -gt 0) { $commonTokens / $allTokens.Count } else { 0 }
    
    return $jaccard
}

function Analyze-PotentialRootCause {
    <#
    .SYNOPSIS
    Performs basic root cause analysis on a group of related alerts
    
    .DESCRIPTION
    This function attempts to identify a potential root cause for a group
    of related alerts based on common patterns and characteristics.
    
    .PARAMETER Alerts
    Array of related alerts to analyze
    
    .EXAMPLE
    $rootCause = Analyze-PotentialRootCause -Alerts $alertGroup
    
    .NOTES
    This function performs basic pattern analysis for root cause identification.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$Alerts
    )
    
    # Analyze patterns in the alerts
    $rootCause = [PSCustomObject]@{
        PotentialCause = ""
        Confidence = 0.0
        SupportingEvidence = @()
        AffectedComponents = @()
    }
    
    if ($Alerts.Count -eq 0) { return $rootCause }
    
    # Group by alert type to see if there's a dominant type
    $typeGroups = $Alerts | Group-Object -Property AlertType
    $dominantType = $typeGroups | Sort-Object Count -Descending | Select-Object -First 1
    
    # Group by source to see if there's a common source
    $sourceGroups = $Alerts | Group-Object -Property Source
    $dominantSource = $sourceGroups | Sort-Object Count -Descending | Select-Object -First 1
    
    # Check for temporal patterns (are alerts clustered in time?)
    $timestamps = $Alerts | ForEach-Object { [datetime]$_.Timestamp }
    $timeDiff = ($timestamps | Measure-Object -Maximum).Maximum - ($timestamps | Measure-Object -Minimum).Minimum
    $isTemporalCluster = $timeDiff.TotalMinutes -lt 10  # If all alerts within 10 minutes
    
    # Generate potential root cause based on patterns
    if ($dominantType.Count / $Alerts.Count -gt 0.5) {
        $rootCause.PotentialCause = "Systematic issue with $($dominantType.Name) component"
        $rootCause.Confidence = 0.7
        $rootCause.SupportingEvidence += "Majority ($($dominantType.Count) of $($Alerts.Count)) alerts are of type '$($dominantType.Name)'"
    }
    
    if ($dominantSource.Count / $Alerts.Count -gt 0.5) {
        $rootCause.PotentialCause = "Issue with $($dominantSource.Name) component or system"
        $rootCause.Confidence = 0.6
        $rootCause.SupportingEvidence += "Majority ($($dominantSource.Count) of $($Alerts.Count)) alerts originate from '$($dominantSource.Name)'"
    }
    
    if ($isTemporalCluster) {
        $rootCause.PotentialCause = "System-wide event affecting multiple components"
        $rootCause.Confidence = 0.8
        $rootCause.SupportingEvidence += "All alerts occurred within $($timeDiff.TotalMinutes) minutes"
    }
    
    # Combine causes if multiple patterns found
    if ($dominantType.Count / $Alerts.Count -gt 0.5 -and $dominantSource.Count / $Alerts.Count -gt 0.5) {
        $rootCause.PotentialCause = "Issue with $($dominantType.Name) component on $($dominantSource.Name) system"
        $rootCause.Confidence = 0.9
    }
    
    # Identify affected components
    $affectedComponents = $Alerts | ForEach-Object { $_.Component } | Where-Object { $_ -ne $null } | Sort-Object -Unique
    $rootCause.AffectedComponents = $affectedComponents
    
    return $rootCause
}

function Get-MergedSeverity {
    <#
    .SYNOPSIS
    Gets the merged severity from a group of alerts
    
    .DESCRIPTION
    This function determines the appropriate severity level when multiple
    alerts are grouped together.
    
    .PARAMETER Alerts
    Array of alerts to merge severity for
    
    .EXAMPLE
    $severity = Get-MergedSeverity -Alerts $alertGroup
    
    .NOTES
    This function uses a simple algorithm to merge alert severities.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$Alerts
    )
    
    # Define severity hierarchy
    $severityOrder = @{"Low" = 1; "Medium" = 2; "High" = 3; "Critical" = 4}
    
    # Find the highest severity in the group
    $highestSeverity = "Low"
    $highestValue = $severityOrder.Low
    
    foreach ($alert in $Alerts) {
        $severity = $alert.Severity
        if ($severityOrder.ContainsKey($severity) -and $severityOrder[$severity] -gt $highestValue) {
            $highestValue = $severityOrder[$severity]
            $highestSeverity = $severity
        }
    }
    
    return $highestSeverity
}

function Suppress-Alerts {
    <#
    .SYNOPSIS
    Suppresses duplicate or low-value alerts
    
    .DESCRIPTION
    This function identifies and suppresses alerts that are duplicates,
    low-priority, or otherwise not valuable to the user.
    
    .PARAMETER Alerts
    Array of alerts to potentially suppress
    
    .PARAMETER SuppressionRules
    Rules for determining which alerts to suppress
    
    .PARAMETER TimeWindow
    Time window for identifying duplicates (in minutes)
    
    .EXAMPLE
    $suppressedResult = Suppress-Alerts -Alerts $alerts -TimeWindow 60
    
    .NOTES
    This function helps reduce alert fatigue by filtering out low-value alerts.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$Alerts,
        
        [Parameter()]
        [hashtable]$SuppressionRules = @{
            "DuplicateWindow" = 60
            "SuppressLowSeverity" = $false
            "MinIntervalBetweenDuplicates" = 30
            "MaxAlertsPerTimeWindow" = 10
        },
        
        [Parameter()]
        [int]$TimeWindow = 60
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Alert suppression completed successfully"
        SuppressedAlerts = @()
        ActiveAlerts = @()
        SuppressionReasons = @{}
        Statistics = @{}
        ConfidenceScore = 0.85
    }
    
    try {
        if ($Alerts.Count -eq 0) {
            $result.Message = "No alerts provided for suppression"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Sort alerts by timestamp
        $sortedAlerts = $Alerts | Sort-Object { [datetime]$_.Timestamp }
        $activeAlerts = @()
        $suppressedAlerts = @()
        $suppressionReasons = @{}
        
        # Keep track of alerts that have been seen recently to identify duplicates
        $recentAlerts = @{}
        
        foreach ($alert in $sortedAlerts) {
            $shouldSuppress = $false
            $suppressionReason = ""
            
            # Check for duplicate alerts (same type and source within time window)
            $alertSignature = "$($alert.AlertType)|$($alert.Source)"
            $currentTime = [datetime]$alert.Timestamp
            
            if ($recentAlerts.ContainsKey($alertSignature)) {
                $lastAlertTime = [datetime]$recentAlerts[$alertSignature].Timestamp
                $timeSinceLast = ($currentTime - $lastAlertTime).TotalMinutes
                
                if ($timeSinceLast -lt $SuppressionRules.DuplicateWindow) {
                    $shouldSuppress = $true
                    $suppressionReason = "Duplicate of alert from $([math]::Round($timeSinceLast, 2)) minutes ago"
                }
            }
            
            # Update the record of recent alerts
            $recentAlerts[$alertSignature] = $alert
            
            # Check other suppression rules
            if (-not $shouldSuppress) {
                # Add more suppression rules here as needed
            }
            
            if ($shouldSuppress) {
                $suppressedAlerts += $alert
                $suppressionReasons[$alert.Id] = $suppressionReason
            } else {
                $activeAlerts += $alert
            }
        }
        
        $result.SuppressedAlerts = $suppressedAlerts
        $result.ActiveAlerts = $activeAlerts
        $result.SuppressionReasons = $suppressionReasons
        
        # Calculate statistics
        $result.Statistics = @{
            TotalAlerts = $Alerts.Count
            ActiveAlerts = $result.ActiveAlerts.Count
            SuppressedAlerts = $result.SuppressedAlerts.Count
            SuppressionRate = if ($Alerts.Count -gt 0) { [math]::Round(($result.SuppressedAlerts.Count / $Alerts.Count) * 100, 2) } else { 0 }
        }
        
        $result.Message = "Suppressed $($result.Statistics.SuppressedAlerts) of $($result.Statistics.TotalAlerts) alerts ($($result.Statistics.SuppressionRate)% reduction)"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Alert suppression failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Create-IntelligentAlertSummary {
    <#
    .SYNOPSIS
    Creates an intelligent summary of alerts with correlation and suppression applied
    
    .DESCRIPTION
    This function provides a comprehensive view of alerts after applying
    correlation and suppression, highlighting the most important issues.
    
    .PARAMETER Alerts
    Array of alerts to summarize
    
    .PARAMETER CorrelationWindow
    Time window for correlation analysis
    
    .PARAMETER SuppressionEnabled
    Whether to apply suppression rules
    
    .EXAMPLE
    $summary = Create-IntelligentAlertSummary -Alerts $alerts -CorrelationWindow 30 -SuppressionEnabled $true
    
    .NOTES
    This function provides an executive summary of alert activity.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$Alerts,
        
        [Parameter()]
        [int]$CorrelationWindow = 30,
        
        [Parameter()]
        [bool]$SuppressionEnabled = $true
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Intelligent alert summary created successfully"
        Summary = @{}
        KeyIssues = @()
        Timeline = @()
        ConfidenceScore = 0.90
    }
    
    try {
        if ($Alerts.Count -eq 0) {
            $result.Message = "No alerts provided for summary"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Apply correlation
        $correlationResult = Correlate-Alerts -Alerts $Alerts -CorrelationWindow $CorrelationWindow
        
        # Apply suppression if enabled
        $effectiveAlerts = if ($SuppressionEnabled) {
            $suppressionResult = Suppress-Alerts -Alerts $Alerts
            $suppressionResult.ActiveAlerts
        } else {
            $Alerts
        }
        
        # Create timeline of significant events
        $timeline = @()
        foreach ($group in $correlationResult.CorrelatedGroups) {
            $timeline += [PSCustomObject]@{
                Timestamp = $group.TimeRange.Start
                EventType = "Correlated Alert Group"
                Description = "Group of $($group.TotalAlerts) related alerts starting with: $($group.PrimaryAlert.Description)"
                Severity = $group.Severity
                AffectedComponents = $group.PotentialRootCause.AffectedComponents -join ", "
            }
        }
        
        # Add unmatched alerts that are high severity
        foreach ($unmatched in $correlationResult.UnmatchedAlerts) {
            if ($unmatched.Severity -eq "High" -or $unmatched.Severity -eq "Critical") {
                $timeline += [PSCustomObject]@{
                    Timestamp = $unmatched.Timestamp
                    EventType = "High Severity Alert"
                    Description = $unmatched.Description
                    Severity = $unmatched.Severity
                    AffectedComponents = $unmatched.Component
                }
            }
        }
        
        # Sort timeline by timestamp
        $timeline = $timeline | Sort-Object Timestamp
        
        # Identify key issues (high severity correlated groups and unmatched alerts)
        $keyIssues = @()
        
        # Add high severity correlated groups
        foreach ($group in $correlationResult.CorrelatedGroups) {
            if ($group.Severity -eq "High" -or $group.Severity -eq "Critical") {
                $keyIssues += [PSCustomObject]@{
                    IssueType = "Correlated Group"
                    Summary = "Group of $($group.TotalAlerts) $($group.Severity.ToLower()) alerts related to $($group.PotentialRootCause.PotentialCause)"
                    Severity = $group.Severity
                    AffectedComponents = $group.PotentialRootCause.AffectedComponents -join ", "
                    TimeRange = $group.TimeRange
                    Confidence = $group.PotentialRootCause.Confidence
                    PrimaryAlert = $group.PrimaryAlert
                }
            }
        }
        
        # Add unmatched high severity alerts
        foreach ($unmatched in $correlationResult.UnmatchedAlerts) {
            if ($unmatched.Severity -eq "High" -or $unmatched.Severity -eq "Critical") {
                $keyIssues += [PSCustomObject]@{
                    IssueType = "Individual Alert"
                    Summary = "High severity alert: $($unmatched.Description)"
                    Severity = $unmatched.Severity
                    AffectedComponents = $unmatched.Component
                    Timestamp = $unmatched.Timestamp
                    OriginalAlert = $unmatched
                }
            }
        }
        
        # Sort key issues by severity and time
        $keyIssues = $keyIssues | Sort-Object { 
            $severityOrder = @{"Critical" = 4; "High" = 3; "Medium" = 2; "Low" = 1}
            $severityOrder[$_.Severity] 
        } -Descending, Timestamp -Descending
        
        # Create summary statistics
        $summary = [PSCustomObject]@{
            TotalOriginalAlerts = $Alerts.Count
            CorrelatedGroups = $correlationResult.Statistics.CorrelatedGroups
            UnmatchedAlerts = $correlationResult.Statistics.UnmatchedAlerts
            AlertsAfterSuppression = if ($SuppressionEnabled) { 
                $effectiveAlerts.Count 
            } else { 
                $Alerts.Count 
            }
            KeyIssuesIdentified = $keyIssues.Count
            CorrelationRate = $correlationResult.Statistics.CorrelationRate
            TimeRange = @{
                Start = ($Alerts | Sort-Object { [datetime]$_.Timestamp })[0].Timestamp
                End = ($Alerts | Sort-Object { [datetime]$_.Timestamp } -Descending)[0].Timestamp
            }
            SeverityBreakdown = $Alerts | Group-Object -Property Severity | ForEach-Object { "$($_.Name): $($_.Count)" }
        }
        
        $result.Summary = $summary
        $result.KeyIssues = $keyIssues
        $result.Timeline = $timeline
        $result.Message = "Created intelligent summary with $($summary.KeyIssuesIdentified) key issues from $($summary.TotalOriginalAlerts) original alerts"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Intelligent alert summary creation failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Learn-AlertSuppressionRules {
    <#
    .SYNOPSIS
    Learns alert suppression rules based on user feedback
    
    .DESCRIPTION
    This function analyzes user feedback about alerts to learn which alerts
    are typically considered noise and should be suppressed in the future.
    
    .PARAMETER AlertHistory
    Historical alert data with user feedback
    
    .PARAMETER FeedbackAttribute
    Attribute indicating user feedback (e.g., "UserMarkedAsNoise")
    
    .EXAMPLE
    $learnedRules = Learn-AlertSuppressionRules -AlertHistory $history -FeedbackAttribute "UserMarkedAsNoise"
    
    .NOTES
    This function enables the system to improve alert quality over time.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$AlertHistory,
        
        [Parameter(Mandatory=$true)]
        [string]$FeedbackAttribute
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Alert suppression rules learned successfully"
        LearnedRules = @()
        RuleMetrics = @{}
        ConfidenceScore = 0.90
    }
    
    try {
        if ($AlertHistory.Count -eq 0) {
            $result.Message = "No alert history provided for learning"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Separate alerts marked as noise from valid alerts
        $noiseAlerts = $AlertHistory | Where-Object { $_.$FeedbackAttribute -eq $true }
        $validAlerts = $AlertHistory | Where-Object { $_.$FeedbackAttribute -ne $true }
        
        # Analyze patterns in noise alerts that are less common in valid alerts
        $learnedRules = @()
        
        # Rule 1: Alert types that are frequently marked as noise
        $noiseTypeCounts = $noiseAlerts | Group-Object -Property AlertType
        $validTypeCounts = $validAlerts | Group-Object -Property AlertType
        
        foreach ($noiseType in $noiseTypeCounts) {
            $validCount = ($validTypeCounts | Where-Object { $_.Name -eq $noiseType.Name }).Count
            $noiseCount = $noiseType.Count
            
            # Calculate noise ratio
            $totalForType = $noiseCount + $validCount
            $noiseRatio = if ($totalForType -gt 0) { $noiseCount / $totalForType } else { 0 }
            
            # If majority of this type are marked as noise, create a rule
            if ($noiseRatio -gt 0.7 -and $noiseCount -gt 2) {  # At least 70% noise and 3+ examples
                $learnedRules += [PSCustomObject]@{
                    RuleType = "AlertType"
                    Condition = "AlertType -eq '$($noiseType.Name)'"
                    Action = "Suppress"
                    Confidence = $noiseRatio
                    Support = $noiseCount
                    Lift = $noiseRatio / 0.1  # Compare to overall noise rate assumption (10%)
                }
            }
        }
        
        # Rule 2: Sources that frequently generate noise
        $noiseSourceCounts = $noiseAlerts | Group-Object -Property Source
        $validSourceCounts = $validAlerts | Group-Object -Property Source
        
        foreach ($noiseSource in $noiseSourceCounts) {
            $validCount = ($validSourceCounts | Where-Object { $_.Name -eq $noiseSource.Name }).Count
            $noiseCount = $noiseSource.Count
            
            $totalForSource = $noiseCount + $validCount
            $noiseRatio = if ($totalForSource -gt 0) { $noiseCount / $totalForSource } else { 0 }
            
            if ($noiseRatio -gt 0.7 -and $noiseCount -gt 2) {
                $learnedRules += [PSCustomObject]@{
                    RuleType = "Source"
                    Condition = "Source -eq '$($noiseSource.Name)'"
                    Action = "Suppress"
                    Confidence = $noiseRatio
                    Support = $noiseCount
                    Lift = $noiseRatio / 0.1
                }
            }
        }
        
        # Rule 3: Description patterns that are often noise
        # This would require more advanced NLP, so we'll use a simple keyword approach
        $noiseDescriptions = $noiseAlerts | ForEach-Object { $_.Description }
        $validDescriptions = $validAlerts | ForEach-Object { $_.Description }
        
        # Find common words in noise descriptions that are rare in valid descriptions
        $noiseWords = @{}
        $validWords = @{}
        
        foreach ($desc in $noiseDescriptions) {
            $words = $desc -split '\s+' | ForEach-Object { $_.ToLower().Trim('.,;:!?()[]{}') }
            foreach ($word in $words) {
                if ($word.Length -gt 3) {  # Only consider words with 4+ characters
                    if ($noiseWords.ContainsKey($word)) {
                        $noiseWords[$word]++
                    } else {
                        $noiseWords[$word] = 1
                    }
                }
            }
        }
        
        foreach ($desc in $validDescriptions) {
            $words = $desc -split '\s+' | ForEach-Object { $_.ToLower().Trim('.,;:!?()[]{}') }
            foreach ($word in $words) {
                if ($word.Length -gt 3) {
                    if ($validWords.ContainsKey($word)) {
                        $validWords[$word]++
                    } else {
                        $validWords[$word] = 1
                    }
                }
            }
        }
        
        # Identify words that appear frequently in noise but rarely in valid alerts
        foreach ($word in $noiseWords.Keys) {
            $noiseFreq = $noiseWords[$word]
            $validFreq = if ($validWords.ContainsKey($word)) { $validWords[$word] } else { 0 }
            
            $totalNoise = ($noiseDescriptions | Measure-Object).Count
            $totalValid = ($validDescriptions | Measure-Object).Count
            
            $noiseRatio = $noiseFreq / $totalNoise
            $validRatio = $validFreq / $totalValid
            
            # If word appears much more in noise than in valid alerts
            if ($noiseFreq -gt 1 -and $noiseRatio -gt (3 * $validRatio) -and $noiseRatio -gt 0.05) {
                $learnedRules += [PSCustomObject]@{
                    RuleType = "DescriptionKeyword"
                    Condition = "Description -like '*$word*'"
                    Action = "Suppress"
                    Confidence = $noiseRatio
                    Support = $noiseFreq
                    Lift = $noiseRatio / $validRatio
                }
            }
        }
        
        $result.LearnedRules = $learnedRules | Sort-Object Confidence -Descending
        $result.RuleMetrics = @{
            TotalRulesLearned = $learnedRules.Count
            HighConfidenceRules = ($learnedRules | Where-Object { $_.Confidence -gt 0.8 }).Count
            TotalNoiseAlerts = $noiseAlerts.Count
            TotalValidAlerts = $validAlerts.Count
        }
        
        $result.Message = "Learned $($result.RuleMetrics.TotalRulesLearned) suppression rules from analysis of $($result.RuleMetrics.TotalNoiseAlerts) noise alerts and $($result.RuleMetrics.TotalValidAlerts) valid alerts"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Learning alert suppression rules failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

# Export functions
Export-ModuleMember -Function Correlate-Alerts, Suppress-Alerts, Create-IntelligentAlertSummary, Learn-AlertSuppressionRules
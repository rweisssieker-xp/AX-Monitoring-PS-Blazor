# AXMonitor.AI - AX-Specific NLP Capabilities Module
# Purpose: Provides natural language processing specifically tailored for AX 2012 R3 monitoring queries
# Author: Qwen Code
# Date: 2025-10-29

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Parse-AXNaturalLanguageQuery {
    <#
    .SYNOPSIS
    Parses natural language queries about AX 2012 R3 performance
    
    .DESCRIPTION
    This function interprets natural language queries and extracts intent, entities,
    and other relevant information for processing AX-specific monitoring requests.
    
    .PARAMETER Query
    The natural language query to parse
    
    .PARAMETER Context
    Context about the current state or previous interactions
    
    .EXAMPLE
    $parsed = Parse-AXNaturalLanguageQuery -Query "Show me CPU usage for AOS1 over the last 2 hours"
    
    .NOTES
    This function enables natural language interaction with the monitoring system.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$Query,
        
        [Parameter()]
        [hashtable]$Context = @{}
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Query parsed successfully"
        Intent = ""
        Entities = @{}
        ExtractedParameters = @{}
        ProcessedQuery = $Query
        ConfidenceScore = 0.70
    }
    
    try {
        # Convert to lowercase for easier processing
        $lowerQuery = $Query.ToLower()
        
        # Define AX-specific terminology and patterns
        $axMetrics = @{
            "cpu usage" = @("cpu", "processor", "cpu_usage", "processor_usage")
            "memory usage" = @("memory", "ram", "memory_usage")
            "batch jobs" = @("batch", "batch_jobs", "jobs")
            "sessions" = @("sessions", "active_sessions", "user_sessions")
            "blocking" = @("blocking", "blocked", "blocks", "deadlock")
            "database performance" = @("db performance", "database", "sql", "query_time", "io")
        }
        
        $timeUnits = @{
            "minutes" = @("minute", "minutes", "min", "mins")
            "hours" = @("hour", "hours", "hr", "hrs")
            "days" = @("day", "days")
        }
        
        $axComponents = @{
            "AOS" = @("aos", "application server", "server")
            "Database" = @("database", "db", "sql", "ax database")
            "Batch" = @("batch", "batch server", "batch job processor")
            "Report" = @("report", "report server")
        }
        
        $timeRefs = @{
            "last" = @("last", "past", "previous")
            "current" = @("current", "now", "current time", "present")
            "next" = @("next", "upcoming", "future")
        }
        
        # Identify intent based on keywords
        $intent = ""
        if ($lowerQuery -match "show|display|view|tell me|what is|give me|get|fetch") {
            $intent = "GetInformation"
        } elseif ($lowerQuery -match "alert|warn|notify|warning") {
            $intent = "SetAlert"
        } elseif ($lowerQuery -match "why|reason|caused by|what caused") {
            $intent = "RootCause"
        } elseif ($lowerQuery -match "trend|trending|changing|increasing|decreasing") {
            $intent = "GetTrend"
        } elseif ($lowerQuery -match "compare|difference|vs") {
            $intent = "Compare"
        } elseif ($lowerQuery -match "predict|forecast|estimate") {
            $intent = "Predict"
        } else {
            $intent = "GetInformation"  # Default intent
        }
        
        $result.Intent = $intent
        
        # Identify entities in the query
        $entities = @{}
        
        # Find metric entities
        foreach ($metric in $axMetrics.Keys) {
            $metricVariants = $axMetrics[$metric]
            foreach ($variant in $metricVariants) {
                if ($lowerQuery -match $variant) {
                    $entities["Metric"] = $metric
                    break
                }
            }
        }
        
        # Find time-related entities
        $timeValue = 0
        $timeUnit = ""
        
        # Extract time value and unit
        $timeValueMatch = [regex]::Match($lowerQuery, "(\d+)\s*(minute|minutes|min|mins|hour|hours|hr|hrs|day|days)")
        if ($timeValueMatch.Success) {
            $timeValue = [int]$timeValueMatch.Groups[1].Value
            $timeUnitText = $timeValueMatch.Groups[2].Value
            
            foreach ($unit in $timeUnits.Keys) {
                if ($timeUnits[$unit] -contains $timeUnitText) {
                    $timeUnit = $unit
                    break
                }
            }
        } else {
            # Check for relative times like "last hour"
            $relativeMatch = [regex]::Match($lowerQuery, "(last|past|previous)\s*(\d*)\s*(minute|minutes|hour|hours|day|days)")
            if ($relativeMatch.Success) {
                $timeRef = $relativeMatch.Groups[1].Value
                $timeNum = if ($relativeMatch.Groups[2].Value) { [int]$relativeMatch.Groups[2].Value } else { 1 }
                $timeType = $relativeMatch.Groups[3].Value
                
                $timeValue = $timeNum
                foreach ($unit in $timeUnits.Keys) {
                    if ($timeUnits[$unit] -contains $timeType) {
                        $timeUnit = $unit
                        break
                    }
                }
            }
        }
        
        if ($timeValue -gt 0 -and $timeUnit) {
            $entities["TimeValue"] = $timeValue
            $entities["TimeUnit"] = $timeUnit
        }
        
        # Find AX component entities
        foreach ($component in $axComponents.Keys) {
            $compVariants = $axComponents[$component]
            foreach ($variant in $compVariants) {
                if ($lowerQuery -match $variant) {
                    $entities["Component"] = $component
                    break
                }
            }
        }
        
        # Find time references (last, current, next)
        foreach ($timeRef in $timeRefs.Keys) {
            $refVariants = $timeRefs[$timeRef]
            foreach ($variant in $refVariants) {
                if ($lowerQuery -match $variant) {
                    $entities["TimeReference"] = $timeRef
                    break
                }
            }
        }
        
        $result.Entities = $entities
        
        # Extract specific parameters for interpretation
        $parameters = @{}
        
        # Convert time to appropriate units for the system
        if ($entities.ContainsKey("TimeValue") -and $entities.ContainsKey("TimeUnit")) {
            $timeInHours = switch ($entities["TimeUnit"]) {
                "minutes" { $entities["TimeValue"] / 60 }
                "hours" { $entities["TimeValue"] }
                "days" { $entities["TimeValue"] * 24 }
            }
            $parameters["TimePeriodHours"] = $timeInHours
            $parameters["TimePeriodMinutes"] = $timeInHours * 60
        }
        
        # Extract component info
        if ($entities.ContainsKey("Component")) {
            $parameters["ComponentFilter"] = $entities["Component"]
        }
        
        # Extract metric info
        if ($entities.ContainsKey("Metric")) {
            $parameters["MetricFilter"] = $entities["Metric"]
        }
        
        $result.ExtractedParameters = $parameters
        
        # Calculate a basic confidence score based on how many entities were identified
        $entityCount = $entities.Count
        $result.ConfidenceScore = [math]::Min(0.95, 0.5 + ($entityCount * 0.1))
        
        $result.Message = "Parsed query with intent '$intent' and $($entityCount) entities identified"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Query parsing failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Generate-AXInsightResponse {
    <#
    .SYNOPSIS
    Generates natural language responses to AX monitoring queries
    
    .DESCRIPTION
    This function takes monitoring data and generates natural language responses
    that are easily understood by AX administrators and users.
    
    .PARAMETER ParsedQuery
    The parsed query result from Parse-AXNaturalLanguageQuery
    
    .PARAMETER MonitoringData
    The monitoring data relevant to the query
    
    .PARAMETER Context
    Context about the system state
    
    .EXAMPLE
    $response = Generate-AXInsightResponse -ParsedQuery $parsed -MonitoringData $metrics
    
    .NOTES
    This function creates human-readable responses to monitoring queries.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object]$ParsedQuery,
        
        [Parameter(Mandatory=$true)]
        [object]$MonitoringData,
        
        [Parameter()]
        [hashtable]$Context = @{}
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Insight response generated successfully"
        ResponseText = ""
        ResponseType = ""
        RelevantData = @{}
        ConfidenceScore = 0.80
    }
    
    try {
        # Generate response based on intent and data
        $responseText = ""
        
        switch ($ParsedQuery.Intent) {
            "GetInformation" {
                if ($ParsedQuery.Entities.ContainsKey("Metric")) {
                    $metric = $ParsedQuery.Entities["Metric"]
                    $component = if ($ParsedQuery.Entities.ContainsKey("Component")) { $ParsedQuery.Entities["Component"] } else { "system" }
                    
                    # Format response based on metric
                    switch ($metric) {
                        "cpu usage" {
                            $avgCPU = $MonitoringData.CPU_Average
                            $currentCPU = $MonitoringData.CPU_Current
                            
                            $responseText = "The $component CPU usage is currently at $currentCPU% on average. "
                            if ($currentCPU -gt 80) {
                                $responseText += "This is higher than normal, which might indicate performance issues."
                            } elseif ($currentCPU -lt 30) {
                                $responseText += "This is lower than usual, suggesting the system is lightly loaded."
                            } else {
                                $responseText += "This is within normal operating parameters."
                            }
                        }
                        "memory usage" {
                            $avgMemory = $MonitoringData.Memory_Average
                            $currentMemory = $MonitoringData.Memory_Current
                            
                            $responseText = "The $component memory usage is currently at $currentMemory% on average. "
                            if ($currentMemory -gt 85) {
                                $responseText += "This is quite high and might impact performance if it continues to increase."
                            } else {
                                $responseText += "This is within acceptable limits for the $component."
                            }
                        }
                        "batch jobs" {
                            $pendingJobs = $MonitoringData.Batch_Pending
                            $runningJobs = $MonitoringData.Batch_Running
                            
                            $responseText = "There are currently $pendingJobs pending batch jobs and $runningJobs running batch jobs in the system. "
                            if ($pendingJobs -gt 20) {
                                $responseText += "The number of pending jobs is high, which might indicate a backlog."
                            } else {
                                $responseText += "The batch job queue looks normal."
                            }
                        }
                        "sessions" {
                            $activeSessions = $MonitoringData.Sessions_Active
                            $inactiveSessions = $MonitoringData.Sessions_Inactive
                            
                            $responseText = "There are currently $activeSessions active user sessions and $inactiveSessions inactive sessions connected to the system. "
                            if ($activeSessions -gt 50) {
                                $responseText += "The number of active sessions is high, which might impact system performance."
                            } else {
                                $responseText += "The session count is within normal ranges."
                            }
                        }
                    }
                }
            }
            "RootCause" {
                # Provide potential causes for issues
                $responseText = "Based on the current system state, potential causes for performance issues could be: "
                
                # Add specific causes based on monitoring data
                if ($MonitoringData.CPU_Current -gt 85) {
                    $responseText += "High CPU utilization possibly due to heavy processing, inefficient queries, or resource contention. "
                }
                if ($MonitoringData.Memory_Current -gt 90) {
                    $responseText += "High memory usage that might indicate memory leaks or insufficient RAM. "
                }
                if ($MonitoringData.Batch_Pending -gt 30) {
                    $responseText += "A large batch job backlog suggesting scheduling issues or insufficient batch server capacity. "
                }
                
                $responseText += "I recommend checking the system logs for more details."
            }
            "GetTrend" {
                # Provide trend information
                $responseText = "Based on recent data, "
                if ($MonitoringData.CPU_Trend -eq "Increasing") {
                    $responseText += "CPU usage is showing an increasing trend over the past few hours, "
                } elseif ($MonitoringData.CPU_Trend -eq "Decreasing") {
                    $responseText += "CPU usage is showing a decreasing trend over the past few hours, "
                } else {
                    $responseText += "CPU usage has remained relatively stable over the past few hours, "
                }
                
                $responseText += "which suggests the system workload is "
                $responseText += if ($MonitoringData.CPU_Trend -eq "Increasing") { "increasing." }
                                elseif ($MonitoringData.CPU_Trend -eq "Decreasing") { "decreasing." }
                                else { "remaining constant." }
            }
            "Compare" {
                # Provide comparison information
                $responseText = "Comparing current performance to the same time last week, "
                
                $cpuChange = $MonitoringData.CPU_Current - $MonitoringData.CPU_LastWeek
                $responseText += "CPU usage has "
                $responseText += if ($cpuChange -gt 5) { "increased significantly by $([math]::Abs($cpuChange))%" }
                               elseif ($cpuChange -lt -5) { "decreased significantly by $([math]::Abs($cpuChange))%" }
                               elseif ($cpuChange -gt 2) { "increased by $([math]::Abs($cpuChange))%" }
                               elseif ($cpuChange -lt -2) { "decreased by $([math]::Abs($cpuChange))%" }
                               else { "remained relatively stable" }
                $responseText += ". "
                
                $sessionChange = $MonitoringData.Sessions_Current - $MonitoringData.Sessions_LastWeek
                $responseText += "The number of active sessions has "
                $responseText += if ($sessionChange -gt 10) { "increased significantly by $([math]::Abs($sessionChange)) sessions" }
                               elseif ($sessionChange -lt -10) { "decreased significantly by $([math]::Abs($sessionChange)) sessions" }
                               elseif ($sessionChange -gt 5) { "increased by $([math]::Abs($sessionChange)) sessions" }
                               elseif ($sessionChange -lt 5) { "decreased by $([math]::Abs($sessionChange)) sessions" }
                               else { "remained relatively stable" }
                $responseText += "."
            }
        }
        
        $result.ResponseText = $responseText
        $result.ResponseType = $ParsedQuery.Intent
        $result.RelevantData = $MonitoringData
        $result.Message = "Generated natural language response for $($ParsedQuery.Intent) intent"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Insight response generation failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Extract-AXEntities {
    <#
    .SYNOPSIS
    Extracts AX-specific entities from text
    
    .DESCRIPTION
    This function identifies and extracts AX-related entities such as component names,
    metric types, time periods, and other relevant information from text.
    
    .PARAMETER Text
    The text to extract entities from
    
    .EXAMPLE
    $entities = Extract-AXEntities -Text "Check AOS1 CPU usage for last 2 hours"
    
    .NOTES
    This function helps identify specific AX entities in user queries.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$Text
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Entity extraction completed successfully"
        Components = @()
        Metrics = @()
        TimePeriods = @()
        Values = @()
        ConfidenceScore = 0.75
    }
    
    try {
        $lowerText = $Text.ToLower()
        
        # AX Component patterns
        $componentPatterns = @(
            "aos\d*",                    # AOS1, AOS2, etc.
            "aosservers?\s+\d+",         # AOS Servers 1, AOS Server 2, etc.
            "database server",           # Database server
            "batch server",              # Batch server
            "report server",             # Report server
            "ax server"                  # AX server
        )
        
        # AX Metric patterns
        $metricPatterns = @(
            "cpu.*usage",
            "memory.*usage", 
            "disk.*io",
            "response.*time",
            "active.*sessions",
            "batch.*jobs?",
            "error.*rate",
            "transaction.*count"
        )
        
        # Time patterns
        $timePatterns = @(
            "last.*\d+\s*(minutes?|hours?|days?)",
            "past.*\d+\s*(minutes?|hours?|days?)",
            "previous.*\d+\s*(minutes?|hours?|days?)",
            "current",
            "today",
            "yesterday",
            "this.*week",
            "this.*month"
        )
        
        # Extract components
        foreach ($pattern in $componentPatterns) {
            $matches = [regex]::Matches($lowerText, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            foreach ($match in $matches) {
                $result.Components += $match.Value.Trim()
            }
        }
        
        # Extract metrics
        foreach ($pattern in $metricPatterns) {
            $matches = [regex]::Matches($lowerText, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            foreach ($match in $matches) {
                $result.Metrics += $match.Value.Trim()
            }
        }
        
        # Extract time periods
        foreach ($pattern in $timePatterns) {
            $matches = [regex]::Matches($lowerText, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            foreach ($match in $matches) {
                $result.TimePeriods += $match.Value.Trim()
            }
        }
        
        # Extract numeric values
        $valueMatches = [regex]::Matches($lowerText, "\d+\.?\d*")
        foreach ($valueMatch in $valueMatches) {
            $result.Values += [double]$valueMatch.Value
        }
        
        $result.Components = $result.Components | Sort-Object -Unique
        $result.Metrics = $result.Metrics | Sort-Object -Unique
        $result.TimePeriods = $result.TimePeriods | Sort-Object -Unique
        
        $result.Message = "Extracted $($result.Components.Count) components, $($result.Metrics.Count) metrics, $($result.TimePeriods.Count) time periods, and $($result.Values.Count) values"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Entity extraction failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Understand-AXQueryIntent {
    <#
    .SYNOPSIS
    Determines the intent of an AX monitoring query
    
    .DESCRIPTION
    This function classifies the user's intent in their query about AX monitoring,
    helping to route to the appropriate processing function.
    
    .PARAMETER Query
    The query to analyze
    
    .PARAMETER PreviousQueries
    Previous queries in the conversation (for context)
    
    .EXAMPLE
    $intent = Understand-AXQueryIntent -Query "Why is my AOS running slowly?"
    
    .NOTES
    This function uses pattern matching to determine query intent.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$Query,
        
        [Parameter()]
        [string[]]$PreviousQueries = @()
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Intent understanding completed successfully"
        Intent = ""
        IntentConfidence = 0.0
        SubIntent = ""
        ContextualInfo = @{}
        ConfidenceScore = 0.75
    }
    
    try {
        $lowerQuery = $Query.ToLower()
        
        # Define intent patterns with weights for scoring
        $intentPatterns = @{
            "GetInformation" = @(
                @("show|display|view|tell me|what is|give me|get|fetch|list|report", 0.8),
                @("current|now|status|values|metrics|data", 0.6),
                @("how much|how many|what's the", 0.7)
            )
            "RootCause" = @(
                @("why|reason|caused by|what caused|troubleshoot|problem|issue|slow", 0.9),
                @("caused|reason|due to|because of", 0.7)
            )
            "AlertConfiguration" = @(
                @("alert me|warn me|notify me|set alert|configure alert|threshold", 0.8),
                @("when|if|exceeds|above|below|greater than|less than", 0.6)
            )
            "TrendAnalysis" = @(
                @("trend|trending|changing|change|increasing|decreasing|growing|reducing", 0.8),
                @("over time|compared to|versus|vs|last week|past month", 0.7)
            )
            "Predictive" = @(
                @("predict|forecast|estimate|expect|will|going to|next", 0.8),
                @("future|upcoming|next", 0.6)
            )
            "Comparative" = @(
                @("compare|comparing|versus|vs|difference|vs.", 0.8),
                @("compared to|relative to|ratio|percentage change", 0.7)
            )
        }
        
        $intentScores = @{}
        
        # Calculate score for each intent
        foreach ($intent in $intentPatterns.Keys) {
            $totalScore = 0
            $patternCount = 0
            
            foreach ($patternGroup in $intentPatterns[$intent]) {
                $pattern = $patternGroup[0]
                $weight = $patternGroup[1]
                
                if ($lowerQuery -match $pattern) {
                    $totalScore += $weight
                }
                $patternCount++
            }
            
            if ($patternCount -gt 0) {
                $intentScores[$intent] = $totalScore / $patternCount
            } else {
                $intentScores[$intent] = 0
            }
        }
        
        # Determine primary intent
        $primaryIntent = $null
        $maxScore = 0
        
        foreach ($intent in $intentScores.Keys) {
            if ($intentScores[$intent] -gt $maxScore) {
                $maxScore = $intentScores[$intent]
                $primaryIntent = $intent
            }
        }
        
        $result.Intent = $primaryIntent
        $result.IntentConfidence = $maxScore
        $result.ContextualInfo = $intentScores
        
        # Set overall confidence based on the highest score
        $result.ConfidenceScore = [math]::Min(0.95, $maxScore)
        
        if ($primaryIntent -eq $null) {
            $result.Intent = "GetInformation"  # Default intent
            $result.IntentConfidence = 0.5
            $result.ConfidenceScore = 0.5
            $result.Message = "Could not determine specific intent, defaulting to GetInformation"
        } else {
            $result.Message = "Identified intent '$primaryIntent' with confidence $($result.IntentConfidence)"
        }
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Intent understanding failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Translate-AXNaturalLanguageToQuery {
    <#
    .SYNOPSIS
    Translates natural language into structured monitoring queries
    
    .DESCRIPTION
    This function converts natural language requests into structured queries
    that can be executed against the monitoring system.
    
    .PARAMETER NaturalLanguage
    The natural language request
    
    .PARAMETER Context
    Context about the current system or conversation
    
    .EXAMPLE
    $structuredQuery = Translate-AXNaturalLanguageToQuery -NaturalLanguage "Show me CPU usage for AOS1"
    
    .NOTES
    This function bridges natural language and structured system queries.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$NaturalLanguage,
        
        [Parameter()]
        [hashtable]$Context = @{}
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Translation completed successfully"
        StructuredQuery = @{}
        QueryType = ""
        Parameters = @{}
        ConfidenceScore = 0.75
    }
    
    try {
        # Parse the natural language query
        $parsed = Parse-AXNaturalLanguageQuery -Query $NaturalLanguage -Context $Context
        
        if ($parsed.Status -ne "Success") {
            $result.Status = "Error"
            $result.Message = "Failed to parse natural language query: $($parsed.Message)"
            $result.ConfidenceScore = 0.0
            return $result
        }
        
        # Build structured query based on parsed information
        $structuredQuery = @{
            Type = $parsed.Intent
            Filters = @{}
            Metrics = @()
            TimeRange = @{}
            Limit = 100  # Default limit
        }
        
        # Add component filter if specified
        if ($parsed.Entities.ContainsKey("Component")) {
            $structuredQuery.Filters["Component"] = $parsed.Entities["Component"]
        }
        
        # Add metric filter if specified
        if ($parsed.Entities.ContainsKey("Metric")) {
            $structuredQuery.Metrics = @($parsed.Entities["Metric"])
            $result.QueryType = "MetricsQuery"
        }
        
        # Add time range if specified
        if ($parsed.ExtractedParameters.ContainsKey("TimePeriodHours")) {
            $hours = $parsed.ExtractedParameters["TimePeriodHours"]
            $structuredQuery.TimeRange = @{
                Start = (Get-Date).AddHours(-$hours)
                End = Get-Date
            }
        }
        
        $result.StructuredQuery = $structuredQuery
        $result.Parameters = $parsed.ExtractedParameters
        
        # Set confidence based on entity extraction success
        $result.ConfidenceScore = $parsed.ConfidenceScore
        $result.Message = "Translated natural language to structured query for $($parsed.Intent) intent"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Translation failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Format-AXNaturalResponse {
    <#
    .SYNOPSIS
    Formats monitoring data into natural language responses
    
    .DESCRIPTION
    This function takes raw monitoring data and formats it into easily
    understandable natural language text for users.
    
    .PARAMETER Data
    Raw monitoring data to format
    
    .PARAMETER DataType
    Type of data being formatted
    
    .PARAMETER UserLevel
    Technical level of the user (Beginner, Intermediate, Expert)
    
    .EXAMPLE
    $response = Format-AXNaturalResponse -Data $metrics -DataType "Performance" -UserLevel "Beginner"
    
    .NOTES
    This function tailors responses to the user's technical level.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object]$Data,
        
        [Parameter(Mandatory=$true)]
        [ValidateSet("Performance", "Alert", "Configuration", "Trend", "Predictive")]
        [string]$DataType,
        
        [Parameter()]
        [ValidateSet("Beginner", "Intermediate", "Expert")]
        [string]$UserLevel = "Intermediate"
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Response formatting completed successfully"
        NaturalResponse = ""
        ResponseLevel = $UserLevel
        FormattedData = $null
        ConfidenceScore = 0.90
    }
    
    try {
        $response = ""
        
        switch ($DataType) {
            "Performance" {
                $cpu = if ($Data.CPU -ne $null) { [math]::Round($Data.CPU, 2) } else { "N/A" }
                $memory = if ($Data.Memory -ne $null) { [math]::Round($Data.Memory, 2) } else { "N/A" }
                $sessions = if ($Data.Sessions -ne $null) { $Data.Sessions } else { "N/A" }
                $batchBacklog = if ($Data.BatchBacklog -ne $null) { $Data.BatchBacklog } else { "N/A" }
                
                if ($UserLevel -eq "Beginner") {
                    $response = "Your AX system is currently operating with "
                    $response += "CPU at $cpu% capacity, "
                    $response += "Memory at $memory% usage, "
                    $response += "$sessions active user sessions, and "
                    $response += "a backlog of $batchBacklog batch jobs. "
                    
                    # Add simple interpretation
                    if ($cpu -gt 80) { $response += "CPU is quite high which might slow down your system. " }
                    if ($memory -gt 85) { $response += "Memory usage is elevated and could impact performance. " }
                    if ($batchBacklog -gt 25) { $response += "You have a significant number of batch jobs waiting to process. " }
                    
                    $response += "Everything else looks normal."
                }
                elseif ($UserLevel -eq "Intermediate") {
                    $response = "Current performance metrics for the AX environment: "
                    $response += "CPU utilization: $cpu%, "
                    $response += "Memory utilization: $memory%, "
                    $response += "Active sessions: $sessions, "
                    $response += "Batch job backlog: $batchBacklog. "
                    
                    # Add analysis
                    if ($cpu -gt 85 -or $memory -gt 90 -or $batchBacklog -gt 30) {
                        $response += "Attention required: at least one metric is outside normal operating range."
                    } else {
                        $response += "All metrics are within acceptable ranges."
                    }
                }
                else {  # Expert
                    $response = "System performance snapshot: "
                    $response += "CPU: $cpu% (baseline: $([math]::Round($Data.BaselineCPU, 2))%), "
                    $response += "Memory: $memory% (baseline: $([math]::Round($Data.BaselineMemory, 2))%), "
                    $response += "Sessions: $sessions (max observed: $($Data.MaxSessions)), "
                    $response += "Batch backlog: $batchBacklog (threshold: $($Data.BatchThreshold)). "
                    
                    # Add technical analysis
                    $response += "Deviation analysis: "
                    if ($Data.CPU - $Data.BaselineCPU -gt 10) { 
                        $response += "CPU showing significant deviation from baseline. " 
                    }
                    if ($Data.Memory - $Data.BaselineMemory -gt 10) { 
                        $response += "Memory showing significant deviation from baseline. " 
                    }
                }
            }
            "Alert" {
                # Format an alert notification into natural language
                $severity = $Data.Severity
                $metric = $Data.Metric
                $actualValue = $Data.ActualValue
                $threshold = $Data.Threshold
                $component = if ($Data.Component) { $Data.Component } else { "system" }
                
                if ($UserLevel -eq "Beginner") {
                    $response = "Alert: The $metric on your $component is currently at $actualValue, which is higher than the recommended level of $threshold. Please check your system."
                }
                elseif ($UserLevel -eq "Intermediate") {
                    $response = "[$severity] Alert triggered for $component: $metric exceeded threshold. Current value: $actualValue (threshold: $threshold)."
                }
                else {
                    $response = "[$severity] Alert ID: $($Data.AlertId) | Component: $component | Metric: $metric | Value: $actualValue (threshold: $threshold) | Time: $($Data.Timestamp) | Details: $($Data.Details)"
                }
            }
            "Trend" {
                # Format trend data into natural language
                $metric = $Data.MetricName
                $trend = $Data.TrendDirection
                $changePercent = $Data.ChangePercent
                $timePeriod = $Data.TimePeriod
                
                if ($UserLevel -eq "Beginner") {
                    $response = "The $metric has been $trend over the last $timePeriod, changing by $([math]::Abs($changePercent))%. This indicates the system is experiencing $($trend -replace 'ing', 'ion')."
                }
                elseif ($UserLevel -eq "Intermediate") {
                    $response = "Trend analysis for $metric: $trend over $timePeriod (change: $([math]::Abs($changePercent))%)."
                }
                else {
                    $response = "Trend analysis: $metric showing $trend pattern over $timePeriod. Change: $changePercent% (p-value: $($Data.PValue), confidence: $($Data.ConfidenceInterval))."
                }
            }
        }
        
        $result.NaturalResponse = $response
        $result.FormattedData = $Data
        $result.Message = "Formatted $DataType data for $UserLevel user level"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Response formatting failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

# Export functions
Export-ModuleMember -Function Parse-AXNaturalLanguageQuery, Generate-AXInsightResponse, Extract-AXEntities, Understand-AXQueryIntent, Translate-AXNaturalLanguageToQuery, Format-AXNaturalResponse
# AXMonitor.Diagnostic - Troubleshooting Wizard Module
# Purpose: Provides an interactive troubleshooting wizard for performance issues
# Author: Qwen Code
# Date: 2025-10-27

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Start-TroubleshootingWizard {
    <#
    .SYNOPSIS
    Starts the interactive troubleshooting wizard
    
    .DESCRIPTION
    This function starts an interactive troubleshooting wizard that guides users through diagnosing performance issues.
    It asks questions about the problem and provides targeted solutions based on the answers.
    
    .PARAMETER ProblemType
    Type of problem to troubleshoot (default: "Performance")
    Valid values: "Performance", "Availability", "Configuration", "Security"
    
    .PARAMETER InitialContext
    Initial context information about the problem
    
    .EXAMPLE
    Start-TroubleshootingWizard -ProblemType "Performance" -InitialContext "Slow batch job execution"
    
    .NOTES
    This wizard provides step-by-step guidance for troubleshooting common issues.
    #>
    param(
        [Parameter()]
        [ValidateSet("Performance", "Availability", "Configuration", "Security")]
        [string]$ProblemType = "Performance",
        
        [Parameter()]
        [string]$InitialContext = ""
    )
    
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  AX Monitor - Troubleshooting Wizard" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    
    # Initialize wizard state
    $wizardState = @{
        CurrentStep = 1
        ProblemType = $ProblemType
        Context = $InitialContext
        Answers = @{}
        Recommendations = @()
        ConfidenceScore = 0.80
    }
    
    # Display welcome message
    Write-Host "Welcome to the AX Monitor Troubleshooting Wizard!" -ForegroundColor Green
    Write-Host "I'll help you diagnose and resolve your issue." -ForegroundColor Yellow
    Write-Host "`
"
    
    # Display problem type
    Write-Host "Problem Type: $ProblemType" -ForegroundColor Cyan
    if ($InitialContext) {
        Write-Host "Initial Context: $InitialContext" -ForegroundColor Yellow
    }
    Write-Host "`
"
    
    # Start the wizard steps
    do {
        switch ($wizardState.CurrentStep) {
            1 {
                # Step 1: Gather basic information
                Write-Host "Step 1: Basic Information" -ForegroundColor Cyan
                Write-Host "What specific issue are you experiencing?" -ForegroundColor Yellow
                
                # Get user input
                $issue = Read-Host "Enter a description of the issue (or press Enter for default)"
                if (-not $issue) {
                    $issue = "Unknown issue"
                }
                
                $wizardState.Answers.Issue = $issue
                
                # Ask about when the issue started
                Write-Host "When did this issue start?" -ForegroundColor Yellow
                $startTime = Read-Host "Enter date/time (or press Enter for 'Unknown')"
                if (-not $startTime) {
                    $startTime = "Unknown"
                }
                
                $wizardState.Answers.StartTime = $startTime
                
                # Ask about frequency
                Write-Host "How often does this issue occur?" -ForegroundColor Yellow
                $frequency = Read-Host "Options: Always, Often, Sometimes, Rarely, Once (or press Enter for 'Unknown')"
                if (-not $frequency) {
                    $frequency = "Unknown"
                }
                
                $wizardState.Answers.Frequency = $frequency
                
                # Move to next step
                $wizardState.CurrentStep++
                
                Write-Host "`
"
            }
            
            2 {
                # Step 2: Gather system context
                Write-Host "Step 2: System Context" -ForegroundColor Cyan
                Write-Host "What system components are involved?" -ForegroundColor Yellow
                
                # Get user input for affected components
                $components = Read-Host "Enter affected components (e.g., Batch Jobs, Sessions, SQL Server, etc.)"
                if (-not $components) {
                    $components = "All components"
                }
                
                $wizardState.Answers.Components = $components
                
                # Ask about recent changes
                Write-Host "Have there been any recent changes to the system?" -ForegroundColor Yellow
                $changes = Read-Host "Describe recent changes (or press Enter for 'None')"
                if (-not $changes) {
                    $changes = "None"
                }
                
                $wizardState.Answers.Changes = $changes
                
                # Ask about impact
                Write-Host "What is the business impact of this issue?" -ForegroundColor Yellow
                $impact = Read-Host "Describe impact (e.g., Users can't complete orders, etc.)"
                if (-not $impact) {
                    $impact = "Unknown impact"
                }
                
                $wizardState.Answers.Impact = $impact
                
                # Move to next step
                $wizardState.CurrentStep++
                
                Write-Host "`
"
            }
            
            3 {
                # Step 3: Gather technical details
                Write-Host "Step 3: Technical Details" -ForegroundColor Cyan
                Write-Host "Please provide technical details about the issue:" -ForegroundColor Yellow
                
                # Ask about error messages
                Write-Host "Are there any error messages?" -ForegroundColor Yellow
                $errors = Read-Host "Enter error messages (or press Enter for 'None')"
                if (-not $errors) {
                    $errors = "None"
                }
                
                $wizardState.Answers.Errors = $errors
                
                # Ask about performance metrics
                Write-Host "What performance metrics are affected?" -ForegroundColor Yellow
                $metrics = Read-Host "Enter affected metrics (e.g., CPU usage, response time, etc.)"
                if (-not $metrics) {
                    $metrics = "All metrics"
                }
                
                $wizardState.Answers.Metrics = $metrics
                
                # Ask about correlation with other events
                Write-Host "Is this issue correlated with any other events?" -ForegroundColor Yellow
                $correlation = Read-Host "Describe correlations (or press Enter for 'None')"
                if (-not $correlation) {
                    $correlation = "None"
                }
                
                $wizardState.Answers.Correlation = $correlation
                
                # Move to next step
                $wizardState.CurrentStep++
                
                Write-Host "`
"
            }
            
            4 {
                # Step 4: Generate recommendations
                Write-Host "Step 4: Analysis & Recommendations" -ForegroundColor Cyan
                Write-Host "Analyzing your issue..." -ForegroundColor Yellow
                
                # Generate recommendations based on collected information
                $recommendations = Get-WizardRecommendations -WizardState $wizardState
                
                # Store recommendations
                $wizardState.Recommendations = $recommendations
                
                # Display recommendations
                Write-Host "`
"
                Write-Host "Recommended Actions:" -ForegroundColor Green
                Write-Host "=====================" -ForegroundColor Green
                
                for ($i = 0; $i -lt $recommendations.Count; $i++) {
                    Write-Host "$(($i + 1).ToString().PadLeft(2)). $($recommendations[$i].Title)" -ForegroundColor Cyan
                    Write-Host "   $($recommendations[$i].Description)" -ForegroundColor Yellow
                    Write-Host "   Priority: $($recommendations[$i].Priority)" -ForegroundColor White
                    Write-Host "   Confidence: $($recommendations[$i].Confidence)%" -ForegroundColor White
                    Write-Host "`
"
                }
                
                # Display additional resources
                Write-Host "Additional Resources:" -ForegroundColor Green
                Write-Host "=====================" -ForegroundColor Green
                Write-Host "- Check the system logs for more detailed information" -ForegroundColor Yellow
                Write-Host "- Review the AX Monitor dashboard for real-time metrics" -ForegroundColor Yellow
                Write-Host "- Contact support if the issue persists" -ForegroundColor Yellow
                
                # Set confidence score based on analysis
                $wizardState.ConfidenceScore = 0.85
                
                # Move to final step
                $wizardState.CurrentStep++
                
                Write-Host "`
"
            }
            
            5 {
                # Step 5: Completion
                Write-Host "Step 5: Completion" -ForegroundColor Cyan
                Write-Host "Thank you for using the AX Monitor Troubleshooting Wizard!" -ForegroundColor Green
                Write-Host "`
"
                
                # Display summary
                Write-Host "Issue Summary:" -ForegroundColor Cyan
                Write-Host "==============" -ForegroundColor Cyan
                Write-Host "Problem Type: $($wizardState.ProblemType)" -ForegroundColor Yellow
                Write-Host "Issue: $($wizardState.Answers.Issue)" -ForegroundColor Yellow
                Write-Host "Start Time: $($wizardState.Answers.StartTime)" -ForegroundColor Yellow
                Write-Host "Frequency: $($wizardState.Answers.Frequency)" -ForegroundColor Yellow
                Write-Host "Affected Components: $($wizardState.Answers.Components)" -ForegroundColor Yellow
                Write-Host "Business Impact: $($wizardState.Answers.Impact)" -ForegroundColor Yellow
                
                Write-Host "`
"
                Write-Host "Next Steps:" -ForegroundColor Green
                Write-Host "===========" -ForegroundColor Green
                Write-Host "1. Implement the recommended actions" -ForegroundColor Yellow
                Write-Host "2. Monitor the system to verify resolution" -ForegroundColor Yellow
                Write-Host "3. If the issue persists, contact support" -ForegroundColor Yellow
                
                Write-Host "`
"
                Write-Host "Confidence Score: $($wizardState.ConfidenceScore * 100)%" -ForegroundColor Cyan
                Write-Host "`
"
                
                # Ask if user wants to save the session
                $saveSession = Read-Host "Would you like to save this troubleshooting session? (y/n)"
                if ($saveSession -eq "y" -or $saveSession -eq "Y") {
                    $sessionFile = Save-TroubleshootingSession -WizardState $wizardState
                    Write-Host "Session saved to: $sessionFile" -ForegroundColor Green
                }
                
                # End the wizard
                $wizardState.CurrentStep++
                
                Write-Host "`
"
            }
        }
    } while ($wizardState.CurrentStep -le 5)
    
    # Return wizard results
    return @{
        Status = "Success"
        Message = "Troubleshooting wizard completed"
        WizardState = $wizardState
        ConfidenceScore = $wizardState.ConfidenceScore
    }
}

function Get-WizardRecommendations {
    <#
    .SYNOPSIS
    Generates recommendations based on wizard state
    
    .DESCRIPTION
    This function generates targeted recommendations based on the information collected during the troubleshooting wizard.
    
    .PARAMETER WizardState
    The current state of the wizard
    
    .EXAMPLE
    $recommendations = Get-WizardRecommendations -WizardState $wizardState
    
    .NOTES
    This function uses heuristic rules to generate recommendations based on the collected information.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object]$WizardState
    )
    
    # Initialize recommendations array
    $recommendations = @()
    
    # Get problem type
    $problemType = $WizardState.ProblemType
    $issue = $WizardState.Answers.Issue
    $components = $WizardState.Answers.Components
    $metrics = $WizardState.Answers.Metrics
    $errors = $WizardState.Answers.Errors
    
    # Generate recommendations based on problem type and context
    switch ($problemType) {
        "Performance" {
            # Performance-related recommendations
            
            # General performance recommendations
            $recommendations += @{
                Title = "Review System Resource Utilization"
                Description = "Check CPU, memory, and I/O usage on all affected systems. Look for resource bottlenecks that may be causing performance issues."
                Priority = "High"
                Confidence = 90
            }
            
            $recommendations += @{
                Title = "Analyze Query Performance"
                Description = "Examine slow-running queries in SQL Server. Use the query analyzer to identify inefficient queries that may be impacting performance."
                Priority = "High"
                Confidence = 85
            }
            
            $recommendations += @{
                Title = "Check for Blocking Operations"
                Description = "Identify any blocking operations in the database that may be preventing transactions from completing. Resolve blocking issues to improve performance."
                Priority = "High"
                Confidence = 80
            }
            
            # Specific recommendations based on issue
            if ($issue -like "*slow*" -or $issue -like "*performance*" -or $issue -like "*response time*") {
                $recommendations += @{
                    Title = "Optimize Application Configuration"
                    Description = "Review application configuration settings that may impact performance, such as connection pool sizes, cache settings, and timeout values."
                    Priority = "Medium"
                    Confidence = 75
                }
            }
            
            if ($issue -like "*batch*" -or $issue -like "*job*") {
                $recommendations += @{
                    Title = "Review Batch Job Scheduling"
                    Description = "Examine batch job scheduling to ensure jobs are not running during peak hours. Consider optimizing job timing to reduce performance impact."
                    Priority = "Medium"
                    Confidence = 80
                }
            }
            
            if ($issue -like "*session*" -or $issue -like "*user*") {
                $recommendations += @{
                    Title = "Investigate Session Management"
                    Description = "Check for long-running sessions or sessions that are not being properly released. Implement session cleanup mechanisms if needed."
                    Priority = "Medium"
                    Confidence = 75
                }
            }
            
            # Add recommendations based on metrics
            if ($metrics -like "*CPU*" -or $metrics -like "*processor*") {
                $recommendations += @{
                    Title = "Optimize CPU Usage"
                    Description = "Identify processes consuming excessive CPU resources. Consider optimizing code, adding caching, or scaling resources to reduce CPU load."
                    Priority = "High"
                    Confidence = 85
                }
            }
            
            if ($metrics -like "*memory*" -or $metrics -like "*RAM*") {
                $recommendations += @{
                    Title = "Optimize Memory Usage"
                    Description = "Identify memory leaks or inefficient memory usage patterns. Consider implementing memory management strategies to reduce memory consumption."
                    Priority = "High"
                    Confidence = 80
                }
            }
            
            if ($metrics -like "*I/O*" -or $metrics -like "*disk*") {
                $recommendations += @{
                    Title = "Optimize Disk I/O"
                    Description = "Identify disk I/O bottlenecks and optimize data access patterns. Consider using faster storage or implementing caching to reduce disk I/O."
                    Priority = "High"
                    Confidence = 85
                }
            }
            
            # Add recommendations based on errors
            if ($errors -ne "None") {
                $recommendations += @{
                    Title = "Address Error Messages"
                    Description = "Investigate the specific error messages reported. These often provide clues about the root cause of the issue."
                    Priority = "High"
                    Confidence = 90
                }
            }
        }
        
        "Availability" {
            # Availability-related recommendations
            
            $recommendations += @{
                Title = "Check System Health"
                Description = "Verify that all critical systems are operational and responding to health checks."
                Priority = "High"
                Confidence = 90
            }
            
            $recommendations += @{
                Title = "Review Redundancy Configuration"
                Description = "Ensure that redundancy mechanisms are properly configured and operational to maintain availability during failures."
                Priority = "High"
                Confidence = 85
            }
            
            $recommendations += @{
                Title = "Test Failover Procedures"
                Description = "Verify that failover procedures work as expected and can quickly restore service in case of failure."
                Priority = "High"
                Confidence = 80
            }
            
            # Specific recommendations based on issue
            if ($issue -like "*down*" -or $issue -like "*unavailable*") {
                $recommendations += @{
                    Title = "Investigate Root Cause of Outage"
                    Description = "Perform a thorough investigation to identify the root cause of the outage. This may involve examining logs, monitoring data, and system configurations."
                    Priority = "Critical"
                    Confidence = 95
                }
            }
            
            if ($issue -like "*connect*" -or $issue -like "*access*") {
                $recommendations += @{
                    Title = "Check Network Connectivity"
                    Description = "Verify network connectivity between all components. Check firewalls, DNS, and network routing configurations."
                    Priority = "High"
                    Confidence = 85
                }
            }
        }
        
        "Configuration" {
            # Configuration-related recommendations
            
            $recommendations += @{
                Title = "Review Configuration Settings"
                Description = "Verify that all configuration settings are correct and consistent across all systems."
                Priority = "High"
                Confidence = 90
            }
            
            $recommendations += @{
                Title = "Validate Configuration Changes"
                Description = "If recent changes were made, verify that they were implemented correctly and are not causing issues."
                Priority = "High"
                Confidence = 85
            }
            
            $recommendations += @{
                Title = "Document Configuration Settings"
                Description = "Maintain up-to-date documentation of all configuration settings to facilitate troubleshooting and future changes."
                Priority = "Medium"
                Confidence = 80
            }
            
            # Specific recommendations based on issue
            if ($issue -like "*setting*" -or $issue -like "*config*") {
                $recommendations += @{
                    Title = "Reset to Default Configuration"
                    Description = "If the issue began after a configuration change, consider resetting to the default configuration to isolate the problem."
                    Priority = "Medium"
                    Confidence = 75
                }
            }
            
            if ($issue -like "*permission*" -or $issue -like "*access*") {
                $recommendations += @{
                    Title = "Review Security Settings"
                    Description = "Verify that security settings and permissions are configured correctly to allow proper access while maintaining security."
                    Priority = "High"
                    Confidence = 85
                }
            }
        }
        
        "Security" {
            # Security-related recommendations
            
            $recommendations += @{
                Title = "Review Security Logs"
                Description = "Examine security logs for any suspicious activity or unauthorized access attempts."
                Priority = "High"
                Confidence = 90
            }
            
            $recommendations += @{
                Title = "Verify Authentication Mechanisms"
                Description = "Ensure that authentication mechanisms are properly configured and functioning correctly."
                Priority = "High"
                Confidence = 85
            }
            
            $recommendations += @{
                Title = "Update Security Patches"
                Description = "Apply the latest security patches and updates to all systems to address known vulnerabilities."
                Priority = "High"
                Confidence = 80
            }
            
            # Specific recommendations based on issue
            if ($issue -like "*access*" -or $issue -like "*permission*") {
                $recommendations += @{
                    Title = "Review User Permissions"
                    Description = "Verify that user permissions are configured correctly and follow the principle of least privilege."
                    Priority = "High"
                    Confidence = 85
                }
            }
            
            if ($issue -like "*breach*" -or $issue -like "*attack*") {
                $recommendations += @{
                    Title = "Implement Additional Security Measures"
                    Description = "Consider implementing additional security measures such as intrusion detection, two-factor authentication, or network segmentation."
                    Priority = "Critical"
                    Confidence = 90
                }
            }
        }
    }
    
    # Add general recommendations
    $recommendations += @{
        Title = "Monitor System Metrics"
        Description = "Implement continuous monitoring of key system metrics to detect issues early and track the effectiveness of implemented solutions."
        Priority = "Medium"
        Confidence = 85
    }
    
    $recommendations += @{
        Title = "Document Findings and Solutions"
        Description = "Maintain detailed documentation of the troubleshooting process, findings, and implemented solutions for future reference."
        Priority = "Medium"
        Confidence = 80
    }
    
    return $recommendations
}

function Save-TroubleshootingSession {
    <#
    .SYNOPSIS
    Saves a troubleshooting session to file
    
    .DESCRIPTION
    This function saves the current troubleshooting session to a JSON file for later reference.
    
    .PARAMETER WizardState
    The current state of the wizard
    
    .EXAMPLE
    $sessionFile = Save-TroubleshootingSession -WizardState $wizardState
    
    .NOTES
    This function creates a timestamped file with the session details.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object]$WizardState
    )
    
    # Create timestamp for filename
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $filename = "troubleshooting_session_$timestamp.json"
    
    # Define output directory
    $outputDir = Join-Path $PSScriptRoot "Sessions"
    if (!(Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force
    }
    
    # Create session object
    $session = @{
        Timestamp = (Get-Date).ToString('o')
        ProblemType = $WizardState.ProblemType
        Context = $WizardState.Context
        Answers = $WizardState.Answers
        Recommendations = $WizardState.Recommendations
        ConfidenceScore = $WizardState.ConfidenceScore
    }
    
    # Save session to file
    $filePath = Join-Path $outputDir $filename
    $session | ConvertTo-Json -Depth 10 | Set-Content -Path $filePath
    
    return $filePath
}

function Load-TroubleshootingSession {
    <#
    .SYNOPSIS
    Loads a troubleshooting session from file
    
    .DESCRIPTION
    This function loads a previously saved troubleshooting session from a JSON file.
    
    .PARAMETER FilePath
    Path to the session file
    
    .EXAMPLE
    $session = Load-TroubleshootingSession -FilePath "sessions/troubleshooting_session_20251027_123456.json"
    
    .NOTES
    This function returns the loaded session data.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$FilePath
    )
    
    # Check if file exists
    if (!(Test-Path $FilePath)) {
        Write-Error "Session file not found: $FilePath"
        return $null
    }
    
    # Load session from file
    $sessionJson = Get-Content -Path $FilePath -Raw
    $session = $sessionJson | ConvertFrom-Json
    
    return $session
}

# Export functions
Export-ModuleMember -Function Start-TroubleshootingWizard, Get-WizardRecommendations, Save-TroubleshootingSession, Load-TroubleshootingSession
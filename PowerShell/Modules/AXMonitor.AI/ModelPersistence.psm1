# AXMonitor.AI - Model Persistence and Versioning Module
# Purpose: Provides advanced model persistence and versioning capabilities
# Author: Qwen Code
# Date: 2025-10-29

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Save-ModelWithVersion {
    <#
    .SYNOPSIS
    Saves a trained model with version information and metadata
    
    .DESCRIPTION
    This function saves a model with comprehensive metadata including version, timestamp,
    performance metrics, and training parameters for proper model management.
    
    .PARAMETER Model
    Trained model object to save
    
    .PARAMETER ModelName
    Name for the model (e.g. "CPU_Predictor_v1")
    
    .PARAMETER ModelType
    Type of the model (e.g. "LinearRegression", "TimeSeries")
    
    .PARAMETER Version
    Version string (e.g. "1.0.0")
    
    .PARAMETER FilePath
    Path to save the model file (optional, will generate if not provided)
    
    .PARAMETER Description
    Description of the model purpose and use
    
    .PARAMETER PerformanceMetrics
    Performance metrics to record with the model
    
    .EXAMPLE
    Save-ModelWithVersion -Model $model -ModelName "CPU_Predictor" -Version "1.0.0" -Description "CPU usage predictor for AX servers"
    
    .NOTES
    This function creates a comprehensive record of each model for proper MLOps practices.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object]$Model,
        
        [Parameter(Mandatory=$true)]
        [string]$ModelName,
        
        [Parameter(Mandatory=$true)]
        [string]$ModelType,
        
        [Parameter(Mandatory=$true)]
        [string]$Version,
        
        [Parameter()]
        [string]$FilePath,
        
        [Parameter()]
        [string]$Description = "",
        
        [Parameter()]
        [hashtable]$PerformanceMetrics = @{}
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Model saved successfully with version information"
        ModelPath = $null
        ModelId = $null
        Version = $Version
        ConfidenceScore = 0.90
    }
    
    try {
        # Generate file path if not provided
        if (-not $FilePath) {
            $fileName = "${ModelName}_v${Version}.json"
            $FilePath = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "Models") $fileName
            
            # Create Models directory if it doesn't exist
            $modelDir = Split-Path $FilePath -Parent
            if (-not (Test-Path $modelDir)) {
                New-Item -ItemType Directory -Path $modelDir -Force
            }
        }
        
        # Generate unique model ID
        $modelId = [guid]::NewGuid().ToString()
        
        # Create comprehensive model record with metadata
        $modelRecord = @{
            ModelId = $modelId
            ModelName = $ModelName
            ModelType = $ModelType
            Version = $Version
            Description = $Description
            CreatedAt = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffZ'
            Author = $env:USERNAME
            PerformanceMetrics = $PerformanceMetrics
            TrainingEnvironment = @{
                PowerShellVersion = $PSVersionTable.PSVersion.ToString()
                HostName = hostname
                OS = $PSVersionTable.OS
            }
            Model = $Model
        }
        
        # Save model to JSON file with metadata
        $modelJson = $modelRecord | ConvertTo-Json -Depth 20
        Set-Content -Path $FilePath -Value $modelJson
        
        # Create/update a model registry file to track all models
        $registryPath = Join-Path (Split-Path $FilePath -Parent) "model_registry.json"
        $registry = @{}
        if (Test-Path $registryPath) {
            $registry = Get-Content -Path $registryPath -Raw | ConvertFrom-Json -AsHashtable
        }
        
        # Add model to registry
        if (-not $registry.ContainsKey("Models")) {
            $registry.Models = @()
        }
        
        # Check if model already exists in registry
        $existingModelIndex = -1
        for ($i = 0; $i -lt $registry.Models.Count; $i++) {
            if ($registry.Models[$i].ModelId -eq $modelId) {
                $existingModelIndex = $i
                break
            }
        }
        
        # Create model entry
        $modelEntry = @{
            ModelId = $modelId
            ModelName = $ModelName
            ModelType = $ModelType
            Version = $Version
            FilePath = $FilePath
            CreatedAt = $modelRecord.CreatedAt
            IsActive = $existingModelIndex -eq -1  # Set to active if new model
            PerformanceMetrics = $PerformanceMetrics
        }
        
        if ($existingModelIndex -ge 0) {
            # Update existing entry
            $registry.Models[$existingModelIndex] = $modelEntry
        } else {
            # Add new entry
            $registry.Models += $modelEntry
        }
        
        # Save registry
        $registryJson = $registry | ConvertTo-Json
        Set-Content -Path $registryPath -Value $registryJson
        
        $result.ModelPath = $FilePath
        $result.ModelId = $modelId
        $result.Message = "Model saved successfully to $FilePath with ID: $modelId"
        
        Write-Host "Model saved: $ModelName v$Version to $FilePath"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to save model: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Load-ModelByVersion {
    <#
    .SYNOPSIS
    Loads a specific version of a trained model
    
    .DESCRIPTION
    This function loads a model by its name and version, including its metadata
    and all necessary information for making predictions.
    
    .PARAMETER ModelName
    Name of the model to load
    
    .PARAMETER Version
    Version string (e.g. "1.0.0")
    
    .PARAMETER FilePath
    Path to the model file (optional, will search registry if not provided)
    
    .EXAMPLE
    $model = Load-ModelByVersion -ModelName "CPU_Predictor" -Version "1.0.0"
    
    .NOTES
    This function enables proper model management and version control.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$ModelName,
        
        [Parameter()]
        [string]$Version,
        
        [Parameter()]
        [string]$FilePath
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Model loaded successfully"
        Model = $null
        Metadata = $null
        ModelId = $null
        ConfidenceScore = 0.90
    }
    
    try {
        # If FilePath not provided, look in registry
        if (-not $FilePath) {
            $registryPath = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "Models") "model_registry.json"
            
            if (Test-Path $registryPath) {
                $registry = Get-Content -Path $registryPath -Raw | ConvertFrom-Json -AsHashtable
                
                # Find model by name and version (or latest if no version specified)
                $matchingModel = $null
                if ($Version) {
                    $matchingModel = $registry.Models | Where-Object { 
                        $_.ModelName -eq $ModelName -and $_.Version -eq $Version
                    } | Select-Object -First 1
                } else {
                    $matchingModel = $registry.Models | Where-Object { 
                        $_.ModelName -eq $ModelName
                    } | Sort-Object CreatedAt -Descending | Select-Object -First 1
                }
                
                if ($matchingModel) {
                    $FilePath = $matchingModel.FilePath
                }
            }
        }
        
        if (-not $FilePath -or -not (Test-Path $FilePath)) {
            $result.Status = "Error"
            $result.Message = "Model file not found: $FilePath"
            $result.ConfidenceScore = 0.0
            return $result
        }
        
        # Load model from JSON file
        $modelJson = Get-Content -Path $FilePath -Raw
        $modelRecord = $modelJson | ConvertFrom-Json
        
        $result.Model = $modelRecord.Model
        $result.Metadata = @{
            ModelId = $modelRecord.ModelId
            ModelName = $modelRecord.ModelName
            ModelType = $modelRecord.ModelType
            Version = $modelRecord.Version
            Description = $modelRecord.Description
            CreatedAt = $modelRecord.CreatedAt
            Author = $modelRecord.Author
            PerformanceMetrics = $modelRecord.PerformanceMetrics
            TrainingEnvironment = $modelRecord.TrainingEnvironment
        }
        $result.ModelId = $modelRecord.ModelId
        $result.Message = "Model loaded successfully from $FilePath"
        
        Write-Host "Model loaded: $($modelRecord.ModelName) v$($modelRecord.Version) from $FilePath"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to load model: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Get-ModelVersions {
    <#
    .SYNOPSIS
    Gets all versions of a model
    
    .DESCRIPTION
    This function lists all available versions of a specific model from the registry.
    
    .PARAMETER ModelName
    Name of the model to find versions for
    
    .EXAMPLE
    $versions = Get-ModelVersions -ModelName "CPU_Predictor"
    
    .NOTES
    This function helps with model version management and rollbacks.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$ModelName
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Model versions retrieved successfully"
        Versions = @()
        ModelName = $ModelName
        ConfidenceScore = 0.95
    }
    
    try {
        $registryPath = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "Models") "model_registry.json"
        
        if (Test-Path $registryPath) {
            $registry = Get-Content -Path $registryPath -Raw | ConvertFrom-Json -AsHashtable
            
            $modelVersions = $registry.Models | Where-Object { $_.ModelName -eq $ModelName } | 
                             Sort-Object CreatedAt -Descending
            
            foreach ($model in $modelVersions) {
                $result.Versions += @{
                    ModelId = $model.ModelId
                    Version = $model.Version
                    FilePath = $model.FilePath
                    CreatedAt = $model.CreatedAt
                    IsActive = $model.IsActive
                    PerformanceMetrics = $model.PerformanceMetrics
                }
            }
            
            $result.Message = "Found $($result.Versions.Count) versions for model $ModelName"
        } else {
            $result.Message = "No model registry found"
            $result.ConfidenceScore = 0.5
        }
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to get model versions: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Set-ActiveModel {
    <#
    .SYNOPSIS
    Sets a specific version of a model as active
    
    .DESCRIPTION
    This function marks a specific version of a model as active, which can be used
    for model deployment and A/B testing.
    
    .PARAMETER ModelName
    Name of the model
    
    .PARAMETER Version
    Version to set as active
    
    .EXAMPLE
    Set-ActiveModel -ModelName "CPU_Predictor" -Version "1.0.0"
    
    .NOTES
    This function is helpful for model deployment management.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$ModelName,
        
        [Parameter(Mandatory=$true)]
        [string]$Version
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Active model set successfully"
        ModelName = $ModelName
        Version = $Version
        ConfidenceScore = 0.95
    }
    
    try {
        $registryPath = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "Models") "model_registry.json"
        
        if (-not (Test-Path $registryPath)) {
            $result.Status = "Error"
            $result.Message = "Model registry not found at $registryPath"
            $result.ConfidenceScore = 0.0
            return $result
        }
        
        $registry = Get-Content -Path $registryPath -Raw | ConvertFrom-Json -AsHashtable
        
        # Check if the model version exists
        $targetModel = $registry.Models | Where-Object { 
            $_.ModelName -eq $ModelName -and $_.Version -eq $Version
        } | Select-Object -First 1
        
        if (-not $targetModel) {
            $result.Status = "Error"
            $result.Message = "Model $ModelName version $Version not found"
            $result.ConfidenceScore = 0.0
            return $result
        }
        
        # Set all models with this name to inactive, then the target to active
        foreach ($model in $registry.Models) {
            if ($model.ModelName -eq $ModelName) {
                $model.IsActive = $false
            }
        }
        
        $targetModel.IsActive = $true
        
        # Save updated registry
        $registryJson = $registry | ConvertTo-Json
        Set-Content -Path $registryPath -Value $registryJson
        
        $result.Message = "Model $ModelName version $Version set as active"
        Write-Host $result.Message
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to set active model: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Get-ActiveModel {
    <#
    .SYNOPSIS
    Gets the active version of a model
    
    .DESCRIPTION
    This function retrieves the currently active version of a model, which is
    typically the one used for production predictions.
    
    .PARAMETER ModelName
    Name of the model to get active version for
    
    .EXAMPLE
    $activeModel = Get-ActiveModel -ModelName "CPU_Predictor"
    
    .NOTES
    This function is essential for production model deployments.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$ModelName
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Active model retrieved successfully"
        Model = $null
        Metadata = $null
        IsActive = $false
        ConfidenceScore = 0.95
    }
    
    try {
        $registryPath = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "Models") "model_registry.json"
        
        if (Test-Path $registryPath) {
            $registry = Get-Content -Path $registryPath -Raw | ConvertFrom-Json -AsHashtable
            
            $activeModel = $registry.Models | Where-Object { 
                $_.ModelName -eq $ModelName -and $_.IsActive -eq $true
            } | Select-Object -First 1
            
            if ($activeModel) {
                # Load the actual model
                $modelFilePath = $activeModel.FilePath
                if (Test-Path $modelFilePath) {
                    $modelJson = Get-Content -Path $modelFilePath -Raw | ConvertFrom-Json
                    $result.Model = $modelJson.Model
                    $result.Metadata = @{
                        ModelId = $modelJson.ModelId
                        ModelName = $modelJson.ModelName
                        ModelType = $modelJson.ModelType
                        Version = $modelJson.Version
                        Description = $modelJson.Description
                        CreatedAt = $modelJson.CreatedAt
                        Author = $modelJson.Author
                        PerformanceMetrics = $modelJson.PerformanceMetrics
                    }
                    $result.IsActive = $true
                    $result.Message = "Active model $ModelName v$($activeModel.Version) loaded from $modelFilePath"
                } else {
                    $result.Status = "Warning"
                    $result.Message = "Active model entry found but file not found: $modelFilePath"
                    $result.ConfidenceScore = 0.3
                }
            } else {
                $result.Message = "No active model found for $ModelName"
                $result.ConfidenceScore = 0.5
            }
        } else {
            $result.Message = "No model registry found"
            $result.ConfidenceScore = 0.5
        }
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to get active model: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Remove-ModelVersion {
    <#
    .SYNOPSIS
    Removes a specific version of a model
    
    .DESCRIPTION
    This function removes a specific version of a model from the registry
    and optionally deletes the model file.
    
    .PARAMETER ModelName
    Name of the model
    
    .PARAMETER Version
    Version to remove
    
    .PARAMETER DeleteFile
    Whether to also delete the model file (default: false)
    
    .EXAMPLE
    Remove-ModelVersion -ModelName "CPU_Predictor" -Version "1.0.0" -DeleteFile $true
    
    .NOTES
    This function helps with model cleanup and management.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$ModelName,
        
        [Parameter(Mandatory=$true)]
        [string]$Version,
        
        [Parameter()]
        [bool]$DeleteFile = $false
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Model version removed successfully"
        ModelName = $ModelName
        Version = $Version
        ConfidenceScore = 0.95
    }
    
    try {
        $registryPath = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "Models") "model_registry.json"
        
        if (-not (Test-Path $registryPath)) {
            $result.Status = "Error"
            $result.Message = "Model registry not found at $registryPath"
            $result.ConfidenceScore = 0.0
            return $result
        }
        
        $registry = Get-Content -Path $registryPath -Raw | ConvertFrom-Json -AsHashtable
        
        # Find the model to remove
        $modelsToRemove = $registry.Models | Where-Object { 
            $_.ModelName -eq $ModelName -and $_.Version -eq $Version
        }
        
        if ($modelsToRemove.Count -eq 0) {
            $result.Status = "Error"
            $result.Message = "Model $ModelName version $Version not found"
            $result.ConfidenceScore = 0.0
            return $result
        }
        
        # Remove from registry
        $registry.Models = $registry.Models | Where-Object { 
            -not ($_.ModelName -eq $ModelName -and $_.Version -eq $Version)
        }
        
        # Save updated registry
        $registryJson = $registry | ConvertTo-Json
        Set-Content -Path $registryPath -Value $registryJson
        
        # Optionally delete the model file
        if ($DeleteFile) {
            foreach ($modelToRemove in $modelsToRemove) {
                $filePath = $modelToRemove.FilePath
                if (Test-Path $filePath) {
                    Remove-Item -Path $filePath -Force
                    Write-Host "Deleted model file: $filePath"
                }
            }
        }
        
        $result.Message = "Removed $ModelName version $Version"
        Write-Host $result.Message
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Failed to remove model version: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

# Export functions
Export-ModuleMember -Function Save-ModelWithVersion, Load-ModelByVersion, Get-ModelVersions, Set-ActiveModel, Get-ActiveModel, Remove-ModelVersion
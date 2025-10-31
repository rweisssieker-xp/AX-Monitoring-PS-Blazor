<#
.SYNOPSIS
    Database connectivity module for AX Monitor
.DESCRIPTION
    Provides SQL Server connectivity for AX 2012 R3 and staging databases
    Enforces READ-ONLY mode for AX database
#>

# Import configuration module
Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Config') -Force

function Test-AXDatabaseConnection {
    <#
    .SYNOPSIS
        Test database connectivity
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    try {
        # Test AX Database
        $axConnString = Get-AXConnectionString -DatabaseConfig $Config.AXDatabase
        $axConn = New-Object System.Data.Odbc.OdbcConnection($axConnString)
        $axConn.Open()
        $axConn.Close()
        
        # Test Staging Database
        $stagingConnString = Get-AXConnectionString -DatabaseConfig $Config.StagingDatabase
        $stagingConn = New-Object System.Data.Odbc.OdbcConnection($stagingConnString)
        $stagingConn.Open()
        $stagingConn.Close()
        
        return @{
            Success = $true
            Message = 'Database connections successful'
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

function Invoke-AXQuery {
    <#
    .SYNOPSIS
        Execute a READ-ONLY query against AX database
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Query,
        
        [Parameter()]
        [hashtable]$Parameters = @{},
        
        [Parameter()]
        [int]$Timeout = 60
    )
    
    # Validate query is read-only
    if ($Query -notmatch '^\s*SELECT' -and $Query -notmatch '^\s*WITH') {
        throw "Only SELECT queries are allowed on AX database (READ-ONLY mode)"
    }
    
    $connectionString = Get-AXConnectionString -DatabaseConfig $Config.AXDatabase
    $connection = New-Object System.Data.Odbc.OdbcConnection($connectionString)
    
    try {
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        $command.CommandTimeout = $Timeout
        
        # Add parameters
        foreach ($key in $Parameters.Keys) {
            $param = $command.CreateParameter()
            $param.ParameterName = $key
            $param.Value = $Parameters[$key]
            [void]$command.Parameters.Add($param)
        }
        
        $adapter = New-Object System.Data.Odbc.OdbcDataAdapter($command)
        $dataSet = New-Object System.Data.DataSet
        [void]$adapter.Fill($dataSet)
        
        # Convert to PowerShell objects
        $results = @()
        if ($dataSet.Tables.Count -gt 0) {
            foreach ($row in $dataSet.Tables[0].Rows) {
                $obj = @{}
                foreach ($col in $dataSet.Tables[0].Columns) {
                    $obj[$col.ColumnName] = $row[$col.ColumnName]
                }
                $results += [PSCustomObject]$obj
            }
        }
        
        return $results
    }
    catch {
        Write-Error "Query execution failed: $($_.Exception.Message)"
        throw
    }
    finally {
        if ($connection.State -eq 'Open') {
            $connection.Close()
        }
        $connection.Dispose()
    }
}

function Invoke-StagingQuery {
    <#
    .SYNOPSIS
        Execute a query against staging database (READ/WRITE)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Query,
        
        [Parameter()]
        [hashtable]$Parameters = @{},
        
        [Parameter()]
        [int]$Timeout = 60,
        
        [Parameter()]
        [switch]$NonQuery
    )
    
    $connectionString = Get-AXConnectionString -DatabaseConfig $Config.StagingDatabase
    $connection = New-Object System.Data.Odbc.OdbcConnection($connectionString)
    
    try {
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        $command.CommandTimeout = $Timeout
        
        # Add parameters
        foreach ($key in $Parameters.Keys) {
            $param = $command.CreateParameter()
            $param.ParameterName = $key
            $param.Value = $Parameters[$key]
            [void]$command.Parameters.Add($param)
        }
        
        if ($NonQuery) {
            $rowsAffected = $command.ExecuteNonQuery()
            return $rowsAffected
        }
        else {
            $adapter = New-Object System.Data.Odbc.OdbcDataAdapter($command)
            $dataSet = New-Object System.Data.DataSet
            [void]$adapter.Fill($dataSet)
            
            # Convert to PowerShell objects
            $results = @()
            if ($dataSet.Tables.Count -gt 0) {
                foreach ($row in $dataSet.Tables[0].Rows) {
                    $obj = @{}
                    foreach ($col in $dataSet.Tables[0].Columns) {
                        $obj[$col.ColumnName] = $row[$col.ColumnName]
                    }
                    $results += [PSCustomObject]$obj
                }
            }
            
            return $results
        }
    }
    catch {
        Write-Error "Query execution failed: $($_.Exception.Message)"
        throw
    }
    finally {
        if ($connection.State -eq 'Open') {
            $connection.Close()
        }
        $connection.Dispose()
    }
}

function Initialize-StagingDatabase {
    <#
    .SYNOPSIS
        Initialize staging database schema
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    Write-Host "Initializing staging database schema..." -ForegroundColor Cyan
    
    # Create metrics table
    $createMetricsTable = @'
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Metrics')
BEGIN
    CREATE TABLE Metrics (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        Timestamp DATETIME2 NOT NULL,
        MetricType NVARCHAR(50) NOT NULL,
        MetricName NVARCHAR(100) NOT NULL,
        Value FLOAT NOT NULL,
        Tags NVARCHAR(MAX),
        CreatedAt DATETIME2 DEFAULT GETDATE()
    )
    
    CREATE INDEX IX_Metrics_Timestamp ON Metrics(Timestamp)
    CREATE INDEX IX_Metrics_Type ON Metrics(MetricType)
END
'@
    
    # Create alerts table
    $createAlertsTable = @'
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Alerts')
BEGIN
    CREATE TABLE Alerts (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        AlertType NVARCHAR(100) NOT NULL,
        Severity NVARCHAR(20) NOT NULL,
        Message NVARCHAR(MAX) NOT NULL,
        Details NVARCHAR(MAX),
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        AcknowledgedBy NVARCHAR(100),
        AcknowledgedAt DATETIME2,
        ResolvedAt DATETIME2,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    )
    
    CREATE INDEX IX_Alerts_Status ON Alerts(Status)
    CREATE INDEX IX_Alerts_CreatedAt ON Alerts(CreatedAt)
END
'@
    
    # Create batch jobs history table
    $createBatchHistoryTable = @'
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BatchJobHistory')
BEGIN
    CREATE TABLE BatchJobHistory (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        BatchJobId NVARCHAR(50) NOT NULL,
        JobName NVARCHAR(200),
        Status NVARCHAR(50),
        StartTime DATETIME2,
        EndTime DATETIME2,
        DurationSeconds INT,
        AOSServer NVARCHAR(50),
        ErrorMessage NVARCHAR(MAX),
        CollectedAt DATETIME2 DEFAULT GETDATE()
    )
    
    CREATE INDEX IX_BatchJobHistory_BatchJobId ON BatchJobHistory(BatchJobId)
    CREATE INDEX IX_BatchJobHistory_CollectedAt ON BatchJobHistory(CollectedAt)
END
'@
    
    # Create sessions history table
    $createSessionsHistoryTable = @'
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SessionHistory')
BEGIN
    CREATE TABLE SessionHistory (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        SessionId NVARCHAR(50) NOT NULL,
        UserId NVARCHAR(100),
        AOSServer NVARCHAR(50),
        LoginTime DATETIME2,
        LastActivity DATETIME2,
        Status NVARCHAR(20),
        CollectedAt DATETIME2 DEFAULT GETDATE()
    )
    
    CREATE INDEX IX_SessionHistory_SessionId ON SessionHistory(SessionId)
    CREATE INDEX IX_SessionHistory_CollectedAt ON SessionHistory(CollectedAt)
END
'@
    
    # Create AI analysis table
    $createAIAnalysisTable = @'
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AIAnalysis')
BEGIN
    CREATE TABLE AIAnalysis (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        AnalysisType NVARCHAR(50) NOT NULL,
        Input NVARCHAR(MAX),
        Output NVARCHAR(MAX),
        Confidence FLOAT,
        Model NVARCHAR(50),
        TokensUsed INT,
        CreatedAt DATETIME2 DEFAULT GETDATE()
    )
    
    CREATE INDEX IX_AIAnalysis_AnalysisType ON AIAnalysis(AnalysisType)
    CREATE INDEX IX_AIAnalysis_CreatedAt ON AIAnalysis(CreatedAt)
END
'@
    
    try {
        Invoke-StagingQuery -Config $Config -Query $createMetricsTable -NonQuery
        Invoke-StagingQuery -Config $Config -Query $createAlertsTable -NonQuery
        Invoke-StagingQuery -Config $Config -Query $createBatchHistoryTable -NonQuery
        Invoke-StagingQuery -Config $Config -Query $createSessionsHistoryTable -NonQuery
        Invoke-StagingQuery -Config $Config -Query $createAIAnalysisTable -NonQuery
        
        Write-Host "Staging database initialized successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "ERROR: Failed to initialize staging database: $($_.Exception.Message)" -ForegroundColor Red
        throw
    }
}

function Save-MetricToStaging {
    <#
    .SYNOPSIS
        Save a metric to staging database
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$MetricType,
        
        [Parameter(Mandatory)]
        [string]$MetricName,
        
        [Parameter(Mandatory)]
        [double]$Value,
        
        [Parameter()]
        [hashtable]$Tags = @{}
    )
    
    $query = @'
INSERT INTO Metrics (Timestamp, MetricType, MetricName, Value, Tags)
VALUES (GETDATE(), ?, ?, ?, ?)
'@
    
    $tagsJson = $Tags | ConvertTo-Json -Compress
    
    $params = @{
        '@p1' = $MetricType
        '@p2' = $MetricName
        '@p3' = $Value
        '@p4' = $tagsJson
    }
    
    Invoke-StagingQuery -Config $Config -Query $query -Parameters $params -NonQuery
}

# Export module members
Export-ModuleMember -Function @(
    'Test-AXDatabaseConnection'
    'Invoke-AXQuery'
    'Invoke-StagingQuery'
    'Initialize-StagingDatabase'
    'Save-MetricToStaging'
)

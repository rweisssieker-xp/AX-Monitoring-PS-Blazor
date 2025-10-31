<#
.SYNOPSIS
    Authentication and Authorization module for AX Monitor
.DESCRIPTION
    Provides JWT-based authentication and role-based access control (RBAC)
    for AX Monitor web interface and API endpoints
#>

# Import required modules
Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Config') -Force
Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Database') -Force

# Define available roles
$Script:AvailableRoles = @(
    @{ Name = 'Viewer'; Description = 'Can view dashboards and metrics' }
    @{ Name = 'Power-User'; Description = 'Can view, acknowledge alerts, and run reports' }
    @{ Name = 'Admin'; Description = 'Full access including user management and configuration' }
)

function New-AXUser {
    <#
    .SYNOPSIS
        Create a new user in the system
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Username,
        
        [Parameter(Mandatory)]
        [string]$Password,
        
        [Parameter(Mandatory)]
        [string]$Role,
        
        [Parameter()]
        [string]$Email = '',
        
        [Parameter()]
        [string]$FullName = ''
    )
    
    # Validate role
    if ($Role -notin ($Script:AvailableRoles.Name)) {
        throw "Invalid role: $Role. Available roles: $($Script:AvailableRoles.Name -join ', ')"
    }
    
    # Hash the password
    $salt = New-Salt
    $hashedPassword = Hash-Password -Password $Password -Salt $salt
    
    # Insert user into database
    $query = @'
INSERT INTO Users (Username, PasswordHash, Salt, Role, Email, FullName, IsActive, CreatedAt, UpdatedAt)
VALUES (?, ?, ?, ?, ?, ?, 1, GETDATE(), GETDATE())
'@
    
    $params = @{
        '@p1' = $Username
        '@p2' = $hashedPassword
        '@p3' = $salt
        '@p4' = $Role
        '@p5' = $Email
        '@p6' = $FullName
    }
    
    $result = Invoke-StagingQuery -Config $Config -Query $query -Parameters $params -NonQuery
    return $result -gt 0
}

function Get-AXUser {
    <#
    .SYNOPSIS
        Retrieve user by username
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Username
    )
    
    $query = @'
SELECT Id, Username, PasswordHash, Salt, Role, Email, FullName, IsActive, CreatedAt, UpdatedAt
FROM Users
WHERE Username = ? AND IsActive = 1
'@
    
    $params = @{ '@p1' = $Username }
    $users = Invoke-StagingQuery -Config $Config -Query $query -Parameters $params
    
    if ($users.Count -gt 0) {
        return $users[0]
    }
    return $null
}

function Test-AXUserCredentials {
    <#
    .SYNOPSIS
        Validate user credentials
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Username,
        
        [Parameter(Mandatory)]
        [string]$Password
    )
    
    $user = Get-AXUser -Config $Config -Username $Username
    if (-not $user) {
        return $false
    }
    
    $hashedPassword = Hash-Password -Password $Password -Salt $user.Salt
    return $hashedPassword -eq $user.PasswordHash
}

function New-AXJWTToken {
    <#
    .SYNOPSIS
        Create a new JWT token for a user
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$UserId,
        
        [Parameter(Mandatory)]
        [string]$Username,
        
        [Parameter(Mandatory)]
        [string]$Role,
        
        [Parameter()]
        [int]$ExpiresInMinutes = 60
    )
    
    # Create payload
    $payload = @{
        sub = $UserId
        username = $Username
        role = $Role
        exp = [int][Math]::Floor([double]((Get-Date).AddMinutes($ExpiresInMinutes).ToFileTime() / 10000000 + 11644473600))
        iat = [int][Math]::Floor([double]((Get-Date).ToFileTime() / 10000000 + 11644473600))
    }
    
    # Encode header and payload
    $header = @{ alg = 'HS256'; typ = 'JWT' } | ConvertTo-Json -Compress
    $payloadJson = $payload | ConvertTo-Json -Compress
    
    $headerBase64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($header)) -replace '=', ''
    $payloadBase64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($payloadJson)) -replace '=', ''
    
    # Create signature
    $message = "$headerBase64.$payloadBase64"
    $secret = $Config.Security.JWTSecret
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($secret)
    $signatureBytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($message))
    $signature = [Convert]::ToBase64String($signatureBytes) -replace '=', ''
    
    return "$message.$signature"
}

function Validate-AXJWTToken {
    <#
    .SYNOPSIS
        Validate a JWT token and return user info
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Token
    )
    
    try {
        $parts = $Token.Split('.')
        if ($parts.Count -ne 3) {
            throw "Invalid token format"
        }
        
        $headerBase64 = $parts[0]
        $payloadBase64 = $parts[1]
        $signatureBase64 = $parts[2]
        
        # Add padding if needed
        while ($headerBase64.Length % 4) { $headerBase64 += '=' }
        while ($payloadBase64.Length % 4) { $payloadBase64 += '=' }
        while ($signatureBase64.Length % 4) { $signatureBase64 += '=' }
        
        # Decode payload
        $payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payloadBase64))
        $payload = $payloadJson | ConvertFrom-Json
        
        # Check expiration
        $exp = [int]$payload.exp
        $now = [int][Math]::Floor([double]((Get-Date).ToFileTime() / 10000000 + 11644473600))
        if ($now -gt $exp) {
            throw "Token has expired"
        }
        
        # Verify signature
        $secret = $Config.Security.JWTSecret
        $message = "$($parts[0]).$($parts[1])"
        $hmac = New-Object System.Security.Cryptography.HMACSHA256
        $hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($secret)
        $expectedSignature = [Convert]::ToBase64String($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($message))) -replace '=', ''
        
        if ($expectedSignature -ne $parts[2]) {
            throw "Invalid token signature"
        }
        
        return @{
            UserId = $payload.sub
            Username = $payload.username
            Role = $payload.role
            IsValid = $true
        }
    }
    catch {
        Write-Verbose "JWT validation failed: $($_.Exception.Message)"
        return @{
            UserId = $null
            Username = $null
            Role = $null
            IsValid = $false
        }
    }
}

function Test-AXUserPermission {
    <#
    .SYNOPSIS
        Check if user has permission for a specific action
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$UserRole,
        
        [Parameter(Mandatory)]
        [string]$RequiredRole
    )
    
    # Define role hierarchy (Viewer < Power-User < Admin)
    $roleHierarchy = @{
        'Viewer' = 1
        'Power-User' = 2
        'Admin' = 3
    }
    
    if (-not $roleHierarchy.ContainsKey($UserRole)) {
        return $false
    }
    
    if (-not $roleHierarchy.ContainsKey($RequiredRole)) {
        return $false
    }
    
    return $roleHierarchy[$UserRole] -ge $roleHierarchy[$RequiredRole]
}

function Initialize-AXAuthDatabase {
    <#
    .SYNOPSIS
        Initialize authentication tables in staging database
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    Write-Host "Initializing authentication database schema..." -ForegroundColor Cyan
    
    # Create Users table
    $createUsersTable = @'
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) UNIQUE NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        Salt NVARCHAR(255) NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        Email NVARCHAR(255),
        FullName NVARCHAR(255),
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    )
    
    CREATE INDEX IX_Users_Username ON Users(Username)
    CREATE INDEX IX_Users_Role ON Users(Role)
END
'@
    
    # Create User Sessions table
    $createSessionsTable = @'
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserSessions')
BEGIN
    CREATE TABLE UserSessions (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId BIGINT NOT NULL,
        Token NVARCHAR(500) NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        LastAccessedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(Id)
    )
    
    CREATE INDEX IX_UserSessions_Token ON UserSessions(Token)
    CREATE INDEX IX_UserSessions_ExpiresAt ON UserSessions(ExpiresAt)
END
'@
    
    # Create API Access Logs table
    $createAccessLogsTable = @'
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'APIAccessLogs')
BEGIN
    CREATE TABLE APIAccessLogs (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId BIGINT,
        Endpoint NVARCHAR(500) NOT NULL,
        Method NVARCHAR(10) NOT NULL,
        IP NVARCHAR(45),
        UserAgent NVARCHAR(500),
        ResponseCode INT,
        Timestamp DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(Id)
    )
    
    CREATE INDEX IX_APIAccessLogs_Timestamp ON APIAccessLogs(Timestamp)
    CREATE INDEX IX_APIAccessLogs_UserId ON APIAccessLogs(UserId)
END
'@
    
    try {
        Invoke-StagingQuery -Config $Config -Query $createUsersTable -NonQuery
        Invoke-StagingQuery -Config $Config -Query $createSessionsTable -NonQuery
        Invoke-StagingQuery -Config $Config -Query $createAccessLogsTable -NonQuery
        
        Write-Host "Authentication database initialized successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "ERROR: Failed to initialize authentication database: $($_.Exception.Message)" -ForegroundColor Red
        throw
    }
}

function New-Salt {
    <#
    .SYNOPSIS
        Generate a random salt for password hashing
    #>
    [CmdletBinding()]
    param()
    
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $bytes = New-Object byte[] 32
    $rng.GetBytes($bytes)
    $rng.Dispose()
    
    return [Convert]::ToBase64String($bytes)
}

function Hash-Password {
    <#
    .SYNOPSIS
        Hash a password with salt using PBKDF2
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Password,
        
        [Parameter(Mandatory)]
        [string]$Salt,
        
        [Parameter()]
        [int]$Iterations = 10000
    )
    
    $r = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($Password, [System.Text.Encoding]::UTF8.GetBytes($Salt), $Iterations, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
    $bytes = $r.GetBytes(32)
    $r.Dispose()
    
    return [Convert]::ToBase64String($bytes)
}

function Get-AXRoles {
    <#
    .SYNOPSIS
        Get all available roles
    #>
    return $Script:AvailableRoles
}

function Get-AXUsers {
    <#
    .SYNOPSIS
        Get all users (admin only)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    $query = @'
SELECT Id, Username, Role, Email, FullName, IsActive, CreatedAt, UpdatedAt
FROM Users
ORDER BY CreatedAt DESC
'@
    
    return Invoke-StagingQuery -Config $Config -Query $query
}

function Update-AXUser {
    <#
    .SYNOPSIS
        Update user information
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [int]$UserId,
        
        [Parameter()]
        [string]$Role,
        
        [Parameter()]
        [string]$Email,
        
        [Parameter()]
        [string]$FullName,
        
        [Parameter()]
        [bool]$IsActive
    )
    
    $setClauses = @()
    $params = @{}
    $paramIndex = 1
    
    if ($PSBoundParameters.ContainsKey('Role')) {
        $setClauses += "Role = @p$paramIndex"
        $params["@p$paramIndex"] = $Role
        $paramIndex++
    }
    
    if ($PSBoundParameters.ContainsKey('Email')) {
        $setClauses += "Email = @p$paramIndex"
        $params["@p$paramIndex"] = $Email
        $paramIndex++
    }
    
    if ($PSBoundParameters.ContainsKey('FullName')) {
        $setClauses += "FullName = @p$paramIndex"
        $params["@p$paramIndex"] = $FullName
        $paramIndex++
    }
    
    if ($PSBoundParameters.ContainsKey('IsActive')) {
        $setClauses += "IsActive = @p$paramIndex"
        $params["@p$paramIndex"] = $IsActive
        $paramIndex++
    }
    
    $setClauses += "UpdatedAt = GETDATE()"
    
    if ($setClauses.Count -eq 1) {  # Only UpdatedAt was added
        throw "No fields to update"
    }
    
    $query = "UPDATE Users SET $($setClauses -join ', ') WHERE Id = @p$paramIndex"
    $params["@p$paramIndex"] = $UserId
    
    $result = Invoke-StagingQuery -Config $Config -Query $query -Parameters $params -NonQuery
    return $result -gt 0
}

function Remove-AXUser {
    <#
    .SYNOPSIS
        Remove a user (soft delete by deactivating)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [int]$UserId
    )
    
    $query = @'
UPDATE Users 
SET IsActive = 0, UpdatedAt = GETDATE()
WHERE Id = ?
'@
    
    $params = @{ '@p1' = $UserId }
    $result = Invoke-StagingQuery -Config $Config -Query $query -Parameters $params -NonQuery
    return $result -gt 0
}

function Log-APIAccess {
    <#
    .SYNOPSIS
        Log API access for audit purposes
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [int]$UserId,
        
        [Parameter(Mandatory)]
        [string]$Endpoint,
        
        [Parameter(Mandatory)]
        [string]$Method,
        
        [Parameter(Mandatory)]
        [string]$IP,
        
        [Parameter(Mandatory)]
        [string]$UserAgent,
        
        [Parameter(Mandatory)]
        [int]$ResponseCode
    )
    
    $query = @'
INSERT INTO APIAccessLogs (UserId, Endpoint, Method, IP, UserAgent, ResponseCode, Timestamp)
VALUES (?, ?, ?, ?, ?, ?, GETDATE())
'@
    
    $params = @{
        '@p1' = $UserId
        '@p2' = $Endpoint
        '@p3' = $Method
        '@p4' = $IP
        '@p5' = $UserAgent
        '@p6' = $ResponseCode
    }
    
    Invoke-StagingQuery -Config $Config -Query $query -Parameters $params -NonQuery
}

# Export module members
Export-ModuleMember -Function @(
    'New-AXUser',
    'Get-AXUser',
    'Test-AXUserCredentials',
    'New-AXJWTToken',
    'Validate-AXJWTToken',
    'Test-AXUserPermission',
    'Initialize-AXAuthDatabase',
    'Get-AXRoles',
    'Get-AXUsers',
    'Update-AXUser',
    'Remove-AXUser',
    'Log-APIAccess'
)
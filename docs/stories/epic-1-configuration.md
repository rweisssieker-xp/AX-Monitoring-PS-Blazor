# Epic 1: Configuration & Secrets Management

**Status:** Not Started  
**Priority:** Critical  
**Estimated Effort:** 1-2 days  
**Dependencies:** Epic 0 (Setup)

---

## Epic Goal

Implement a robust, secure, and environment-aware configuration system that supports multiple deployment environments (DEV, TST, PRD) with proper secrets management and runtime configuration validation.

---

## User Story

**As a** DevOps Engineer  
**I want** to configure the application for different environments without code changes  
**So that** I can deploy the same codebase to DEV, TST, and PRD with environment-specific settings

---

## Acceptance Criteria

### AC1: Environment Variable Management
- [ ] `.env.example` template created with all required variables documented
- [ ] Support for environment-specific files: `.env.dev`, `.env.tst`, `.env.prd`
- [ ] Runtime environment selection via `APP_ENV` variable (defaults to DEV)
- [ ] All `.env*` files (except `.example`) in `.gitignore`
- [ ] `python-dotenv` loads correct file based on `APP_ENV`

### AC2: Configuration Schema Defined
- [ ] `app/config.py` module created
- [ ] Configuration dataclass/Pydantic model defined with:
  - Environment name (DEV/TST/PRD)
  - AX database connection (driver, server, database, user, password)
  - Staging database connection (separate from AX)
  - SMTP settings (host, port, TLS, from address)
  - Alert recipients list
  - Cache TTL settings
  - Scheduler intervals (batch, session, health)
  - Feature toggles (optional)
- [ ] Type hints for all configuration fields
- [ ] Validation of required fields on load

### AC3: Database Connection Strings
- [ ] Support for both pyodbc and pymssql (driver selection via config)
- [ ] Connection string builder for SQL Server ODBC
- [ ] Separate connections for AX (read-only) and Staging (read-write)
- [ ] Connection timeout configurable (default: 30s)
- [ ] Query timeout configurable (default: 60s)

### AC4: Configuration Validation
- [ ] Validate all required environment variables are set
- [ ] Raise clear error messages for missing variables
- [ ] Validate environment name is one of: DEV, TST, PRD
- [ ] Validate SMTP port is numeric
- [ ] Validate cache TTL is positive integer
- [ ] Log loaded configuration (with secrets redacted)

### AC5: Health Check Function
- [ ] `health_check()` function tests:
  - AX database connectivity (SELECT 1)
  - Staging database connectivity
  - SMTP server connectivity (optional test email)
- [ ] Health check can be called from Admin page
- [ ] Health check results include timestamp and duration
- [ ] Failures logged with detailed error messages

### AC6: Secrets Security
- [ ] No secrets in source code (all from env vars)
- [ ] `.env` files documented to be restricted (NTFS ACLs in production)
- [ ] Password fields redacted in logs
- [ ] Connection strings not logged verbatim
- [ ] Documentation on secret rotation process

---

## Technical Implementation

### Configuration Module (`app/config.py`)

```python
"""
Configuration management for AX Performance Monitor.

Loads settings from environment variables using python-dotenv.
Supports multiple environments (DEV, TST, PRD) via APP_ENV variable.
"""

import os
from dataclasses import dataclass
from typing import Optional
from dotenv import load_dotenv
import structlog

log = structlog.get_logger()


@dataclass
class DatabaseConfig:
    """Database connection configuration."""
    driver: str
    server: str
    database: str
    user: str
    password: str
    timeout: int = 30


@dataclass
class SMTPConfig:
    """SMTP server configuration for alerts."""
    host: str
    port: int
    from_address: str
    use_tls: bool = False
    username: Optional[str] = None
    password: Optional[str] = None


@dataclass
class SchedulerConfig:
    """Background scheduler intervals (seconds)."""
    batch_interval: int = 120
    session_interval: int = 30
    health_interval: int = 300
    deadlock_interval: int = 120


@dataclass
class CacheConfig:
    """Caching configuration."""
    ttl_overview: int = 30
    ttl_batch: int = 60
    ttl_sessions: int = 30
    ttl_sql_health: int = 120
    max_size: int = 100


@dataclass
class AppConfig:
    """Complete application configuration."""
    environment: str
    ax_db: DatabaseConfig
    staging_db: DatabaseConfig
    smtp: SMTPConfig
    scheduler: SchedulerConfig
    cache: CacheConfig
    alert_recipients: list[str]
    log_level: str = "INFO"


def load_config() -> AppConfig:
    """
    Load configuration from environment variables.
    
    Environment-specific .env files are loaded based on APP_ENV:
    - APP_ENV=DEV → .env.dev
    - APP_ENV=TST → .env.tst
    - APP_ENV=PRD → .env.prd
    
    Returns:
        AppConfig instance with all settings loaded
        
    Raises:
        ValueError: If required environment variables are missing
    """
    # Determine environment
    env = os.getenv('APP_ENV', 'DEV').upper()
    if env not in ['DEV', 'TST', 'PRD']:
        raise ValueError(f"Invalid APP_ENV: {env}. Must be DEV, TST, or PRD.")
    
    # Load environment-specific .env file
    env_file = f'.env.{env.lower()}'
    if not load_dotenv(env_file):
        log.warning("env_file_not_found", file=env_file, fallback=".env")
        load_dotenv('.env')  # Fallback
    
    # Helper to get required env var
    def get_required(key: str) -> str:
        value = os.getenv(key)
        if not value:
            raise ValueError(f"Required environment variable not set: {key}")
        return value
    
    # Build configuration
    config = AppConfig(
        environment=env,
        ax_db=DatabaseConfig(
            driver=get_required('AX_DB_DRIVER'),
            server=get_required('AX_DB_SERVER'),
            database=get_required('AX_DB_NAME'),
            user=get_required('AX_DB_USER'),
            password=get_required('AX_DB_PASSWORD'),
            timeout=int(os.getenv('AX_DB_TIMEOUT', '30'))
        ),
        staging_db=DatabaseConfig(
            driver=get_required('STAGING_DB_DRIVER'),
            server=get_required('STAGING_DB_SERVER'),
            database=get_required('STAGING_DB_NAME'),
            user=get_required('STAGING_DB_USER'),
            password=get_required('STAGING_DB_PASSWORD'),
            timeout=int(os.getenv('STAGING_DB_TIMEOUT', '30'))
        ),
        smtp=SMTPConfig(
            host=get_required('SMTP_HOST'),
            port=int(os.getenv('SMTP_PORT', '25')),
            from_address=os.getenv('SMTP_FROM', 'axmonitor@corp.local'),
            use_tls=os.getenv('SMTP_USE_TLS', 'false').lower() == 'true',
            username=os.getenv('SMTP_USERNAME'),
            password=os.getenv('SMTP_PASSWORD')
        ),
        scheduler=SchedulerConfig(
            batch_interval=int(os.getenv('SCHEDULER_BATCH_INTERVAL', '120')),
            session_interval=int(os.getenv('SCHEDULER_SESSION_INTERVAL', '30')),
            health_interval=int(os.getenv('SCHEDULER_HEALTH_INTERVAL', '300')),
            deadlock_interval=int(os.getenv('SCHEDULER_DEADLOCK_INTERVAL', '120'))
        ),
        cache=CacheConfig(
            ttl_overview=int(os.getenv('CACHE_TTL_OVERVIEW', '30')),
            ttl_batch=int(os.getenv('CACHE_TTL_BATCH', '60')),
            ttl_sessions=int(os.getenv('CACHE_TTL_SESSIONS', '30')),
            ttl_sql_health=int(os.getenv('CACHE_TTL_SQL_HEALTH', '120')),
            max_size=int(os.getenv('CACHE_MAX_SIZE', '100'))
        ),
        alert_recipients=get_required('ALERT_RECIPIENTS').split(','),
        log_level=os.getenv('LOG_LEVEL', 'INFO').upper()
    )
    
    # Log loaded config (with secrets redacted)
    log.info(
        "configuration_loaded",
        environment=config.environment,
        ax_server=config.ax_db.server,
        ax_database=config.ax_db.database,
        staging_server=config.staging_db.server,
        smtp_host=config.smtp.host,
        log_level=config.log_level
    )
    
    return config


def get_connection_string(db_config: DatabaseConfig) -> str:
    """
    Build ODBC connection string from DatabaseConfig.
    
    Args:
        db_config: Database configuration
        
    Returns:
        ODBC connection string
    """
    if 'odbc' in db_config.driver.lower():
        return (
            f"DRIVER={{{db_config.driver}}};"
            f"SERVER={db_config.server};"
            f"DATABASE={db_config.database};"
            f"UID={db_config.user};"
            f"PWD={db_config.password};"
        )
    else:
        # pymssql connection string
        return (
            f"server={db_config.server};"
            f"database={db_config.database};"
            f"user={db_config.user};"
            f"password={db_config.password};"
        )
```

### Example `.env.example`

```env
# ==============================================
# AX Performance Monitor - Environment Template
# ==============================================
# Copy this file to .env.dev, .env.tst, .env.prd
# and fill in environment-specific values.
#
# SECURITY: Never commit .env files to Git!
# ==============================================

# Environment (DEV, TST, PRD)
APP_ENV=DEV

# ==============================================
# AX Database (Read-Only)
# ==============================================
AX_DB_DRIVER=ODBC Driver 17 for SQL Server
AX_DB_SERVER=sqldev.corp.local\AX2012
AX_DB_NAME=AX2012R3_DEV
AX_DB_USER=ax_monitor_ro
AX_DB_PASSWORD=***CHANGEME***
AX_DB_TIMEOUT=30

# ==============================================
# Staging Database (Read/Write)
# ==============================================
STAGING_DB_DRIVER=ODBC Driver 17 for SQL Server
STAGING_DB_SERVER=sqldev.corp.local\AX2012
STAGING_DB_NAME=AXMonitoring_DEV
STAGING_DB_USER=ax_monitor_rw
STAGING_DB_PASSWORD=***CHANGEME***
STAGING_DB_TIMEOUT=30

# ==============================================
# SMTP Configuration
# ==============================================
SMTP_HOST=smtp.corp.local
SMTP_PORT=25
SMTP_FROM=axmonitor-dev@corp.local
SMTP_USE_TLS=false
# SMTP_USERNAME=  # Optional, if auth required
# SMTP_PASSWORD=  # Optional, if auth required

# ==============================================
# Alert Recipients (comma-separated)
# ==============================================
ALERT_RECIPIENTS=dev-team@corp.local,ops-team@corp.local

# ==============================================
# Scheduler Intervals (seconds)
# ==============================================
SCHEDULER_BATCH_INTERVAL=120
SCHEDULER_SESSION_INTERVAL=30
SCHEDULER_HEALTH_INTERVAL=300
SCHEDULER_DEADLOCK_INTERVAL=120

# ==============================================
# Cache Configuration
# ==============================================
CACHE_TTL_OVERVIEW=30
CACHE_TTL_BATCH=60
CACHE_TTL_SESSIONS=30
CACHE_TTL_SQL_HEALTH=120
CACHE_MAX_SIZE=100

# ==============================================
# Logging
# ==============================================
LOG_LEVEL=INFO

# ==============================================
# Feature Toggles (optional)
# ==============================================
# FEATURE_ANOMALY_DETECTION=false
# FEATURE_TEAMS_ALERTS=false
```

---

## Definition of Done

- [ ] All acceptance criteria met
- [ ] Configuration loads successfully from `.env.dev`
- [ ] Missing env var raises clear ValueError
- [ ] Secrets redacted in logs
- [ ] Health check function implemented and tested
- [ ] Unit tests for config loading (≥80% coverage)
- [ ] Documentation updated in README.md
- [ ] Code passes lint, format, typecheck

---

## Tasks Breakdown

### Task 1: Create Configuration Module (2 hours)
- Implement `app/config.py` with dataclasses
- Implement `load_config()` function
- Implement `get_connection_string()` helper

### Task 2: Create .env Templates (30 min)
- Create `.env.example` with all variables documented
- Add `.env*` to `.gitignore` (except `.example`)

### Task 3: Validation & Error Handling (1 hour)
- Validate required variables
- Validate environment name
- Log loaded config (secrets redacted)

### Task 4: Health Check Function (1 hour)
- Implement database connectivity tests
- Implement SMTP connectivity test (optional)
- Return structured results

### Task 5: Unit Tests (1.5 hours)
- Test successful config load
- Test missing env var raises error
- Test invalid environment name
- Test secrets redaction in logs

### Task 6: Documentation (30 min)
- Update README.md with configuration section
- Document environment variable requirements
- Document secret management best practices

---

## Testing Strategy

### Unit Tests (`tests/unit/test_config.py`)

```python
import os
import pytest
from app.config import load_config, AppConfig

def test_load_config_success(monkeypatch):
    """Test successful configuration loading."""
    # Mock environment variables
    monkeypatch.setenv('APP_ENV', 'DEV')
    monkeypatch.setenv('AX_DB_DRIVER', 'ODBC Driver 17 for SQL Server')
    monkeypatch.setenv('AX_DB_SERVER', 'localhost')
    # ... (mock all required vars)
    
    config = load_config()
    
    assert isinstance(config, AppConfig)
    assert config.environment == 'DEV'
    assert config.ax_db.server == 'localhost'

def test_load_config_missing_required_var(monkeypatch):
    """Test error when required env var missing."""
    monkeypatch.setenv('APP_ENV', 'DEV')
    # Don't set AX_DB_SERVER
    
    with pytest.raises(ValueError, match="Required environment variable"):
        load_config()

def test_load_config_invalid_environment(monkeypatch):
    """Test error for invalid APP_ENV."""
    monkeypatch.setenv('APP_ENV', 'INVALID')
    
    with pytest.raises(ValueError, match="Invalid APP_ENV"):
        load_config()
```

### Manual Testing
- [ ] Create `.env.dev` with valid settings
- [ ] Run `python -c "from app.config import load_config; config = load_config(); print(config.environment)"`
- [ ] Verify config loads and logs environment (not passwords)
- [ ] Test with missing required var, verify clear error
- [ ] Test health check function

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Secrets committed to Git | Critical | `.gitignore` enforcement, pre-commit hook for secret scanning |
| Wrong environment loaded | High | Explicit APP_ENV required, log loaded environment prominently |
| ODBC driver name mismatch | Medium | Document exact driver name, provide error message with examples |
| Connection string format errors | Medium | Test connection strings in unit tests |

---

## Dependencies

**Upstream:** Epic 0 (Setup) - requires project structure and dependencies

**Downstream:** 
- Epic 2 (DB Access) - uses DatabaseConfig
- All subsequent epics - require configuration

---

## Related Documents

- `docs/deploy-runbook.md` - Configuration in production
- `README.md` - Setup instructions

---

## Notes

- Configuration system must be finalized before any database work
- Secret management strategy critical for production deployment
- Health check function reused in Admin page (Epic 7)
- Feature toggles framework ready for future use

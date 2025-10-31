# Coding Standards
## AX 2012 R3 Performance Leak Monitor

**Last Updated:** 2025-10-23

---

## General Principles

### Code Philosophy

**Readability > Cleverness**
- Code is read 10x more than written
- Favor explicit over implicit
- Use descriptive names over comments

**SOLID Principles**
- Single Responsibility
- Open/Closed
- Liskov Substitution
- Interface Segregation
- Dependency Inversion

**DRY (Don't Repeat Yourself)**
- Extract common logic into functions
- Use inheritance/composition appropriately
- Create reusable utilities

**YAGNI (You Aren't Gonna Need It)**
- Implement for current requirements
- Avoid speculative features
- Refactor when patterns emerge

---

## Python Style Guide

### Base Standard: PEP 8

Follow [PEP 8](https://peps.python.org/pep-0008/) with the following specifics:

### Code Formatting (Enforced by Black)

**Line Length:** 100 characters (more readable on modern displays)

**Indentation:** 4 spaces (no tabs)

**Quotes:** Double quotes `"` preferred, single `'` acceptable (Black will normalize)

**Imports:**
```python
# Standard library
import os
import sys
from datetime import datetime, timedelta

# Third-party
import pandas as pd
import streamlit as st
from apscheduler.schedulers.background import BackgroundScheduler

# Local application
from app.services.batch_service import get_batch_backlog
from app.db.connection_manager import get_connection
```

**Ordering:**
1. Standard library imports
2. Third-party library imports
3. Local application imports
4. Alphabetically within each group

---

## Naming Conventions

### Variables & Functions

**Functions/Methods:** `snake_case`
```python
def calculate_batch_backlog(environment: str) -> int:
    pass
```

**Variables:** `snake_case`
```python
batch_count = 42
user_session_id = "ABC123"
```

**Constants:** `UPPER_SNAKE_CASE`
```python
MAX_RETRY_ATTEMPTS = 3
DEFAULT_TIMEOUT_SECONDS = 30
CACHE_TTL_BATCH = 60
```

**Private Functions/Variables:** Leading underscore `_name`
```python
def _internal_helper():
    pass

_cached_connection = None
```

### Classes

**Classes:** `PascalCase`
```python
class BatchService:
    pass

class AlertRuleEngine:
    pass
```

**Exception Classes:** Suffix with `Error`
```python
class DatabaseConnectionError(Exception):
    pass

class InvalidConfigurationError(Exception):
    pass
```

### Files & Modules

**Modules:** `snake_case.py`
```
batch_service.py
connection_manager.py
alert_rule_engine.py
```

**Test Files:** `test_*.py`
```
test_batch_service.py
test_connection_manager.py
```

---

## Type Hints (Mandatory)

### Function Signatures

**Always include type hints** for function parameters and return types:

```python
def get_batch_backlog(
    environment: str,
    time_range: str,
    aos_filter: Optional[str] = None
) -> pd.DataFrame:
    """
    Retrieve batch job backlog for the specified environment.
    
    Args:
        environment: Environment name (DEV, TST, PRD)
        time_range: Time range filter (24h, 7d, 30d)
        aos_filter: Optional AOS instance filter
        
    Returns:
        DataFrame with columns: batch_class, backlog_count, avg_duration
        
    Raises:
        DatabaseConnectionError: If database is unreachable
    """
    pass
```

**Use typing module for complex types:**
```python
from typing import Dict, List, Optional, Tuple, Union

def process_results(
    data: List[Dict[str, Union[int, str]]]
) -> Tuple[bool, Optional[str]]:
    pass
```

**Type aliases for clarity:**
```python
from typing import TypeAlias

AlertRule: TypeAlias = Dict[str, Union[str, int, float, bool]]
SessionSnapshot: TypeAlias = pd.DataFrame
```

---

## Documentation Standards

### Docstrings

**Use Google-style docstrings** (readable in IDE tooltips):

```python
def send_alert_email(
    recipient: str,
    subject: str,
    body: str,
    alert_id: int
) -> bool:
    """
    Send an alert notification via SMTP.
    
    This function formats an HTML email with the alert details and
    includes a deep link to the dashboard for investigation.
    
    Args:
        recipient: Email address of the recipient
        subject: Email subject line
        body: HTML body content
        alert_id: Unique alert identifier for tracking
        
    Returns:
        True if email sent successfully, False otherwise
        
    Raises:
        SMTPException: If SMTP server is unreachable
        
    Example:
        >>> send_alert_email(
        ...     "ops@corp.local",
        ...     "Batch Backlog High",
        ...     "<p>Backlog count: 42</p>",
        ...     12345
        ... )
        True
    """
    pass
```

**Module-level docstrings:**
```python
"""
Batch monitoring service module.

This module provides functions for querying and analyzing AX batch job
performance, including backlog calculation, execution time trends, and
error rate analysis.

Typical usage:
    from app.services.batch_service import get_batch_backlog
    
    backlog = get_batch_backlog("PRD", "24h")
    print(backlog.head())
"""
```

### Comments

**When to comment:**
- Why, not what (code should explain what)
- Complex algorithms or business logic
- Workarounds for known issues
- TODO/FIXME for future improvements

**Avoid:**
- Obvious comments (e.g., `# Increment counter`)
- Commented-out code (use Git instead)

**Examples:**
```python
# Good: Explains why
# Use NOLOCK hint to avoid blocking AX production queries
query = "SELECT * FROM BATCHJOB WITH (NOLOCK)"

# Bad: States the obvious
# Loop through batch jobs
for job in batch_jobs:
    pass

# Good: TODO with context
# TODO(username): Implement baseline calculation once 14 days of data available
baseline = None
```

---

## Code Organization

### Project Structure

```
app/
├── __init__.py
├── main.py                 # Streamlit entry point
├── config.py               # Configuration loader
├── pages/                  # Streamlit pages
│   ├── 1_overview.py
│   ├── 2_batch.py
│   └── ...
├── services/               # Business logic
│   ├── __init__.py
│   ├── batch_service.py
│   ├── session_service.py
│   └── ...
├── db/                     # Data access layer
│   ├── __init__.py
│   ├── connection_manager.py
│   ├── ax_queries.py
│   └── ...
├── scheduler/              # Background jobs
│   ├── __init__.py
│   ├── jobs.py
│   └── scheduler_manager.py
├── alerts/                 # Alerting system
│   ├── __init__.py
│   ├── rule_engine.py
│   └── email_sender.py
├── ui/                     # Reusable UI components
│   ├── __init__.py
│   ├── filters.py
│   ├── kpi_tiles.py
│   └── charts.py
└── utils/                  # Shared utilities
    ├── __init__.py
    ├── logger.py
    └── cache.py
```

### File Size Limits

- **Maximum file length:** 500 lines
- **If exceeded:** Split into multiple modules with clear responsibilities

### Function Length

- **Target:** 20-30 lines per function
- **Maximum:** 50 lines (excluding docstring)
- **If exceeded:** Extract helper functions

---

## Error Handling

### Exception Handling Best Practices

**Be specific with exceptions:**
```python
# Good
try:
    connection = get_connection()
except DatabaseConnectionError as e:
    log.error("database_connection_failed", error=str(e))
    return None

# Bad
try:
    connection = get_connection()
except Exception:  # Too broad
    pass
```

**Don't suppress errors silently:**
```python
# Bad
try:
    risky_operation()
except:
    pass  # Error swallowed

# Good
try:
    risky_operation()
except SpecificError as e:
    log.warning("operation_failed", error=str(e))
    # Graceful fallback or re-raise
```

**Use custom exceptions for domain errors:**
```python
class AlertThrottledError(Exception):
    """Raised when alert is suppressed due to throttling."""
    pass

class DataValidationError(Exception):
    """Raised when data fails validation rules."""
    pass
```

**Logging in exception handlers:**
```python
try:
    result = expensive_query()
except DatabaseConnectionError as e:
    log.error(
        "query_failed",
        query_name="batch_backlog",
        environment=env,
        error=str(e),
        exc_info=True  # Include stack trace
    )
    raise
```

---

## Database Query Standards

### Parameterized Queries (Mandatory)

**Always use parameterized queries** to prevent SQL injection:

```python
# Good
query = "SELECT * FROM BATCHJOB WHERE STATUS = ? AND CREATEDDATETIME > ?"
cursor.execute(query, (status, cutoff_date))

# Bad - SQL INJECTION RISK
query = f"SELECT * FROM BATCHJOB WHERE STATUS = '{status}'"
cursor.execute(query)
```

### Query Optimization

**Use TOP to limit results:**
```python
query = """
    SELECT TOP 1000 
        RECID, CAPTION, STATUS, CREATEDDATETIME
    FROM BATCHJOB
    WHERE STATUS IN (1, 3, 4)
    ORDER BY CREATEDDATETIME DESC
"""
```

**Filter by time range:**
```python
query = """
    SELECT *
    FROM SYSCLIENTSESSIONS
    WHERE LOGINDATETIME > DATEADD(hour, -2, GETDATE())
"""
```

**Use NOLOCK for read-only queries:**
```python
query = """
    SELECT *
    FROM BATCHJOB WITH (NOLOCK)
    WHERE STATUS = ?
"""
```

**Document expected execution time:**
```python
def get_top_expensive_queries() -> pd.DataFrame:
    """
    Retrieve top 50 expensive queries from DMV.
    
    Expected execution time: 2-5 seconds
    Data freshness: Real-time
    """
    query = """
        SELECT TOP 50 ...
        FROM sys.dm_exec_query_stats
    """
    return pd.read_sql(query, connection)
```

---

## Logging Standards

### Structured Logging

**Use structlog with context:**
```python
import structlog

log = structlog.get_logger()

# Good: Structured fields
log.info(
    "batch_job_collected",
    job_id=12345,
    duration_ms=450,
    status="completed",
    environment="PRD"
)

# Bad: Unstructured string
log.info(f"Collected job 12345 in 450ms (completed) for PRD")
```

### Log Levels

**DEBUG:** Detailed diagnostic information
```python
log.debug("query_executed", query=query, params=params, rows_returned=len(results))
```

**INFO:** Normal operational events
```python
log.info("scheduler_job_started", job_name="collect_batch", environment="PRD")
```

**WARNING:** Unexpected but handled situations
```python
log.warning("query_slow", query_name="batch_backlog", duration_ms=3500, threshold_ms=2000)
```

**ERROR:** Error events that need attention
```python
log.error("database_connection_failed", server=server, database=db, error=str(e))
```

**CRITICAL:** System failure, service down
```python
log.critical("scheduler_stopped", reason="unhandled_exception", exc_info=True)
```

### Sensitive Data

**Never log:**
- Passwords or connection strings
- API keys or tokens
- Sensitive business data (unless explicitly approved)

**Redact if needed:**
```python
log.info(
    "database_connected",
    server=server,
    user=user,
    password="***REDACTED***"  # Don't log actual password
)
```

---

## Testing Standards

### Test Coverage

**Minimum coverage:** 80% for service layer

**Coverage command:**
```bash
pytest --cov=app --cov-report=html --cov-report=term
```

### Test Structure

**Use AAA pattern (Arrange-Act-Assert):**
```python
def test_calculate_batch_backlog():
    # Arrange
    mock_data = pd.DataFrame({
        'batch_class': ['ClassA', 'ClassB'],
        'status': [1, 1],
        'count': [10, 5]
    })
    
    # Act
    result = calculate_backlog(mock_data)
    
    # Assert
    assert result == 15
    assert isinstance(result, int)
```

### Test Naming

**Pattern:** `test_<function>_<scenario>_<expected_result>`

```python
def test_get_batch_backlog_with_valid_environment_returns_dataframe():
    pass

def test_send_alert_email_with_invalid_recipient_raises_error():
    pass

def test_calculate_p95_with_empty_data_returns_none():
    pass
```

### Fixtures

**Use pytest fixtures for setup:**
```python
import pytest
from app.db.connection_manager import ConnectionManager

@pytest.fixture
def mock_connection():
    """Provide a mock database connection."""
    conn = MagicMock()
    conn.cursor.return_value.fetchall.return_value = []
    return conn

@pytest.fixture
def sample_batch_data():
    """Provide sample batch job data for testing."""
    return pd.DataFrame({
        'job_id': [1, 2, 3],
        'status': [1, 2, 4],
        'duration': [100, 200, 300]
    })

def test_process_batch_data(sample_batch_data):
    result = process_data(sample_batch_data)
    assert len(result) == 3
```

### Mocking

**Mock external dependencies:**
```python
from unittest.mock import patch, MagicMock

@patch('app.db.connection_manager.pyodbc.connect')
def test_get_connection_success(mock_connect):
    # Arrange
    mock_connect.return_value = MagicMock()
    
    # Act
    conn = get_connection()
    
    # Assert
    assert conn is not None
    mock_connect.assert_called_once()
```

---

## Performance Standards

### Caching

**Cache expensive operations:**
```python
from functools import lru_cache
from cachetools import TTLCache, cached

# TTL cache for time-sensitive data
cache = TTLCache(maxsize=100, ttl=60)

@cached(cache)
def get_batch_backlog(environment: str, time_range: str) -> pd.DataFrame:
    # Expensive database query
    return query_result
```

**Cache key best practices:**
- Include all filter parameters
- Use immutable types (strings, tuples)
- Document cache duration

### Query Optimization

**Measure query performance:**
```python
import time

start = time.time()
result = cursor.execute(query).fetchall()
duration_ms = (time.time() - start) * 1000

log.info("query_executed", query_name="batch_backlog", duration_ms=duration_ms)

if duration_ms > 2000:  # 2 second threshold
    log.warning("query_slow", query_name="batch_backlog", duration_ms=duration_ms)
```

### Data Processing

**Use Pandas efficiently:**
```python
# Good: Vectorized operations
df['duration_minutes'] = df['duration_seconds'] / 60

# Bad: Loop over rows
for index, row in df.iterrows():
    df.at[index, 'duration_minutes'] = row['duration_seconds'] / 60
```

**Limit DataFrame size:**
```python
# Filter early
df_recent = df[df['timestamp'] > cutoff_date]

# Use chunking for large data
for chunk in pd.read_sql(query, connection, chunksize=10000):
    process_chunk(chunk)
```

---

## Security Standards

### Input Validation

**Validate all user inputs:**
```python
def get_batch_data(environment: str, time_range: str) -> pd.DataFrame:
    # Validate environment
    valid_environments = ['DEV', 'TST', 'PRD']
    if environment not in valid_environments:
        raise ValueError(f"Invalid environment: {environment}")
    
    # Validate time range
    valid_ranges = ['1h', '24h', '7d', '30d']
    if time_range not in valid_ranges:
        raise ValueError(f"Invalid time range: {time_range}")
    
    # Proceed with query
    pass
```

### Secrets Management

**Never hardcode secrets:**
```python
# Bad
DB_PASSWORD = "MyPassword123"

# Good
import os
DB_PASSWORD = os.getenv('DB_PASSWORD')
if not DB_PASSWORD:
    raise ValueError("DB_PASSWORD environment variable not set")
```

### SQL Injection Prevention

**Always use parameterized queries** (see Database Query Standards)

---

## Git Commit Standards

### Commit Message Format

**Use Conventional Commits:**
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code formatting (no logic change)
- `refactor`: Code restructuring (no behavior change)
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples:**
```
feat(batch): add P95 calculation for execution times

Implements percentile calculation using numpy.percentile for batch
job duration analysis. Adds caching with 60-second TTL.

Closes #42
```

```
fix(alerts): prevent duplicate alert emails

Added deduplication logic using alert_suppression table.
Suppression window: 30 minutes per alert rule + entity.

Fixes #78
```

---

## Code Review Checklist

Before submitting a pull request, verify:

- [ ] Code follows PEP 8 and Black formatting
- [ ] All functions have type hints and docstrings
- [ ] No hardcoded secrets or credentials
- [ ] All SQL queries are parameterized
- [ ] Error handling is appropriate (no silent failures)
- [ ] Logging uses structured format with context
- [ ] Unit tests added/updated (≥80% coverage)
- [ ] Manual testing completed in DEV environment
- [ ] No commented-out code (use Git history)
- [ ] Performance considerations documented
- [ ] Security implications reviewed

---

## IDE Configuration

### VS Code Settings (.vscode/settings.json)

```json
{
    "python.linting.enabled": true,
    "python.linting.ruffEnabled": true,
    "python.formatting.provider": "black",
    "python.formatting.blackArgs": ["--line-length=100"],
    "python.analysis.typeCheckingMode": "basic",
    "editor.formatOnSave": true,
    "editor.codeActionsOnSave": {
        "source.organizeImports": true
    }
}
```

### Pre-commit Configuration (.pre-commit-config.yaml)

```yaml
repos:
  - repo: https://github.com/astral-sh/ruff-pre-commit
    rev: v0.1.6
    hooks:
      - id: ruff
        args: [--fix, --exit-non-zero-on-fix]
  - repo: https://github.com/psf/black
    rev: 23.11.0
    hooks:
      - id: black
        args: [--line-length=100]
  - repo: https://github.com/pre-commit/mirrors-mypy
    rev: v1.7.1
    hooks:
      - id: mypy
        additional_dependencies: [types-all]
```

---

## References

- [PEP 8 – Style Guide for Python Code](https://peps.python.org/pep-0008/)
- [PEP 484 – Type Hints](https://peps.python.org/pep-0484/)
- [Google Python Style Guide](https://google.github.io/styleguide/pyguide.html)
- [Conventional Commits](https://www.conventionalcommits.org/)

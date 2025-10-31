# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**AX 2012 R3 Performance Leak Monitor** - A Streamlit-based real-time monitoring and alerting dashboard for Microsoft Dynamics AX 2012 R3 systems. This application monitors batch jobs, user sessions, SQL blocking chains, and database health to generate proactive alerts before users are impacted.

**Critical Architecture Principle:** This system operates in **READ-ONLY mode** against the AX production database. All write operations use file-based storage. The `AX_DB_READ_ONLY=true` flag must NEVER be disabled.

## Essential Commands

### Development Setup
```bash
# Create and activate virtual environment
python -m venv .venv
.venv\Scripts\Activate.ps1  # Windows
source .venv/bin/activate    # Linux/Mac

# Install dependencies
pip install -r requirements.txt

# Install development dependencies
make install-dev
# or manually:
pip install pytest pytest-cov pytest-mock black isort flake8 mypy pre-commit
```

### Running the Application
```bash
# Start the Streamlit app
streamlit run app/app.py
# or
make run

# Development mode with auto-reload
streamlit run app/app.py --server.runOnSave true
# or
make run-dev
```

### Testing
```bash
# Run all tests with coverage
pytest tests/ -v --cov=app --cov-report=html --cov-report=term
# or
make test

# Run unit tests only
pytest tests/ -v -m "unit"
# or
make test-unit

# Run integration tests only
pytest tests/ -v -m "integration"
# or
make test-integration

# Run specific test file
pytest tests/test_query_validator.py -v

# Open coverage report
start htmlcov\index.html  # Windows
open htmlcov/index.html   # Mac
```

### Code Quality
```bash
# Run all quality checks
make lint

# Format code
black app tests
isort app tests
# or
make format

# Check formatting without modifying
black --check app tests
isort --check-only app tests
# or
make format-check

# Type checking
mypy app --ignore-missing-imports

# Security checks
make security-check
```

### Environment Configuration
```bash
# Set environment (DEV/TST/PRD)
$env:APP_ENV="DEV"    # Uses .env.dev
$env:APP_ENV="TST"    # Uses .env.tst
$env:APP_ENV="PRD"    # Uses .env.prd

# Copy and configure environment file
cp .env.example .env.dev
# Edit .env.dev with your credentials
```

## Architecture Deep-Dive

### Critical Read-Only Architecture

**The system has been migrated from database-based to file-based storage to guarantee read-only access to AX database:**

```
AX 2012 DB (READ-ONLY) → Query Validator → App → File Storage (data/)
                                                      ├── history/    (CSV)
                                                      ├── alerts/     (JSON)
                                                      ├── audit/      (JSONL)
                                                      └── analytics/  (JSON)
```

**Query Validation Layer (`app/db/query_validator.py`):**
- ALL queries to AX database are validated before execution
- Blocks: INSERT, UPDATE, DELETE, MERGE, TRUNCATE, CREATE, DROP, ALTER, EXEC, SELECT INTO
- Only SELECT statements are allowed
- Validation is case-insensitive and removes SQL comments

**AX Connector (`app/db/ax_connector.py`):**
- Enforces read-only mode via `AX_DB_READ_ONLY` environment variable
- All queries pass through `execute_query()` which validates SQL
- Connection pooling and retry logic with exponential backoff

### File-Based Storage Architecture

**Storage Layer (`app/storage/file_storage.py`):**
- **JSONStorage** - Structured config data (alerts rules, dashboard configs)
- **CSVStorage** - Time-series metrics (batch history, sessions, SQL health)
- **JSONLStorage** - Streaming event logs (audit events, alert history)
- Atomic writes with temp file + rename pattern
- Automatic backups before every write operation
- Monthly file rotation for time-series data

**History Storage (`app/db/history_storage_csv.py`):**
- Replaces database staging tables with CSV files
- Files organized by month: `metrics_YYYY-MM.csv`, `batch_jobs_YYYY-MM.csv`, etc.
- Query methods aggregate across multiple monthly files
- Configurable retention (12 months for historical, 30 days for detailed)

**Alerts Engine (`app/alerts/rules_engine_json.py`):**
- Replaces database-backed alerts with JSON/JSONL files
- `rules.json` - Alert rule definitions
- `active_alerts.json` - Currently active alerts
- `history_YYYY-MM.jsonl` - Alert history (JSON Lines format)

### Component Layers

**1. Presentation Layer (`app/pages/`)**
- Multi-page Streamlit dashboard
- Pages: Overview, Batch, Sessions, Blocking, SQL Health, Alerts, Admin, Business Intelligence, ML Predictions, AI Assistant
- Custom UI components in `app/components/` (navigation, theme, charts, AI chat)

**2. Service Layer (`app/services/` implied structure)**
- Business logic and data aggregations
- Caching layer with TTL (`cachetools`)
- Performance targets: p95 < 3s for page loads

**3. Data Collection Layer (APScheduler)**
- Background jobs collect metrics at intervals:
  - Sessions/Blocking: 30-60s
  - Batch status: 60-120s
  - SQL health: 1-5min
  - Deadlocks: 2min
- Jobs write to file storage, not database

**4. Data Access Layer (`app/db/`)**
- `ax_connector.py` - AX database connection with read-only enforcement
- `query_validator.py` - SQL validation before execution
- `history_storage_csv.py` - CSV-based metrics storage
- All write operations go to files, not database

**5. Analytics & ML Layer**
- `app/ml/` - Anomaly detection and predictive models
- `app/analytics/` - Trend analysis and RCA engine
- `app/bi/` - Business KPIs and dashboard builder

### Special Page: Auftragsfreigabe (11_Auftragsfreigabe.py)

This is a comprehensive batch job release management page for AX 2012 R3. Key characteristics:
- **Large file (~65KB)** - Contains extensive business logic
- **Helper modules:**
  - `helper_functions_auftragsfreigabe.py` - Core helper functions
  - `advanced_features_helpers.py` - Advanced analytics helpers
- **Merge scripts:** Three Python scripts in root directory help inject features:
  - `insert_helpers.py` - Inserts basic helpers into main page
  - `insert_features.py` - Inserts feature sections
  - `merge_all_features.py` - Orchestrates complete merge

When working on this page, use the helper modules and merge scripts rather than editing the monolithic file directly.

## Configuration Management

### Environment Variables Structure

The system uses environment-specific `.env` files:
- `.env.dev` - Development database credentials
- `.env.tst` - Test environment credentials
- `.env.prd` - Production credentials
- `.env.storage` - Storage configuration template

**Critical Variables:**
```bash
# Database connections
AX_DB_SERVER=sqlserver\instance
AX_DB_NAME=AX2012R3_PROD
AX_DB_USER=ax_monitor_ro  # READ-ONLY user required
AX_DB_PASSWORD=***

# CRITICAL - Never disable!
AX_DB_READ_ONLY=true

# Storage configuration
STORAGE_TYPE=file
DATA_DIR=./data
HISTORY_RETENTION_MONTHS=12
ALERT_HISTORY_MONTHS=6
BACKUP_RETENTION_DAYS=7

# SMTP for alerts
SMTP_HOST=smtp.local
SMTP_PORT=25
SMTP_FROM=ax-monitor@corp.local
ALERT_RECIPIENTS=ops-team@corp.local

# Performance tuning
ENABLE_FILE_LOCKING=true
FILE_CACHE_TTL=60
CSV_BATCH_SIZE=1000
```

### Switching Environments
```bash
# Windows PowerShell
$env:APP_ENV="DEV"
streamlit run app/app.py

# Linux/Mac
export APP_ENV="TST"
streamlit run app/app.py
```

## Data Migration

If migrating from an existing database-based system:

```bash
# Run migration script
python scripts/migrate_db_to_files.py --db-path ./staging.db --output-dir ./data

# Verify migration
python -c "from app.db.history_storage_csv import history_storage; print(history_storage.get_storage_stats())"
```

See `STORAGE_MIGRATION.md` for detailed migration procedures.

## Development Workflow

### Adding New Features

1. **For data collection:** Add methods to `app/db/ax_connector.py` or service layer
2. **For UI pages:** Create new page in `app/pages/` following Streamlit conventions
3. **For alerts:** Add rules via Admin UI or directly to `data/alerts/rules.json`
4. **Always validate:** Queries must pass through `query_validator` before execution

### Testing New Queries

```python
# Test query validation
from app.db.query_validator import query_validator

query = "SELECT * FROM BATCHJOB WHERE STATUS = 1"
is_valid, error = query_validator.validate(query)
if not is_valid:
    print(f"Query blocked: {error}")

# Test query execution
from app.db.ax_connector import AXConnector

connector = AXConnector(read_only=True)
df = connector.execute_query(query)
print(df.head())
```

### Adding Alert Rules

Alert rules are JSON-based. Example rule structure:
```json
{
  "name": "High Batch Backlog",
  "type": "threshold",
  "metric": "batch_backlog",
  "threshold": 100,
  "severity": "warning",
  "enabled": true
}
```

Edit `data/alerts/rules.json` or use Admin UI (page 7_Admin.py).

## Common Development Tasks

### Adding a New Dashboard Page

1. Create `app/pages/N_PageName.py` (N = sequence number)
2. Import required components:
   ```python
   import streamlit as st
   from components.ui_theme import setup_page_config, create_page_header
   from components.navigation import Navigation
   from data_service import data_service
   ```
3. Follow existing page structure (see `1_Overview.py` as template)
4. Add navigation entry in `app/components/navigation.py`

### Querying AX Data Safely

```python
from app.db.ax_connector import AXConnector

# Always use context manager
connector = AXConnector(read_only=True)
with connector.get_connection() as conn:
    df = pd.read_sql_query("""
        SELECT RECID, CAPTION, STATUS
        FROM BATCHJOB
        WHERE STATUS IN (1, 3, 4)
        ORDER BY CREATEDDATETIME DESC
    """, conn)
```

### Storing Metrics

```python
from app.db.history_storage_csv import history_storage
import pandas as pd

# Store metrics
metrics_df = pd.DataFrame({
    'timestamp': [datetime.now()],
    'metric_name': ['batch_backlog'],
    'value': [42]
})
history_storage.store_metrics(metrics_df)

# Query metrics
df = history_storage.get_metrics_history(
    metric_name='batch_backlog',
    start_date=datetime.now() - timedelta(days=7)
)
```

## Performance Targets (p95)

- Dashboard page load: < 3s
- Data freshness: < 60s
- Ingestion job latency: < 10s
- Query validation overhead: < 5ms
- Cache hit rate: > 70%
- Service error rate: < 0.1%

## Troubleshooting

### Query Blocked by Validator

**Error:** "Query contains write operations"
**Solution:** Ensure query is SELECT-only. Check for hidden keywords in subqueries or CTEs.

```python
# Blocked
query = "INSERT INTO TempTable SELECT * FROM BATCHJOB"  # ❌

# Allowed
query = "SELECT * FROM BATCHJOB WHERE STATUS = 1"  # ✅
```

### File Storage Issues

**Error:** "Permission denied" on data files
**Solution:** Check file system permissions and ensure `data/` directory is writable.

```bash
# Windows
icacls data /grant Users:(OI)(CI)F

# Linux
chmod -R 755 data/
```

**Error:** "Storage directory not found"
**Solution:** Initialize storage directories:

```python
from app.storage.file_storage import FileStorage
storage = FileStorage("./data")
# Directories auto-created on first use
```

### View Logs

```bash
# Application logs
tail -f logs/app.log

# Storage operation logs
tail -f logs/storage.log

# Query validation logs (check for blocked queries)
grep "BLOCKED" logs/app.log
```

### Database Connection Issues

1. Verify ODBC Driver 17/18 is installed
2. Test connection string manually
3. Check `AX_DB_READ_ONLY=true` is set
4. Verify read-only database user has SELECT permissions

## Important Files & Locations

### Documentation
- `README.md` - Main project documentation
- `DEV_TODOS.md` - Detailed development checklist (German)
- `STORAGE_MIGRATION.md` - Migration from DB to file storage
- `IMPLEMENTATION_SUMMARY.md` - Summary of read-only implementation
- `docs/architecture.md` - Full architecture document
- `docs/prd.md` - Product requirements document
- `docs/stories/` - Epic definitions and user stories

### Configuration
- `.env.example` - Template for environment files
- `.env.storage` - Storage configuration template
- `pyproject.toml` - Project metadata and tool configs
- `pytest.ini` - Test configuration
- `Makefile` - Development commands

### Key Modules
- `app/app.py` - Main application entry point
- `app/db/ax_connector.py` - Database connector with read-only enforcement
- `app/db/query_validator.py` - SQL validation layer (CRITICAL)
- `app/storage/file_storage.py` - File storage abstraction
- `app/db/history_storage_csv.py` - CSV-based metrics storage
- `app/alerts/rules_engine_json.py` - JSON-based alert engine
- `app/data_service.py` - Mock data service (for development)
- `app/components/navigation.py` - Multi-page navigation
- `app/components/ui_theme.py` - UI theming and styling

### Helper Scripts
- `insert_helpers.py` - Inject helper functions into Auftragsfreigabe page
- `insert_features.py` - Inject feature sections into Auftragsfreigabe page
- `merge_all_features.py` - Complete merge orchestration for Auftragsfreigabe page
- `scripts/migrate_db_to_files.py` - Database to file storage migration

## Security & Compliance

### Read-Only Enforcement

**CRITICAL:** The system has multiple layers of read-only protection:
1. Environment variable: `AX_DB_READ_ONLY=true` (checked at connector init)
2. Query validation: All SQL validated before execution
3. Database user: Should have SELECT-only permissions
4. File-based writes: All mutations go to local files, never to database

### Never Do This

```python
# ❌ NEVER disable read-only mode
connector = AXConnector(read_only=False)

# ❌ NEVER bypass query validation
conn = pyodbc.connect(connection_string)
cursor = conn.cursor()
cursor.execute("INSERT INTO ...")  # Unvalidated!

# ❌ NEVER set this in production
os.environ['AX_DB_READ_ONLY'] = 'false'
```

### Always Do This

```python
# ✅ Always use connector with validation
connector = AXConnector(read_only=True)
df = connector.execute_query("SELECT * FROM BATCHJOB")

# ✅ Store data in files, not database
from app.db.history_storage_csv import history_storage
history_storage.store_metrics(df)

# ✅ Verify read-only mode is active
assert connector.read_only == True
```

## Testing Strategy

### Test Organization
- `tests/test_query_validator.py` - Query validation tests (24 test cases)
- `tests/fixtures/` - Test data and fixtures
- Target: ≥80% coverage for core logic

### Running Specific Tests

```bash
# Query validator tests (most critical)
pytest tests/test_query_validator.py -v

# Future integration tests
pytest tests/ -m integration -v

# Skip slow tests
pytest tests/ -m "not slow"
```

### Writing New Tests

```python
# Mark test type
@pytest.mark.unit
def test_query_validation():
    from app.db.query_validator import query_validator

    # Test blocking write operations
    is_valid, error = query_validator.validate("INSERT INTO table ...")
    assert not is_valid
    assert "write operations" in error.lower()

    # Test allowing SELECT
    is_valid, error = query_validator.validate("SELECT * FROM BATCHJOB")
    assert is_valid
    assert error is None
```

## Deployment

### Windows Service Deployment

```powershell
# Install NSSM
choco install nssm

# Configure service
nssm install AXMonitor "C:\apps\ax-monitor\.venv\Scripts\streamlit.exe"
nssm set AXMonitor AppParameters "run app\app.py --server.port 8501"
nssm set AXMonitor AppDirectory "C:\apps\ax-monitor"
nssm set AXMonitor AppEnvironmentExtra "APP_ENV=PRD"

# Start service
nssm start AXMonitor

# Check status
nssm status AXMonitor
```

See `docs/deploy-runbook.md` for complete deployment procedures.

## Key Architectural Decisions

1. **File-based storage over database** - Eliminates risk of accidental writes to production AX database
2. **Query validation layer** - All SQL validated before execution, blocks any write operations
3. **CSV for time-series, JSON for config** - Optimal format for each data type
4. **Monthly file rotation** - Balances query performance with file management
5. **Atomic writes with backups** - Data safety without database transactions
6. **No staging database required** - Simpler deployment, fewer dependencies

## References

- AX 2012 R3 Documentation: Tables (BATCHJOB, BATCH, SYSCLIENTSESSIONS)
- SQL Server 2016 DMVs: sys.dm_exec_requests, sys.dm_os_waiting_tasks, sys.dm_exec_query_stats
- Streamlit docs: https://docs.streamlit.io/
- APScheduler: https://apscheduler.readthedocs.io/

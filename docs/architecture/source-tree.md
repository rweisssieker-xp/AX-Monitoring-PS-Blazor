# Source Tree Structure
## AX 2012 R3 Performance Leak Monitor

**Last Updated:** 2025-10-23

---

## Overview

This document describes the complete directory structure and the purpose of each component in the AXMonitoringBU project.

---

## Root Directory

```
AXMonitoringBU/
├── .bmad-core/              # BMad framework files (agents, tasks, templates)
├── .git/                    # Git version control
├── .venv/                   # Python virtual environment (local only)
├── .windsurf/               # Windsurf IDE configuration
├── app/                     # ★ Main application code
├── docs/                    # ★ Project documentation
├── tests/                   # ★ Test suite
├── .gitignore               # Git ignore rules
├── .env.example             # Environment variable template
├── config.yaml              # Application configuration
├── DEV_TODOS.md             # Development task list
├── LICENSE                  # Project license
├── Makefile                 # Build and automation tasks
├── pyproject.toml           # Python project metadata + tool configs
├── pytest.ini               # Pytest configuration
├── README.md                # Project overview
├── requirements.txt         # Python dependencies
└── streamlit.toml           # Streamlit configuration
```

---

## Application Code (`app/`)

### Structure

```
app/
├── __init__.py              # Package initialization
├── main.py                  # ★ Streamlit application entry point
├── config.py                # Configuration loader (.env handling)
│
├── pages/                   # ★ Streamlit multi-page dashboards
│   ├── __init__.py
│   ├── 1_overview.py        # KPI overview dashboard
│   ├── 2_batch.py           # Batch monitoring page
│   ├── 3_sessions.py        # Session monitoring page
│   ├── 4_blocking.py        # Blocking chains page
│   ├── 5_sql_health.py      # SQL health metrics page
│   ├── 6_alerts.py          # Alert inbox and management
│   └── 7_admin.py           # Admin configuration page
│
├── services/                # ★ Business logic layer
│   ├── __init__.py
│   ├── batch_service.py     # Batch job queries and aggregations
│   ├── session_service.py   # Session and transaction queries
│   ├── blocking_service.py  # Blocking chain analysis
│   ├── sql_health_service.py # SQL DMV queries and health checks
│   ├── alert_service.py     # Alert rule evaluation
│   └── cache_manager.py     # Caching coordination
│
├── db/                      # ★ Data access layer
│   ├── __init__.py
│   ├── connection_manager.py # Connection pooling and health checks
│   ├── ax_queries.py        # SQL queries for AX tables
│   ├── sql_queries.py       # SQL Server DMV queries
│   ├── staging_writer.py    # Write operations to staging DB
│   └── models.py            # Data models (Pydantic or dataclasses)
│
├── scheduler/               # ★ Background job scheduling
│   ├── __init__.py
│   ├── scheduler_manager.py # APScheduler setup and lifecycle
│   ├── jobs.py              # Job definitions (collect_batch, etc.)
│   └── retention.py         # Data retention cleanup jobs
│
├── alerts/                  # ★ Alerting system
│   ├── __init__.py
│   ├── rule_engine.py       # Alert rule evaluation logic
│   ├── deduplicator.py      # Alert suppression and throttling
│   ├── email_sender.py      # SMTP email delivery
│   └── alert_store.py       # Alert persistence to staging DB
│
├── ui/                      # ★ Reusable UI components
│   ├── __init__.py
│   ├── filters.py           # Global filters (time range, environment, AOS)
│   ├── kpi_tiles.py         # KPI display tiles
│   ├── charts.py            # Plotly chart builders
│   └── tables.py            # Formatted data tables
│
├── sql/                     # ★ SQL scripts
│   ├── schema/              # DDL scripts for staging DB
│   │   ├── 001_dimensions.sql
│   │   ├── 002_facts.sql
│   │   ├── 003_alerts.sql
│   │   └── 004_indexes.sql
│   ├── queries/             # Reusable query templates
│   │   ├── batch_queries.sql
│   │   ├── session_queries.sql
│   │   └── dmv_queries.sql
│   └── migrations/          # Schema migration scripts
│       ├── v1_to_v2.sql
│       └── README.md
│
└── utils/                   # ★ Shared utilities
    ├── __init__.py
    ├── logger.py            # Structured logging setup (structlog)
    ├── cache.py             # Cache decorators and utilities
    ├── date_utils.py        # Date/time helpers
    ├── validators.py        # Input validation functions
    └── formatters.py        # Data formatting utilities
```

---

## Documentation (`docs/`)

```
docs/
├── README.md                # Documentation index
├── prd.md                   # ★ Product Requirements Document
├── architecture.md          # ★ Architecture overview
│
├── architecture/            # ★ Detailed architecture docs
│   ├── tech-stack.md        # Technology decisions
│   ├── coding-standards.md  # Code quality guidelines
│   └── source-tree.md       # This file
│
├── sql/                     # SQL-specific documentation
│   ├── xevents-deadlock-setup.sql    # Extended Events setup
│   ├── xevents-deadlock-read.sql     # Read deadlock data
│   └── xevents-deadlock-view.sql     # Deadlock views
│
├── deploy-runbook.md        # Deployment procedures
├── rollback-runbook.md      # Rollback procedures
├── alert-triage-runbook.md  # Alert response guide
├── baseline-plan.md         # Baseline calculation strategy
└── xevents-deadlock-guide.md # Deadlock monitoring guide
```

---

## Tests (`tests/`)

```
tests/
├── __init__.py
├── conftest.py              # Pytest fixtures and configuration
│
├── unit/                    # ★ Unit tests (service logic)
│   ├── __init__.py
│   ├── test_batch_service.py
│   ├── test_session_service.py
│   ├── test_blocking_service.py
│   ├── test_sql_health_service.py
│   ├── test_alert_service.py
│   └── test_cache_manager.py
│
├── integration/             # ★ Integration tests (DB access)
│   ├── __init__.py
│   ├── test_connection_manager.py
│   ├── test_ax_queries.py
│   ├── test_sql_queries.py
│   └── test_staging_writer.py
│
├── e2e/                     # ★ End-to-end smoke tests
│   ├── __init__.py
│   ├── test_overview_page.py
│   ├── test_batch_page.py
│   └── test_alert_flow.py
│
└── fixtures/                # Test data fixtures
    ├── README.md
    ├── batch_data.json
    ├── session_data.json
    └── sample_queries.sql
```

---

## Configuration Files

### Root Configuration

**`.gitignore`**
- Excludes: `.venv/`, `__pycache__/`, `.env*`, `*.pyc`, `logs/`

**`.env.example`**
- Template for environment-specific configuration
- Copy to `.env.dev`, `.env.tst`, `.env.prd`

**`config.yaml`**
- Application-level configuration (non-secret)
- Environment names, feature toggles, default thresholds

**`pyproject.toml`**
- Python project metadata
- Tool configurations (ruff, black, mypy, pytest)

**`requirements.txt`**
- Production dependencies (pinned versions)

**`dev-requirements.txt`** (optional)
- Development dependencies (pytest, ruff, black, mypy)

**`Makefile`**
- Common tasks: `make test`, `make lint`, `make run`, `make deploy`

### Streamlit Configuration

**`streamlit.toml`**
```toml
[server]
port = 8501
enableCORS = false
enableXsrfProtection = true

[browser]
gatherUsageStats = false

[theme]
primaryColor = "#0066CC"
backgroundColor = "#FFFFFF"
secondaryBackgroundColor = "#F0F2F6"
textColor = "#262730"
font = "sans serif"
```

### Pytest Configuration

**`pytest.ini`**
```ini
[pytest]
testpaths = tests
python_files = test_*.py
python_classes = Test*
python_functions = test_*
addopts = 
    --verbose
    --tb=short
    --cov=app
    --cov-report=html
    --cov-report=term
markers =
    unit: Unit tests (fast)
    integration: Integration tests (require DB)
    slow: Slow tests (> 1 second)
```

---

## Key Files

### Application Entry Point

**`app/main.py`**
```python
"""
Streamlit application entry point.

Run with: streamlit run app/main.py
"""
import streamlit as st
from app.config import load_config
from app.scheduler.scheduler_manager import start_scheduler
from app.utils.logger import setup_logger

# Initialize logger
log = setup_logger()

# Load configuration
config = load_config()

# Start background scheduler
scheduler = start_scheduler()

# Streamlit page configuration
st.set_page_config(
    page_title="AX Performance Monitor",
    page_icon="📊",
    layout="wide",
    initial_sidebar_state="expanded"
)

# Main page content
st.title("AX 2012 R3 Performance Monitor")
st.markdown("Select a page from the sidebar to begin.")
```

### Configuration Loader

**`app/config.py`**
```python
"""
Configuration management using environment variables.
"""
import os
from dataclasses import dataclass
from dotenv import load_dotenv

@dataclass
class DatabaseConfig:
    driver: str
    server: str
    database: str
    user: str
    password: str

@dataclass
class AppConfig:
    environment: str
    ax_db: DatabaseConfig
    staging_db: DatabaseConfig
    smtp_host: str
    smtp_port: int
    alert_recipients: list[str]
    cache_ttl: int

def load_config() -> AppConfig:
    """Load configuration from environment variables."""
    env = os.getenv('APP_ENV', 'DEV')
    load_dotenv(f'.env.{env.lower()}')
    
    return AppConfig(
        environment=env,
        ax_db=DatabaseConfig(
            driver=os.getenv('AX_DB_DRIVER'),
            server=os.getenv('AX_DB_SERVER'),
            database=os.getenv('AX_DB_NAME'),
            user=os.getenv('AX_DB_USER'),
            password=os.getenv('AX_DB_PASSWORD')
        ),
        # ... rest of config
    )
```

---

## File Naming Conventions

### Python Files

| Pattern | Purpose | Example |
|---------|---------|---------|
| `*_service.py` | Business logic services | `batch_service.py` |
| `*_manager.py` | Resource managers | `connection_manager.py` |
| `*_queries.py` | SQL query collections | `ax_queries.py` |
| `*_utils.py` | Utility modules | `date_utils.py` |
| `test_*.py` | Test files | `test_batch_service.py` |

### SQL Files

| Pattern | Purpose | Example |
|---------|---------|---------|
| `###_*.sql` | Schema migrations | `001_dimensions.sql` |
| `*_queries.sql` | Query templates | `batch_queries.sql` |
| `xevents-*.sql` | Extended Events | `xevents-deadlock-setup.sql` |

### Documentation

| Pattern | Purpose | Example |
|---------|---------|---------|
| `*.md` | Markdown documents | `architecture.md` |
| `*-runbook.md` | Operational runbooks | `deploy-runbook.md` |

---

## Import Paths

### Absolute Imports (Preferred)

```python
# Good: Absolute from project root
from app.services.batch_service import get_batch_backlog
from app.db.connection_manager import get_connection
from app.utils.logger import get_logger
```

### Relative Imports (Within Package)

```python
# Acceptable: Within same package
from .cache_manager import get_cache
from ..utils.logger import get_logger
```

### Import Order

1. Standard library
2. Third-party packages
3. Local application modules

---

## Environment-Specific Files

### Not in Git (gitignored)

```
.env.dev              # Development environment config
.env.tst              # Test environment config
.env.prd              # Production environment config
.venv/                # Virtual environment
__pycache__/          # Python bytecode
*.pyc                 # Compiled Python
.pytest_cache/        # Pytest cache
.coverage             # Coverage data
htmlcov/              # Coverage HTML report
logs/                 # Application logs
*.log                 # Log files
.DS_Store             # macOS metadata
Thumbs.db             # Windows metadata
```

### In Git (tracked)

```
.env.example          # Template for .env files
requirements.txt      # Production dependencies
dev-requirements.txt  # Development dependencies
pyproject.toml        # Project metadata
pytest.ini            # Test configuration
.gitignore            # Git ignore rules
README.md             # Project documentation
```

---

## Build Artifacts

### Generated Directories

```
.pytest_cache/        # Pytest cache (gitignored)
htmlcov/              # Coverage HTML report (gitignored)
dist/                 # Built packages (gitignored)
build/                # Build artifacts (gitignored)
*.egg-info/           # Package metadata (gitignored)
```

### Log Files

```
logs/
├── ax-monitor.log           # Application log (rotated)
├── ax-monitor.log.1         # Rotated log
├── ax-monitor.log.2         # Rotated log
└── scheduler.log            # Scheduler-specific log
```

---

## Deployment Structure

### Production Deployment Path

```
C:\apps\ax-monitor\
├── .venv\                   # Virtual environment
├── app\                     # Application code
├── docs\                    # Documentation
├── logs\                    # Log files
├── .env.prd                 # Production config
├── config.yaml              # App configuration
└── requirements.txt         # Dependencies
```

### Windows Service Configuration

```
Service Name: AXMonitor
Display Name: AX Performance Monitor
Executable: C:\apps\ax-monitor\.venv\Scripts\streamlit.exe
Parameters: run app\main.py --server.port 8501
Working Dir: C:\apps\ax-monitor
Startup: Automatic
Recovery: Restart on failure
```

---

## Development Workflow

### Initial Setup

```powershell
# Clone repository
git clone <repo-url> AXMonitoringBU
cd AXMonitoringBU

# Create virtual environment
python -m venv .venv
.venv\Scripts\Activate.ps1

# Install dependencies
pip install -r requirements.txt
pip install -r dev-requirements.txt

# Copy environment template
cp .env.example .env.dev

# Edit .env.dev with your settings
notepad .env.dev

# Run tests
pytest

# Start application
streamlit run app\main.py
```

### Adding a New Feature

1. Create feature branch: `git checkout -b feature/new-feature`
2. Implement in appropriate layer (service, db, ui)
3. Add unit tests in `tests/unit/`
4. Add integration tests if needed
5. Update documentation
6. Run quality checks: `make lint`, `make test`
7. Commit with conventional commit message
8. Create pull request

---

## Module Dependencies

### Dependency Flow

```
Streamlit Pages (UI)
    ↓
Services (Business Logic)
    ↓
DB Access Layer
    ↓
SQL Server (AX + Staging)

Scheduler (Background)
    ↓
Services
    ↓
DB Access Layer
    ↓
SQL Server
```

### Circular Dependency Prevention

- **Services** should not import from **Pages**
- **DB Layer** should not import from **Services**
- **Utils** should not import from **Services** or **DB**
- Use dependency injection where needed

---

## Future Structure Evolution

### Phase 2 Additions

```
app/
├── api/                     # REST API endpoints (FastAPI)
│   ├── __init__.py
│   ├── main.py
│   ├── routes/
│   └── models/
│
├── ml/                      # ML models for anomaly detection
│   ├── __init__.py
│   ├── baseline_model.py
│   └── anomaly_detector.py
│
└── integrations/            # External integrations
    ├── __init__.py
    ├── teams_webhook.py
    └── servicenow_api.py
```

---

## References

- Python Package Structure: [Python Packaging Guide](https://packaging.python.org/)
- Streamlit Multi-Page Apps: [Streamlit Docs](https://docs.streamlit.io/library/get-started/multipage-apps)
- Project Layout Best Practices: [Real Python](https://realpython.com/python-application-layouts/)

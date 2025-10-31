# Technology Stack
## AX 2012 R3 Performance Leak Monitor

**Last Updated:** 2025-10-23

---

## Core Technology Decisions

### Programming Language: Python 3.10+

**Rationale:**
- Strong ecosystem for data processing (Pandas) and visualization (Plotly)
- Streamlit framework requires Python 3.10+
- Excellent SQL Server connectivity libraries (pyodbc, pymssql)
- Rich testing frameworks (pytest)
- Type hinting support for maintainability

**Alternatives Considered:**
- ❌ .NET/C# - Less suitable for rapid dashboard prototyping
- ❌ Node.js - Weaker data processing libraries
- ❌ PowerShell - Limited web framework options

---

## Application Framework

### Web Framework: Streamlit 1.31+

**Rationale:**
- **Rapid Development:** Build dashboards in minutes, not days
- **Reactive UI:** Automatic re-execution on user interactions
- **Built-in Components:** Charts, filters, tables out-of-the-box
- **Python Native:** No JavaScript required
- **Multi-Page Support:** Native page routing
- **Community:** Large ecosystem of components and examples

**Limitations Accepted:**
- Not suitable for complex, highly interactive SPAs
- Limited customization compared to React/Vue
- Performance limited for >100 concurrent users (acceptable for MVP)

**Alternatives Considered:**
- ❌ Flask/Django + React - Higher development complexity
- ❌ Dash (Plotly) - More boilerplate than Streamlit
- ❌ Gradio - Less mature for dashboard use cases

---

## Data Processing & Visualization

### Data Processing: Pandas 2.0+

**Rationale:**
- Industry standard for tabular data manipulation
- Excellent SQL integration (read_sql)
- Efficient aggregations (groupby, rolling windows)
- Type support for data validation

### Visualization: Plotly 5.18+

**Rationale:**
- Interactive charts (zoom, pan, hover)
- Seamless Streamlit integration (`st.plotly_chart`)
- Wide range of chart types (bar, line, scatter, Sankey, etc.)
- Responsive and mobile-friendly (bonus)
- Export to PNG/SVG

**Alternatives Considered:**
- ❌ Matplotlib - Static charts, less interactive
- ❌ Altair - Declarative but limited chart types

---

## Database Connectivity

### Primary: pyodbc (ODBC Driver 17/18)

**Rationale:**
- **Official Microsoft driver** for SQL Server
- **Mature and stable** - production-proven
- Supports all SQL Server features (Extended Events, DMVs)
- Connection pooling built-in

**Configuration:**
```python
import pyodbc
conn_string = (
    f"DRIVER={{ODBC Driver 17 for SQL Server}};"
    f"SERVER={server};DATABASE={database};"
    f"UID={user};PWD={password};"
)
conn = pyodbc.connect(conn_string, timeout=30)
```

### Fallback: pymssql

**Rationale:**
- Pure Python alternative (no ODBC dependency)
- Useful for environments where ODBC driver installation is restricted
- Feature toggle: `DB_DRIVER=pymssql` in .env

**Trade-offs:**
- Slightly less feature-complete than pyodbc
- Less community support for edge cases

**Decision:** Use pyodbc by default, pymssql as fallback option

---

## Scheduling & Background Jobs

### Scheduler: APScheduler 3.10+

**Rationale:**
- **In-process scheduling** - no external dependencies (vs. Celery)
- **Multiple triggers:** Interval, cron, date-based
- **Thread-based execution** - suitable for I/O-bound tasks
- **Persistent storage** optional (not needed for MVP)
- **Lightweight** - minimal overhead

**Configuration:**
```python
from apscheduler.schedulers.background import BackgroundScheduler

scheduler = BackgroundScheduler()
scheduler.add_job(
    func=collect_batch_data,
    trigger='interval',
    seconds=120,
    id='collect_batch',
    max_instances=1  # Prevent overlapping runs
)
scheduler.start()
```

**Alternatives Considered:**
- ❌ Celery + Redis/RabbitMQ - Over-engineered for MVP
- ❌ Windows Task Scheduler - Less flexible, harder to test
- ❌ Cron (Linux) - Not applicable on Windows target

---

## Caching

### In-Memory Cache: cachetools 5.3+

**Rationale:**
- **TTL (Time-To-Live) cache** - automatic expiration
- **LRU (Least Recently Used)** support for memory management
- **Simple API** - decorator-based caching
- **No external dependencies** - in-process memory

**Usage Pattern:**
```python
from cachetools import TTLCache, cached

cache = TTLCache(maxsize=100, ttl=60)  # 60 second TTL

@cached(cache)
def get_batch_backlog(environment: str, time_range: str):
    # Expensive query here
    return query_result
```

**Cache Invalidation Strategy:**
- Time-based: TTL per data type (30s-120s)
- Manual: Admin page "Clear Cache" button
- Key-based: Include filters in cache key

**Future Enhancement:** Redis for multi-instance deployments (Phase 2)

---

## Logging & Observability

### Structured Logging: structlog 23.2+

**Rationale:**
- **Structured output** - JSON-ready logs for parsing
- **Context binding** - Attach correlation IDs, user context
- **Flexible processors** - Add timestamps, log levels, stack traces
- **Performance** - Minimal overhead

**Configuration:**
```python
import structlog

structlog.configure(
    processors=[
        structlog.processors.TimeStamper(fmt="iso"),
        structlog.processors.add_log_level,
        structlog.processors.StackInfoRenderer(),
        structlog.dev.ConsoleRenderer()  # Human-readable for dev
    ]
)

log = structlog.get_logger()
log.info("batch_job_collected", job_id=123, duration_ms=450)
```

**Log Levels:**
- DEBUG: Query details, cache hits/misses
- INFO: Job execution, alert sent
- WARNING: Retry attempts, slow queries
- ERROR: Job failures, connection errors
- CRITICAL: Service startup failures

---

## Configuration Management

### Environment Variables: python-dotenv 1.0+

**Rationale:**
- **12-Factor App** compliant
- **Environment-specific** configs (DEV/TST/PRD)
- **Secret-safe** - .env not committed to Git
- **Simple API** - `os.getenv()` or `Config` class

**Structure:**
```
.env.example      # Template with dummy values
.env.dev          # Development settings
.env.tst          # Test environment
.env.prd          # Production (gitignored)
```

**Loading:**
```python
from dotenv import load_dotenv
import os

env = os.getenv('APP_ENV', 'DEV')
load_dotenv(f'.env.{env.lower()}')

DB_SERVER = os.getenv('AX_DB_SERVER')
```

---

## Testing Framework

### Unit Testing: pytest 7.4+

**Rationale:**
- **Powerful fixtures** - Setup/teardown, parameterization
- **Rich assertions** - Better error messages than unittest
- **Plugin ecosystem** - Coverage, mocking, parallel execution
- **Markers** - Tag tests (unit, integration, slow)

### Mocking: pytest-mock / unittest.mock

**Usage:**
- Mock SQL connections for unit tests
- Mock SMTP for alert tests
- Fixtures for test data

### Coverage: pytest-cov

**Target:** ≥80% coverage for service layer

---

## Code Quality Tools

### Linting: ruff 0.1+

**Rationale:**
- **Fast** - Written in Rust, 10-100x faster than Flake8
- **All-in-one** - Replaces Flake8, isort, pyupgrade
- **Auto-fix** - Many issues fixed automatically

### Formatting: black 23.11+

**Rationale:**
- **Opinionated** - No configuration needed
- **Consistent** - Standard in Python community
- **Streamlit compatible**

### Type Checking: mypy 1.7+

**Rationale:**
- **Static type checking** - Catch bugs at dev time
- **Gradual typing** - Can ignore legacy code
- **IDE integration** - VS Code, PyCharm

**Configuration (pyproject.toml):**
```toml
[tool.mypy]
python_version = "3.10"
warn_return_any = true
warn_unused_configs = true
disallow_untyped_defs = true
```

### Pre-commit Hooks: pre-commit 3.5+

**Hooks:**
- ruff linting
- black formatting
- mypy type checking
- Trailing whitespace removal
- YAML/JSON validation

---

## Deployment Technologies

### Runtime Environment

**Operating System:** Windows Server 2016+ or Windows 10/11
- Target: Customer Windows infrastructure
- No containerization required for MVP

**Python Environment:** Virtual environment (venv)
```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

### Service Management

**Option 1 (Preferred): Windows Service via NSSM**
```powershell
nssm install AXMonitor "C:\apps\ax-monitor\.venv\Scripts\streamlit.exe"
nssm set AXMonitor AppParameters "run app.py --server.port 8501"
nssm set AXMonitor AppDirectory "C:\apps\ax-monitor"
nssm start AXMonitor
```

**Option 2: Scheduled Task**
- Trigger: At startup
- Action: Run Streamlit CLI
- Survives user logoff

### Reverse Proxy (Optional, Recommended for PRD)

**IIS (Internet Information Services)**
- HTTPS termination
- Windows Authentication (future)
- URL rewriting
- Load balancing (multi-instance future)

**Alternative: Nginx for Windows**

---

## Database Technologies

### SQL Server 2016

**Why 2016:**
- Customer constraint (existing infrastructure)
- AX 2012 R3 compatibility
- Extended Events support (deadlock capture)
- DMV availability

**Driver Requirements:**
- ODBC Driver 17 or 18 for SQL Server
- TLS 1.2 support

**Connection Pooling:**
- Enabled by default in pyodbc
- Max pool size: 10 connections per environment

---

## SMTP / Email

**SMTP Client:** Python `smtplib` (built-in)

**Configuration:**
- Port 25: Relay without authentication (typical for internal SMTP)
- Port 587: TLS with authentication (if required)

**Email Format:**
- Plain text + HTML multipart
- Deep links to dashboard with filters pre-applied

---

## Development Tools

### IDE: Visual Studio Code (Recommended)

**Extensions:**
- Python (Microsoft)
- Pylance (type checking)
- Streamlit (syntax highlighting)
- GitLens

### Version Control: Git

**Branching Strategy:**
- `main` - Production-ready code
- `develop` - Integration branch
- `feature/*` - Feature branches

**Commit Conventions:**
- Conventional Commits (feat, fix, docs, chore)

---

## Dependency Management

### Requirements File: requirements.txt

**Structure:**
```
# Core
streamlit==1.31.0
pandas==2.0.3
plotly==5.18.0

# Database
pyodbc==5.0.1
pymssql==2.2.8  # Optional fallback

# Scheduling
APScheduler==3.10.4

# Utilities
python-dotenv==1.0.0
cachetools==5.3.2
structlog==23.2.0

# Development (dev-requirements.txt)
pytest==7.4.3
pytest-cov==4.1.0
pytest-mock==3.12.0
ruff==0.1.6
black==23.11.0
mypy==1.7.1
pre-commit==3.5.0
```

**Installation:**
```powershell
pip install -r requirements.txt
pip install -r dev-requirements.txt  # Dev only
```

---

## Future Technology Considerations

### Phase 2 Enhancements

**Distributed Caching:**
- Redis for multi-instance deployments
- Shared cache across web app instances

**Message Queue:**
- RabbitMQ or Azure Service Bus for async alerting
- Decouple alert processing from data collection

**Container Deployment:**
- Docker image for portability
- Kubernetes for orchestration (if scale demands)

**Advanced Monitoring:**
- Prometheus metrics endpoint
- Grafana for application observability

### Phase 3 Enhancements

**API Layer:**
- FastAPI for REST endpoints
- JWT authentication
- OpenAPI documentation

**Frontend Evolution:**
- React dashboard for richer interactivity
- Streamlit remains for admin/config pages

---

## Technology Decision Log

| Decision | Date | Rationale | Alternatives |
|----------|------|-----------|--------------|
| Streamlit over Flask+React | 2025-10 | Faster MVP delivery | Flask+React (higher complexity) |
| pyodbc over pymssql | 2025-10 | Microsoft official driver | pymssql (fallback option) |
| APScheduler over Celery | 2025-10 | Simpler for single-instance | Celery (overkill for MVP) |
| cachetools over Redis | 2025-10 | No external dependencies | Redis (Phase 2 if needed) |
| Windows Service over Docker | 2025-10 | Customer environment | Docker (future option) |

---

## Licensing

All selected technologies use permissive open-source licenses:
- **Python:** PSF License
- **Streamlit:** Apache 2.0
- **Pandas, Plotly:** BSD 3-Clause
- **pyodbc:** MIT
- **APScheduler:** MIT

**No copyleft licenses (GPL) used** - safe for commercial deployment

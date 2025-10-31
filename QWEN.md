# AX Monitoring BU - Qwen Context

## Project Overview

The AX Monitoring BU is a comprehensive monitoring solution for Microsoft Dynamics AX 2012 R3 systems running on SQL Server 2016. Built as a Streamlit-based dashboard application, it provides real-time visibility into AX 2012 R3 and SQL Server 2016 performance metrics. The system monitors batch jobs, user sessions, SQL blocking chains, and database health, generating proactive alerts before users are impacted.

The project is designed to be a read-only monitoring solution that integrates with AX 2012 R3 databases without modifying them, focusing on performance leak detection and real-time alerting.

### Key Features
- **Batch Monitoring:** Real-time backlog tracking, execution time trends (P50/P95/P99), error rates
- **Session Management:** Active/inactive sessions per AOS, long-running transactions
- **Blocking Analysis:** SQL blocking chain visualization with root cause SQL text
- **SQL Health:** CPU, I/O, TempDB, wait statistics, top expensive queries
- **Proactive Alerting:** Threshold and baseline-based rules with email notifications
- **Admin Dashboard:** Configuration management, health checks, alert tuning
- **Advanced Features:** AI assistant, ML predictions, trend analysis, business intelligence
- **Auftragsfreigabe (Order Release):** Specialized order release functionality

### Technology Stack
- **Language:** Python 3.10+
- **Web Framework:** Streamlit
- **Database:** SQL Server 2016 (pyodbc/pymssql)
- **Scheduler:** APScheduler
- **Visualization:** Plotly, Pandas
- **Logging:** structlog
- **ML/Analytics:** scikit-learn, numpy, matplotlib, seaborn

## Project Structure
```
AXMonitoringBU/
├── app/                     # Main application code
│   ├── alerts/             # Alerting system
│   ├── analytics/          # Analytics and ML components
│   ├── api/                # API components (though Streamlit-only)
│   ├── automation/         # Automation features
│   ├── bi/                 # Business intelligence components
│   ├── compliance/         # Compliance features
│   ├── components/         # Reusable UI components
│   ├── db/                 # Database access layer
│   ├── integrations/       # External integrations
│   ├── metrics/            # Metrics collection
│   ├── ml/                 # Machine learning components
│   ├── monitoring/         # Core monitoring logic
│   ├── pages/              # Streamlit dashboard pages
│   ├── reports/            # Report generation
│   ├── storage/            # Data storage components
│   ├── __init__.py
│   ├── app.py              # Main application entry point
│   └── data_service.py     # Data service layer
├── docs/                   # Documentation
├── tests/                  # Test suite
├── requirements.txt        # Production dependencies
├── pyproject.toml          # Project configuration
├── config.yaml             # Application configuration
├── Makefile                # Build automation
└── README.md               # Project documentation
```

## Building and Running

### Prerequisites
- Python 3.10 or higher
- SQL Server ODBC Driver 17 or 18
- Access to AX 2012 R3 database (read-only)
- Separate staging database for metrics storage

### Installation
```powershell
# Create virtual environment
python -m venv .venv
.venv\Scripts\Activate.ps1

# Install dependencies
pip install -r requirements.txt
pip install -r dev-requirements.txt  # For development

# Configure environment
cp env.example .env.dev
# Edit .env.dev with your database credentials
notepad .env.dev
```

### Running the Application
```powershell
# Using python directly
python -m streamlit run app/app.py

# Or using Makefile
make run

# For development with auto-reload
make run-dev
```

### Testing
```powershell
# Run all tests with coverage
pytest --cov=app --cov-report=html

# Run specific test types
pytest tests/unit          # Unit tests only
pytest tests/integration   # Integration tests only
pytest -m "not slow"       # Skip slow tests
```

### Quality Checks
```powershell
# Run linting
ruff check app tests

# Format code
black app tests

# Type checking
mypy app

# Run all checks
pre-commit run --all-files
```

## Development Conventions

### Code Quality
This project enforces code quality through automated tools:
- **Formatting:** Black and isort for consistent formatting
- **Linting:** Ruff for code quality checks
- **Type Checking:** MyPy for type safety
- **Pre-commit Hooks:** Automated checks before commits

### Testing Standards
- Minimum 80% code coverage for critical components
- Unit tests for all business logic
- Integration tests for database interactions
- Mock external dependencies in unit tests

### Configuration Management
- Use environment variables loaded from `.env` files
- Support multiple environments (DEV/TST/PRD)
- No hardcoded secrets in the codebase
- Structured logging with correlation IDs

### Git Workflow
1. Create feature branch: `git checkout -b feature/your-feature`
2. Implement changes following coding standards
3. Write/update tests (≥80% coverage target)
4. Run quality checks: `make lint`, `make test`, `make typecheck`
5. Commit with conventional commit message: `feat(batch): add P95 calculation`
6. Push and create pull request

## Configuration

### Environment Variables
Configuration is managed through `.env` files with the following key variables:

- **Database:** `DB_SERVER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `DB_DRIVER`
- **Email:** `SMTP_SERVER`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `EMAIL_FROM`
- **Teams:** `TEAMS_WEBHOOK_URL`
- **Security:** `SECRET_KEY`, `JWT_SECRET`

### Application Configuration
The system uses a `config.yaml` file for application-level settings:
- Monitoring thresholds and intervals
- Alert rules and conditions
- ML model parameters
- UI theming options
- API settings

### Required Environment Variables
- **AX Database:** `DB_SERVER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `DB_DRIVER`
- **SMTP:** `SMTP_SERVER`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `EMAIL_FROM`
- **Teams:** `TEAMS_WEBHOOK_URL`
- **Security:** `SECRET_KEY`, `JWT_SECRET`

## Development Status

### Currently Implemented
- Main dashboard with KPI metrics
- Batch job monitoring with detailed views
- Session tracking and monitoring
- SQL Health monitoring
- Blocking chain visualization
- Alert management
- Admin configuration page
- AI Assistant interface
- ML Predictions framework
- Business Intelligence reporting
- Trend Analysis component
- Specialized order release functionality

### Development Pipeline (from DEV_TODOS.md)
The project follows a comprehensive development roadmap organized by epics:

- **Epic 0:** Project setup and quality assurance
- **Epic 1:** Configuration and secrets management
- **Epic 2:** Database access layer (read-only)
- **Epic 3:** Data model for staging/reporting
- **Epic 4:** Data ingestion and scheduler
- **Epic 5:** Service layer for business logic
- **Epic 6:** Alerting system
- **Epic 7:** UI components and pages
- **Epic 8:** Security and RBAC
- **Epic 9:** Observability features
- **Epic 10:** Testing and data quality
- **Epic 11:** CI/CD pipeline

### Key Development Tasks
- Implement data ingestion from AX 2012 R3 database
- Develop scheduler for regular monitoring jobs
- Complete alerting system with thresholds
- Add authentication and role-based access control
- Implement proper database connection handling
- Complete integration with real AX 2012 R3 data sources
- Add comprehensive testing framework
- Set up CI/CD pipeline

## Special Components

### Auftragsfreigabe (Order Release)
A specialized feature for order release functionality, including backup implementation for resilience.

### AI Assistant
Integrated AI assistant (page 16) for enhanced monitoring and analysis capabilities.

### ML Predictions
Machine learning components for predictive monitoring and anomaly detection.

### Business Intelligence
Advanced analytics and reporting features for business insights.

## Deployment

### Windows Service Deployment
The application can be deployed as a Windows service using NSSM:
```powershell
# Install NSSM (if not already)
choco install nssm

# Install service
nssm install AXMonitor "C:\apps\ax-monitor\.venv\Scripts\streamlit.exe"
nssm set AXMonitor AppParameters "run app\main.py --server.port 8501"
nssm set AXMonitor AppDirectory "C:\apps\ax-monitor"
nssm set AXMonitor AppEnvironmentExtra "APP_ENV=PRD"

# Start service
nssm start AXMonitor
```

## Performance Targets
| Metric | Target (p95) |
|--------|--------------|
| Dashboard page load | < 3s |
| Data freshness | < 60s |
| Ingestion job latency | < 10s |
| Cache hit rate | > 70% |

## Key Files and Components

### Main Application
- `app/app.py` - Main dashboard entry point
- `app/data_service.py` - Data service with mock data
- `app/pages/` - Multiple dashboard pages

### Configuration
- `config.yaml` - Application configuration
- `env.example` - Environment variables template
- `pyproject.toml` - Project dependencies and settings

### Development
- `DEV_TODOS.md` - Comprehensive development task list
- `Makefile` - Build and development commands
- `requirements.txt` - Python dependencies
- `pytest.ini` - Test configuration
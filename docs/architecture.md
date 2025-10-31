# Architecture Document
## AX 2012 R3 Performance Leak Monitor

**Version:** 1.0  
**Date:** 2025-10-23  
**Status:** Draft  
**Architecture Version:** v4

---

## 1. Architecture Overview

### 1.1 System Purpose
The AX Performance Monitor is a read-only monitoring and alerting system that collects performance metrics from Microsoft Dynamics AX 2012 R3 and SQL Server 2016, presents them in real-time dashboards, and generates proactive alerts.

### 1.2 Key Architectural Principles

**Read-Only Philosophy**
- Zero write operations to AX production database
- All collected data stored in separate staging database
- Monitoring queries optimized to minimize production impact

**Performance-First Design**
- Sub-3-second dashboard load times (p95)
- Aggressive caching with configurable TTL
- Query result pagination and TOP limits
- Asynchronous data collection

**Reliability & Resilience**
- Retry logic with exponential backoff
- Graceful degradation on connection failures
- Idempotent data collection jobs
- Comprehensive error logging

**Operational Simplicity**
- Single-server deployment
- Configuration via environment variables
- No external dependencies beyond SQL Server
- Windows Service deployment model

---

## 2. System Context

```
External Users → [Streamlit Web App] → [Service Layer] → [Scheduler] 
                                                            ↓
                                    [AX DB (RO)] ← [DB Access Layer] → [Staging DB]
                                                            ↓
                                                      [SMTP Server]
```

**External Systems:**
1. **AX 2012 R3 Production DB** - Read-only queries for batch, session, SQL health
2. **Staging Database** - Persistent storage for collected metrics
3. **SMTP Server** - Alert email delivery

---

## 3. Component Architecture

### 3.1 Layers

**Presentation Layer (Streamlit UI)**
- Entry: `app.py`
- Pages: `app/pages/` (overview, batch, sessions, blocking, sql_health, alerts, admin)
- Components: `app/ui/` (filters, KPI tiles, charts)

**Service Layer**
- `app/services/batch_service.py` - Batch queries, trends
- `app/services/session_service.py` - Session counts, transactions
- `app/services/blocking_service.py` - Blocking chains
- `app/services/sql_health_service.py` - DMV queries, waits
- `app/services/alert_service.py` - Alert evaluation
- `app/services/cache_manager.py` - TTL caching

**Data Collection Layer (APScheduler)**
- `collect_sessions` - 30-60s interval
- `collect_batch` - 60-120s interval
- `collect_sql_health` - 1-5min interval
- `collect_deadlocks` - 2min interval
- `retention_cleanup` - Daily

**Data Access Layer**
- `app/db/connection_manager.py` - Connection pooling
- `app/db/ax_queries.py` - AX table queries
- `app/db/sql_queries.py` - DMV queries
- `app/db/staging_writer.py` - Staging writes

**Alerting Layer**
- `app/alerts/rule_engine.py` - Rule evaluation
- `app/alerts/deduplicator.py` - Alert suppression
- `app/alerts/email_sender.py` - SMTP delivery

---

## 4. Data Architecture

### 4.1 Staging Database Schema

**Dimension Tables:**
- `dim_environment` - DEV/TST/PRD
- `dim_aos` - AOS instances
- `dim_batch_class` - Batch classifications
- `dim_user` - User references

**Fact Tables (Detailed):**
- `fact_session_snapshot` - 30 days retention
- `fact_batch_execution` - 30 days retention
- `fact_blocking_event` - 30 days retention
- `fact_sql_health_sample` - 30 days retention
- `fact_deadlock` - 30 days retention

**Fact Tables (Aggregated):**
- `fact_batch_daily` - 12 months retention
- `fact_sessions_hourly` - 12 months retention

**Alert Tables:**
- `alert_history` - 90 days retention
- `alert_suppression` - Active suppressions
- `alert_rules` - Rule definitions
- `maintenance_windows` - Maintenance schedule

---

## 5. Deployment Architecture

### 5.1 Deployment Model

**Environment:** Windows Server 2016+ or Windows 10+
**Runtime:** Python 3.10+ virtual environment
**Service:** Windows Service (NSSM) or Scheduled Task

**Network Requirements:**
- Inbound: Port 8501 (HTTP dashboard)
- Outbound: Port 1433 (SQL Server)
- Outbound: Port 25/587 (SMTP)

### 5.2 Configuration

**Environment Variables (.env):**
```env
APP_ENV=PRD
AX_DB_SERVER=sqlprd\AX2012
AX_DB_NAME=AX2012R3_PRD
AX_DB_USER=ax_monitor_ro
AX_DB_PASSWORD=***
STAGING_DB_SERVER=sqlprd\AX2012
STAGING_DB_NAME=AXMonitoring
STAGING_DB_USER=ax_monitor_rw
STAGING_DB_PASSWORD=***
SMTP_HOST=smtp.local
SMTP_PORT=25
ALERT_RECIPIENTS=ops@corp.local
```

**Secret Management:**
- MVP: `.env` file with NTFS ACLs
- Future: Azure Key Vault

---

## 6. Security Architecture

### 6.1 Authentication & Authorization

**Roles:**
- **Viewer** - Read dashboards, export data
- **Power-User** - Viewer + acknowledge alerts
- **Admin** - Power-User + modify config

**Credentials:**
- MVP: Simple login (hashed passwords in staging DB)
- Session timeout: 8 hours

### 6.2 Data Security

- AX DB: Read-only user, SELECT only
- Parameterized queries (SQL injection prevention)
- `.env` never committed (in .gitignore)
- HTTPS via reverse proxy (recommended)
- Audit logging for logins and config changes

---

## 7. Operational Architecture

### 7.1 Monitoring

**Health Checks:**
- SQL connectivity (AX + Staging)
- Scheduler status
- SMTP connectivity
- Cache performance

**Logging:**
- Framework: `structlog`
- Levels: DEBUG, INFO, WARNING, ERROR
- Outputs: Console + rotating file logs
- Retention: 30 days

### 7.2 Error Handling

**Transient Errors:** Exponential backoff retry (3 attempts)
**Config Errors:** Fail startup, log prominently
**Data Errors:** Log warning, skip record, continue
**User Errors:** Friendly UI message with guidance

### 7.3 Backup & Recovery

- Staging DB: Daily full + hourly transaction logs
- Config files: Daily backup to secure location
- RTO: 4 hours, RPO: 1 hour

---

## 8. Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Language | Python | 3.10+ |
| Web Framework | Streamlit | 1.31+ |
| DB Driver | pyodbc / pymssql | Latest |
| Scheduler | APScheduler | 3.10+ |
| Visualization | Plotly | 5.18+ |
| Data | Pandas | 2.0+ |
| Logging | structlog | 23.2+ |
| Caching | cachetools | 5.3+ |
| Config | python-dotenv | 1.0+ |

**Infrastructure:**
- OS: Windows Server 2016+ / Win10+
- Database: SQL Server 2016
- Service: Windows Service (NSSM)
- Reverse Proxy: IIS (optional, for HTTPS)

**Development:**
- Linting: ruff, black
- Type checking: mypy
- Testing: pytest
- Git hooks: pre-commit

---

## 9. Performance Targets

| Metric | Target (p95) |
|--------|--------------|
| Dashboard page load | < 3s |
| Data freshness | < 60s |
| Ingestion job latency | < 10s |
| Ingestion job duration | < 1.5s |
| Cache hit rate | > 70% |
| Service error rate | < 0.1% |

**Optimization Strategies:**
- TOP N queries with time filters
- Aggressive TTL caching
- Background data collection
- Index recommendations for staging DB

---

## 10. Integration Points

**Inbound:** None (pull-based)

**Outbound:**
1. SQL Server (AX) - TDS/ODBC, Read-only
2. SQL Server (Staging) - TDS/ODBC, Read/write
3. SMTP - Email alerts

**Future Integrations:**
- Teams/Slack webhooks
- ServiceNow/Jira API
- REST API for external tools
- Power BI (via staging DB)

---

## 11. Testing Strategy

**Test Coverage Targets:**
- Unit tests: ≥80% for service layer
- Integration tests: All critical SQL queries
- Smoke tests: 5-10 core user flows

**Test Types:**
- **Unit:** Service logic, calculations, cache behavior
- **Integration:** DB queries against test fixtures
- **Smoke:** End-to-end page loads, alert flow
- **Performance:** Load testing for p95 targets

---

## 12. Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| AX schema customizations | High | Configurable table/field mapping |
| SQL load impact | Medium | Optimized queries, configurable intervals |
| Alert fatigue | Medium | Pilot tuning, deduplication, throttling |
| Delayed permissions | High | Develop on TEST, mock data for unit tests |

---

## 13. References

- See `docs/architecture/tech-stack.md` for detailed technology decisions
- See `docs/architecture/coding-standards.md` for development guidelines
- See `docs/architecture/source-tree.md` for codebase structure
- See `docs/deploy-runbook.md` for deployment procedures

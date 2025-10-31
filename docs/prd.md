# Product Requirements Document (PRD)
## AX 2012 R3 Performance Leak Monitor

**Version:** 1.0  
**Date:** 2025-10-23  
**Status:** Draft  
**PRD Version:** v4

---

## 1. Executive Summary

### 1.1 Product Vision
The AX 2012 R3 Performance Leak Monitor is a real-time monitoring and alerting dashboard that provides visibility into performance issues within Microsoft Dynamics AX 2012 R3 environments. The system enables IT operations teams to proactively detect, diagnose, and resolve performance bottlenecks related to batch processing, session management, SQL blocking, and database health.

### 1.2 Business Opportunity
Organizations running AX 2012 R3 on SQL Server 2016 frequently experience performance degradation due to:
- Batch job backlogs causing delayed business processes
- Long-running sessions blocking critical transactions
- SQL blocking chains halting operations
- Undetected performance leaks accumulating over time

Current tools lack AX-specific context and require manual investigation across multiple systems, leading to delayed problem resolution and business impact.

---

## 2. Problem Statement

### 2.1 Problem Definition
**Who:** IT Operations teams, Database Administrators, AX System Administrators, and Performance Engineers managing AX 2012 R3 environments

**What:** Lack of real-time visibility into AX-specific performance issues, forcing reactive troubleshooting after users report problems

**Why it matters:**
- **Business Impact:** Batch delays cascade into delayed financial reporting, order processing, and inventory management
- **Operational Cost:** 2-4 hours average time to diagnose performance issues manually
- **User Experience:** Business users experience unexplained slowness and transaction failures
- **Risk:** Critical end-of-period processes fail due to undetected resource exhaustion

**Current State:**
- DBAs monitor SQL DMVs manually via SSMS
- AX admins check batch jobs through AX client (slow UI)
- No unified view of AX + SQL performance
- Alerts come from users, not systems
- No historical trending for proactive capacity planning

**Desired State:**
- Single pane of glass for AX performance health
- Proactive alerts before users are impacted
- Drill-down capability to root cause within seconds
- Historical trends for capacity planning
- Self-service for Level 1 support teams

---

## 3. Target Users & Personas

### 3.1 Primary Persona: AX Operations Engineer (Max)
**Background:**
- Manages 3 AX environments (DEV, TST, PRD)
- On-call rotation for production issues
- Reports to IT Operations Manager

**Goals:**
- Detect batch backlogs before business users notice
- Quickly identify which AOS instance is causing problems
- Correlate AX batch issues with SQL performance
- Provide evidence-based reports to management

**Pain Points:**
- Switching between AX client, SSMS, and monitoring tools wastes time
- No visibility during off-hours until paged
- Difficult to explain root cause to non-technical stakeholders
- Can't predict when next performance crisis will occur

**Success Metrics:**
- Reduce mean time to detect (MTTD) from 30+ minutes to < 2 minutes
- Reduce mean time to diagnose (MTTD) from 2 hours to < 15 minutes

---

### 3.2 Secondary Persona: Database Administrator (Sarah)
**Background:**
- Manages SQL Server 2016 infrastructure for multiple applications
- AX is one of 15+ databases under management
- Limited AX application knowledge

**Goals:**
- Understand AX-specific SQL load patterns
- Identify expensive queries originating from AX
- Proactively manage TempDB and blocking issues

**Pain Points:**
- SQL waits don't tell her which AX business process is affected
- Can't distinguish normal AX load from problems
- Blamed for AX slowness even when it's application-side

**Success Metrics:**
- Reduce false escalations from AX team by 60%
- Provide AX-context with SQL diagnostics

---

### 3.3 Tertiary Persona: AX System Administrator (Tom)
**Background:**
- Functional AX expert, limited SQL knowledge
- Manages batch job schedules and configurations
- First escalation point for business users

**Goals:**
- See batch job health without logging into AX client
- Understand which batch classes are problematic
- Provide business users with realistic ETAs

**Pain Points:**
- AX client is slow and crashes frequently
- No historical view of batch performance trends
- Difficult to prove that infrastructure (not configuration) is the issue

**Success Metrics:**
- Access batch status in < 3 seconds (vs. 45+ seconds in AX client)
- Export reports for business stakeholders

---

## 4. Business Goals & Success Metrics

### 4.1 Primary Business Goals
1. **Reduce Operational Downtime**
   - Target: 40% reduction in performance-related incidents within 6 months
   - Baseline: Currently 12-15 incidents/month averaging 2.5 hours resolution time

2. **Improve Operational Efficiency**
   - Target: 60% reduction in time spent on performance troubleshooting
   - Baseline: Operations team spends ~15 hours/week on reactive troubleshooting

3. **Enable Proactive Management**
   - Target: 80% of issues detected by system alerts before user reports
   - Baseline: Currently 90% of issues reported by users first

### 4.2 Key Performance Indicators (KPIs)

| Metric | Baseline | Target (6 months) | Measurement Method |
|--------|----------|-------------------|-------------------|
| Mean Time to Detect (MTTD) | 30 min | < 2 min | Alert timestamp vs. issue occurrence |
| Mean Time to Diagnose (MTTD) | 2 hours | < 15 min | Issue reported to root cause identified |
| Alert Accuracy | N/A | > 90% | True positives / Total alerts |
| Dashboard Load Time | N/A | < 3s (p95) | Application performance monitoring |
| Data Freshness | N/A | < 60s (p95) | Last update timestamp |
| User Adoption | 0 | 15+ active users | Login analytics |

---

## 5. MVP Scope Definition

### 5.1 MVP Core Features (MUST HAVE)

#### F1: Real-Time Batch Monitoring
**User Story:** As an AX Operations Engineer, I want to see batch job backlog and execution status in real-time so I can identify stuck or failing jobs before business impact.

**Capabilities:**
- Live view of running, waiting, and failed batch jobs
- Backlog count per batch class/group
- Execution time trends (P50, P95, P99)
- Drill-down to individual batch job instances
- CSV export for reporting

**Acceptance Criteria:**
- Data refreshed every 60-120 seconds
- P95 query latency < 1.5s
- Shows last 24 hours by default, filterable to 7 days

---

#### F2: Session & Blocking Monitoring
**User Story:** As a DBA, I want to visualize SQL blocking chains with AX context so I can quickly identify which business process is causing the block.

**Capabilities:**
- Active sessions per AOS instance
- Long-running transactions (> threshold)
- Blocking chains with blocker/victim relationships
- SQL text and AX context for blocked queries
- Historical blocking events (24h minimum)

**Acceptance Criteria:**
- Blocking detected within 10 seconds (p95)
- Shows AX username, session ID, and business process
- Graph/Sankey visualization for blocking chains

---

#### F3: SQL Health Dashboard
**User Story:** As an Operations Engineer, I want to see SQL Server health metrics alongside AX metrics so I can understand the full performance picture.

**Capabilities:**
- CPU, Memory, Disk I/O utilization
- TempDB usage and growth
- Top wait types with AX context
- Expensive queries (CPU, duration, reads)
- Deadlock capture and visualization

**Acceptance Criteria:**
- Standard view loads in < 2s
- Data cached appropriately (cache hit rate > 70%)
- Tooltips explain each metric

---

#### F4: Alerting & Notification System
**User Story:** As an AX Operations Engineer, I want to receive email alerts when performance thresholds are breached so I can respond before users are impacted.

**Capabilities:**
- Rule-based alerting (thresholds, baselines, windows)
- Email notifications (SMTP)
- Alert deduplication (30 min suppression)
- Maintenance window configuration
- Alert inbox with acknowledge/snooze actions

**Acceptance Criteria:**
- Configurable thresholds per environment
- Maximum 1 alert per 15 minutes per alert type
- Alert delivery within 60 seconds (p95)
- No alerts during maintenance windows

---

#### F5: Overview Dashboard
**User Story:** As any user, I want a single overview page showing all critical KPIs so I can assess system health in < 10 seconds.

**Capabilities:**
- KPI tiles: Batch backlog, error rate, active sessions, blocking count, CPU/IO
- Color-coded health indicators (green/yellow/red)
- Last update timestamp
- Quick navigation to detail pages

**Acceptance Criteria:**
- Page loads in < 3s (p95)
- Data age < 60s (p95)
- Responsive design (desktop focus)

---

### 5.2 Scope Boundaries (OUT OF SCOPE for MVP)

**Explicitly Excluded:**
- ❌ Write operations (read-only monitoring only)
- ❌ Automated remediation (e.g., killing sessions)
- ❌ Multi-tenant / multi-organization support
- ❌ Mobile app or responsive mobile UI
- ❌ Integration with ITSM tools (ServiceNow, Jira)
- ❌ Advanced ML/AI anomaly detection
- ❌ Custom user dashboards or widgets
- ❌ SSO/Active Directory integration (simple auth for MVP)
- ❌ Audit trail of user actions (basic logging only)
- ❌ API for external integrations
- ❌ Real-time alerting via Teams/Slack (email only)

**Future Enhancements (Post-MVP):**
- Phase 2: Anomaly detection with baseline learning
- Phase 2: Teams/Slack webhook integration
- Phase 2: Scheduled PDF reports
- Phase 3: API for ITSM integration
- Phase 3: Custom dashboard builder
- Phase 3: Multi-environment topology view

---

### 5.3 MVP Validation Approach

**Success Criteria for MVP:**
1. At least 10 active users across 3 personas within 4 weeks
2. 70% of users report "faster troubleshooting" in survey
3. 5+ incidents detected by alerts before user reports
4. Zero critical bugs in production after 2 weeks
5. Dashboard performance meets SLOs (< 3s p95)

**Validation Method:**
- 2-week pilot with 5 power users in TEST environment
- 4-week production trial with full operations team
- Weekly feedback sessions
- Post-incident reviews comparing "with vs. without dashboard"

**Learning Goals:**
- Which visualizations are most valuable?
- What alert thresholds minimize false positives?
- What additional AX-specific metrics are needed?

**Timeline Expectations:**
- Sprint 1-2: Core infrastructure + Session monitoring
- Sprint 3-4: Batch monitoring + Basic alerting
- Sprint 5-6: SQL Health + Full alerting + Stabilization
- Sprint 7: Pilot deployment + feedback
- Sprint 8: Production rollout

---

## 6. User Experience Requirements

### 6.1 Primary User Flows

#### Flow 1: Morning Health Check (Daily Routine)
1. User opens dashboard URL
2. Landing page shows Overview with all KPIs
3. User scans color-coded health indicators (< 10s)
4. If issues detected, clicks through to detail page
5. Exports data for status report if needed

**Success Criteria:** Complete flow in < 2 minutes

---

#### Flow 2: Alert Response (Incident)
1. User receives email alert: "Batch backlog threshold exceeded"
2. Email contains link to relevant dashboard page
3. User clicks link, sees Batch page filtered to problem
4. Drills down to specific batch job instances
5. Identifies root cause, takes action outside system
6. Acknowledges alert in Alert Inbox

**Success Criteria:** Time from alert to root cause < 5 minutes

---

#### Flow 3: Performance Investigation (Troubleshooting)
1. User reports slowness at 10:00 AM
2. Engineer opens Sessions or Blocking page
3. Sets time filter to 09:45-10:15
4. Identifies blocking chain active at 09:58
5. Views SQL text and AX context
6. Exports details for post-incident report

**Success Criteria:** Complete investigation in < 15 minutes

---

### 6.2 Usability Requirements

**Accessibility:**
- WCAG 2.1 Level A compliance minimum
- Keyboard navigation for all primary actions
- High-contrast mode support

**Platform Compatibility:**
- Desktop browsers: Chrome, Edge, Firefox (last 2 versions)
- Minimum resolution: 1280x720
- No mobile support required for MVP

**Performance Expectations:**
- Initial page load: < 3s (p95)
- Page navigation: < 1s
- Data refresh: < 2s
- Export generation: < 5s

**Error Handling:**
- Graceful degradation if SQL connection lost
- Clear error messages with next steps
- Automatic retry with exponential backoff
- Admin health check page shows component status

---

### 6.3 UI/UX Principles

**Information Architecture:**
- Navbar: Overview | Batch | Sessions | Blocking | SQL Health | Alerts | Admin
- Global filters: Time range, Environment, AOS instance
- Filter state persists during navigation

**Visual Design:**
- Modern, clean interface using Streamlit best practices
- Color coding: Green (healthy), Yellow (warning), Red (critical)
- Charts: Plotly for interactive visualizations
- Tables: Sortable, searchable, paginated

**Content Strategy:**
- Tooltips explain technical terms
- Timestamps always visible (absolute + relative)
- Empty states guide user to next action

---

## 7. Functional Requirements

### 7.1 Batch Monitoring Requirements

**FR-B1:** System SHALL capture batch job status every 60-120 seconds  
**FR-B2:** System SHALL calculate P50, P95, P99 execution times per batch class  
**FR-B3:** System SHALL display backlog count per batch class/group  
**FR-B4:** System SHALL show error rate (failed/total) per batch class  
**FR-B5:** System SHALL enable drill-down to individual job instances with full parameters  
**FR-B6:** System SHALL support CSV export of filtered batch data  
**FR-B7:** System SHALL retain 30 days of detailed batch data, 12 months of aggregates  

---

### 7.2 Session & Blocking Requirements

**FR-S1:** System SHALL capture session snapshots every 30-60 seconds  
**FR-S2:** System SHALL classify sessions as Active/Inactive per AOS  
**FR-S3:** System SHALL identify long-running transactions exceeding configurable threshold  
**FR-S4:** System SHALL detect blocking chains in < 10s (p95)  
**FR-S5:** System SHALL visualize blocking relationships as graph or Sankey diagram  
**FR-S6:** System SHALL display SQL text for blocker and victim  
**FR-S7:** System SHALL show AX username, session ID, and database context  
**FR-S8:** System SHALL retain 24 hours of session detail, 7 days of blocking events  

---

### 7.3 SQL Health Requirements

**FR-H1:** System SHALL monitor CPU, Memory, Disk I/O every 1-5 minutes  
**FR-H2:** System SHALL capture top wait types with configurable sample interval  
**FR-H3:** System SHALL identify top expensive queries (CPU, duration, reads)  
**FR-H4:** System SHALL monitor TempDB usage and growth  
**FR-H5:** System SHALL capture deadlocks via SQL Server Extended Events  
**FR-H6:** System SHALL make deadlock details visible within 10 seconds  
**FR-H7:** System SHALL cache heavy queries with > 70% hit rate  

---

### 7.4 Alerting Requirements

**FR-A1:** System SHALL support threshold-based rules (absolute values)  
**FR-A2:** System SHALL support baseline-based rules (percentage deviation)  
**FR-A3:** System SHALL send alerts via SMTP email  
**FR-A4:** System SHALL deduplicate alerts with 30-minute suppression window  
**FR-A5:** System SHALL rate-limit to max 1 alert per 15 min per alert type  
**FR-A6:** System SHALL respect configured maintenance windows (no alerts)  
**FR-A7:** System SHALL provide Alert Inbox with filter, acknowledge, snooze actions  
**FR-A8:** System SHALL deliver alerts within 60 seconds (p95) of threshold breach  
**FR-A9:** System SHALL include dashboard deep-link in alert emails  

---

### 7.5 Admin & Configuration Requirements

**FR-C1:** System SHALL load configuration from environment variables (.env)  
**FR-C2:** System SHALL support multiple environments (DEV/TST/PRD) with env-specific configs  
**FR-C3:** System SHALL provide Admin page for threshold configuration  
**FR-C4:** System SHALL provide "Test Alert" function for validation  
**FR-C5:** System SHALL display system health status (DB connection, scheduler, email)  
**FR-C6:** System SHALL log all admin configuration changes with timestamp and user  

---

## 8. Non-Functional Requirements

### 8.1 Performance Requirements

| Requirement | Target | Measurement |
|-------------|--------|-------------|
| Dashboard page load (p95) | < 3s | Browser performance API |
| Data freshness (p95) | < 60s | Timestamp comparison |
| Ingestion job latency (p95) | < 10s | Job completion time |
| Ingestion job duration (p95) | < 1.5s | Job execution time |
| SQL query execution (p95) | < 1.0s | Query performance counters |
| Cache hit rate (heavy queries) | > 70% | Cache metrics |
| Service error rate (p95) | < 0.1% | Error logging |

### 8.2 Scalability Requirements

**Data Volume:**
- Support 3 AX environments concurrently
- Up to 10 AOS instances per environment
- Up to 500 concurrent AX sessions per environment
- Up to 1000 batch jobs tracked simultaneously

**User Load:**
- Support 20 concurrent dashboard users
- 100 page views per minute peak load

**Data Retention:**
- 30 days detailed metrics (row-level)
- 12 months aggregated metrics (daily/hourly)

---

### 8.3 Security Requirements

**Authentication:**
- Simple login (username/password) for MVP
- Role-based access control: Viewer, Power-User, Admin
- Session timeout: 8 hours

**Authorization:**
- Viewers: Read-only access to all dashboards
- Power-Users: Can acknowledge/snooze alerts
- Admins: Can modify thresholds and configurations

**Data Protection:**
- Database credentials stored in environment variables
- No secrets in code or repository
- SQL injection prevention via parameterized queries
- HTTPS required for production deployment (infrastructure)

**Audit:**
- Log all login events
- Log all admin configuration changes
- Log all alert status changes (acknowledge/snooze)

---

### 8.4 Reliability Requirements

**Availability:**
- Target: 98% uptime during business hours (6 AM - 10 PM local time)
- Planned maintenance windows: Weekly, off-hours

**Fault Tolerance:**
- Scheduler jobs retry with exponential backoff (max 2 min)
- Transient SQL errors handled gracefully
- Data collection continues even if alerting fails

**Data Quality:**
- Batch ingestion miss rate < 0.1% (due to timing windows)
- Alert duplicate rate < 1%

**Recovery:**
- Manual restart procedure documented
- Application restarts automatically on crash (Windows Service or supervisor)

---

### 8.5 Operational Requirements

**Deployment:**
- Windows Server or Windows 10/11 workstation
- Python 3.10+ required
- SQL Server ODBC Driver 17/18 required
- Runs as Windows Service or scheduled task

**Monitoring:**
- Health check endpoint/function for external monitoring
- Structured logs (JSON optional) with correlation IDs
- Error logs retained for 30 days

**Backup & Recovery:**
- Configuration backed up with environment configs
- Staging database backed up daily (same as AX DB backup strategy)

**Maintenance:**
- Data retention cleanup runs nightly
- No application downtime required for threshold changes

---

## 9. Technical Constraints & Assumptions

### 9.1 Technical Constraints

**Database:**
- Must use SQL Server 2016 (customer environment constraint)
- Read-only access to AX production database
- Separate staging database for collected metrics

**Connectivity:**
- Application host must reach SQL Server on port 1433
- SMTP server must be accessible for email alerts
- No internet connectivity required (air-gapped deployment possible)

**Technology Stack:**
- Python 3.10+ (Streamlit compatibility)
- Streamlit framework for UI
- APScheduler for job scheduling
- ODBC/pymssql for SQL connectivity

**Dependencies:**
- pyodbc or pymssql
- pandas, plotly
- APScheduler
- python-dotenv, cachetools, structlog

---

### 9.2 Assumptions

**Infrastructure:**
- ✅ Database read-only user will be provisioned with SELECT on AX tables + DMVs
- ✅ Network firewall rules will allow app host → SQL Server connectivity
- ✅ SMTP relay available for alert emails
- ✅ ODBC Driver 17/18 can be installed on app host
- ⚠️ **Risk:** AX schema follows standard Microsoft schema (minimal customizations)

**Operational:**
- ✅ Operations team has capacity to pilot and provide feedback
- ✅ Testing environments (DEV, TST) available for validation
- ⚠️ **Risk:** Extended Events permissions for deadlock capture may require DBA assistance

**Data Quality:**
- ✅ AX tables (BATCHJOB, BATCH, SYSCLIENTSESSIONS) are reliable sources
- ⚠️ **Risk:** Customer-specific table extensions may require configuration mapping

---

## 10. Dependencies & Risks

### 10.1 Critical Dependencies

| Dependency | Description | Owner | Status | Risk |
|------------|-------------|-------|--------|------|
| DB Read-Only User | SELECT on AX tables + SQL DMVs | DBA Team | 🔴 Pending | High |
| Network Access | Firewall rules: App host → SQL Server | Network Team | 🔴 Pending | High |
| ODBC Driver | Driver 17/18 installed on app host | Ops Team | 🟡 In Progress | Medium |
| SMTP Relay | Email alerts functional | Email Team | 🔴 Pending | Medium |
| Extended Events Access | Read XE files for deadlocks | DBA Team | 🟡 Negotiating | Low |

---

### 10.2 Risks & Mitigations

**Risk 1: AX Schema Customizations** (Medium)
- **Impact:** SQL queries fail or return incorrect data
- **Mitigation:** Configuration file for table/field name mapping
- **Contingency:** Manual schema mapping during pilot phase

**Risk 2: SQL Server Load** (Medium)
- **Impact:** Monitoring queries impact production performance
- **Mitigation:** 
  - Queries optimized with TOP, time filters, indexes recommended
  - Configurable sample intervals
  - Query timeout limits
- **Contingency:** Disable heavy queries via feature toggle

**Risk 3: Alert Fatigue** (Medium)
- **Impact:** Operations team ignores alerts
- **Mitigation:**
  - Pilot phase to tune thresholds
  - Baseline-based alerting reduces false positives
  - Deduplication and throttling built-in
- **Contingency:** Start with email-only to small group, expand gradually

**Risk 4: Delayed Permissions** (High)
- **Impact:** Development blocked waiting for DB access
- **Mitigation:**
  - Develop against TEST environment first
  - Mock data for unit tests
  - Parallel track: DBA escalation path
- **Contingency:** Extend timeline if critical path blocked

---

## 11. Stakeholder & Communication Plan

### 11.1 Stakeholders

| Role | Name | Responsibility | Involvement |
|------|------|----------------|-------------|
| Product Owner | TBD | Prioritization, acceptance | High |
| Tech Lead | TBD | Architecture, dev guidance | High |
| DBA Team Lead | TBD | SQL permissions, optimization | Medium |
| Ops Manager | TBD | User acceptance, rollout | High |
| Security Lead | TBD | Security review, compliance | Low |

### 11.2 Communication Cadence

- **Weekly Status Update:** Stakeholder email with progress, blockers, next steps
- **Bi-Weekly Demo:** Show working features, gather feedback
- **Monthly Steering:** Executive update on milestones, risks, budget
- **Slack/Teams Channel:** Daily async updates and questions

### 11.3 Decision Framework

**Decision Authority:**
- **Product Owner:** Scope, priority, acceptance criteria
- **Tech Lead:** Technology choices, architecture
- **Security Lead:** Security requirements, exceptions
- **Ops Manager:** Deployment timing, rollout plan

---

## 12. Acceptance Criteria (PRD Complete)

This PRD is considered complete and ready for architecture phase when:

- ✅ Problem statement validated by 3+ operations engineers
- ✅ User personas reviewed by actual users
- ✅ MVP scope approved by Product Owner
- ✅ Business goals and KPIs measurable and agreed
- ✅ All dependencies identified with owners assigned
- ✅ Stakeholder communication plan established
- ✅ Architect confirms PRD provides sufficient detail for design

---

## 13. Approval & Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Product Owner | TBD | TBD | _________ |
| Tech Lead | TBD | TBD | _________ |
| Ops Manager | TBD | TBD | _________ |

---

## Appendix A: Glossary

- **AOS:** Application Object Server (AX runtime server)
- **AX:** Microsoft Dynamics AX 2012 R3
- **Blocking Chain:** Series of SQL sessions where session A blocks B, B blocks C, etc.
- **DMV:** Dynamic Management View (SQL Server diagnostic views)
- **MVP:** Minimum Viable Product
- **P50/P95/P99:** 50th, 95th, 99th percentile performance metrics
- **TempDB:** SQL Server temporary database for intermediate results

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-23 | System | Initial PRD creation from DEV_TODOS |

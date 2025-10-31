## KPI Baseline & Measurement Plan

Scope
- 14-day rolling baseline per environment (DEV/TST/PRD)
- Metrics: Batch duration P50/P95/P99 per class, error rate, backlog, sessions, waits, top queries

Storage
- Daily aggregates in `fact_*_daily`
- Optional `baseline_*` tables storing computed percentiles and thresholds

Computation
- Nightly job computes percentiles; on schema changes recompute window

Usage
- Alerts use baseline + offset (e.g., +30%)
- Dashboards display baseline overlays for trends


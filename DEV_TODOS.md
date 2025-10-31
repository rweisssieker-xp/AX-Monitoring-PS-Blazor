## AX 2012 R3 Performance Leak Monitor – Dev ToDos (Streamlit + Python)

Kurzbeschreibung: Umsetzung des PRD für eine Streamlit-App, die Performance-Leaks von AX 2012 R3 (CU) auf SQL Server 2016 überwacht (Batch, Sessions, Blocking, SQL-Health). Fokus: Read-only, schnelle Dashboards, Alerts, Drilldowns.

### Globales DoD (Definition of Done)
- Funktionalität erfüllt Akzeptanzkriterien des jeweiligen Tasks
- Unit-Tests (≥80% für Kernlogik), Smoke-Tests für Integrationspfade
- Typprüfung ohne Fehler (mypy), Lint/Format sauber (ruff/black)
- Konfigurierbar via `.env`; keine Secrets im Code/Repo
- Log-Ausgaben strukturiert; Fehler werden erfasst und mit Kontext geloggt
- Dokumentation (README/HowTo im passenden Ordner) aktualisiert

---

### Epic 0 – Projekt-Setup & Qualitätssicherung
- [ ] Projekt-Gerüst erstellen (Streamlit, Python 3.10+, Poetry/Pip)
  - Akzeptanz: `streamlit run app.py` startet leere Multi-Page-App
- [ ] Abhängigkeiten definieren (`pyodbc` oder `pymssql`, `pandas`, `plotly`, `APScheduler`, `python-dotenv`, `cachetools`, `structlog`)
  - Akzeptanz: `pip install -r requirements.txt` erfolgreich
- [ ] Code-Qualität: ruff, black, mypy, pre-commit Hooks
  - Akzeptanz: `pre-commit run --all-files` grün
- [ ] Ordnerstruktur festlegen (`app/`, `app/pages/`, `app/services/`, `app/sql/`, `app/scheduler/`, `app/ui/`, `tests/`)
  - Akzeptanz: Struktur dokumentiert und konsistent verwendet

### Epic 1 – Konfiguration & Secrets
- [ ] `.env` Handhabung implementieren (mehrere Umgebungen DEV/TST/PRD)
  - Akzeptanz: Umschalten per ENV-Var; Fallback-Doku vorhanden
- [ ] DB-Connection-String mit ODBC Driver 17/18 (SQL Server 2016 kompatibel)
  - Akzeptanz: Health-Check gegen DB mit Read-only User funktioniert

Beispiel `.env`:
```env
APP_ENV=DEV
DB_DRIVER=ODBC Driver 17 for SQL Server
DB_SERVER=sqlserver01\INST
DB_DATABASE=AX2012R3
DB_USER=ax_ro
DB_PASSWORD=***
SMTP_HOST=smtp.local
SMTP_PORT=25
``` 

### Epic 2 – DB-Zugriffsschicht (Read-only)
- [ ] Verbindungsmanager (Pooling, Timeout, Retries, Health-Check)
  - Akzeptanz: Wiederverwendung von Verbindungen; Timeout/Fehler sauber geloggt
- [ ] Sichere Abfrage-Hilfen (parametrisierte SQLs, zentrale Fehlerbehandlung)
  - Akzeptanz: Keine String-Konkatenation für SQL-Parameter
- [ ] Feature-Toggles `pyodbc` vs `pymssql`
  - Akzeptanz: Laufzeit-umschaltbar via Konfiguration

### Epic 3 – Datenmodell Staging/Reporting
- [ ] DDL-Skripte für Minimal-Schema (Dimensions/Facts laut PRD)
  - Akzeptanz: Skripte laufen auf SQL Server 2016; Idempotenz
- [ ] Migrationstool (einfaches Python-Skript) für DDL-Versionierung
  - Akzeptanz: Versions-Tabelle, Up/Down-Doku vorhanden
- [ ] Retention-Strategie (30 Tage Detail, 12 Monate Aggregate)
  - Akzeptanz: Geplante Bereinigung/Archivierung getestet

### Epic 4 – Ingestion & Scheduler (APScheduler)
- [ ] Job: Sessions/Blocking Snapshot (30–60s)
  - Akzeptanz: Snapshot persistiert; p95 Laufzeit < 1.0s; Erfassungslatenz p95 < 10s
- [ ] Job: Batch-Status Pull (60–120s) + 5-min Aggregationen
  - Akzeptanz: Aggregationstabellen befüllt; Idempotenz; p95 Laufzeit < 1.5s; Miss-Rate < 0.1%
- [ ] Job: SQL-Health/Waits (1–5 min), schwere Checks seltener
  - Akzeptanz: Sampling konfigurierbar; keine DB-Lastspitzen; p95 Laufzeit < 1.5s
- [ ] Deadlock-Capture via Extended Events (SQL 2016)
  - Akzeptanz: XE-Session aktiv; Deadlocks innerhalb 10s sichtbar; RO-Zugriff ohne erhöhte Rechte dokumentiert
- [ ] Retry/Backoff, De-Duplizierung pro Key
  - Akzeptanz: Kein Alert-/Daten-Duplikat bei transienten Fehlern; Exponentielles Backoff bis max 2m

### Epic 5 – Service-Layer (Abfragen & Business-Logik)
- [ ] Batch: offene/runnende/fehlgeschlagene Jobs, Laufzeiten, Backlog je Klasse
  - Akzeptanz: Funktionen liefern P50/P95/P99, Fehlerraten und Top-N; Service-Fehlerrate p95 < 0.1%
- [ ] Sessions: Aktiv/Inactive, je AOS, lange Transaktionen
  - Akzeptanz: Schwellen für „lange TX“ konfigurierbar; Service-Fehlerrate p95 < 0.1%
- [ ] Blocking/Deadlocks: Chains, Tiefe, Dauer, betroffene Objekte
  - Akzeptanz: Blocker/Victim inkl. SQL-Text abrufbar; Erkennungszeit p95 < 10s
- [ ] SQL-Health: CPU, IO, TempDB, Waits, Top Queries
  - Akzeptanz: Standard-Ansicht < 2s; Queries gecached; Cache-Hitrate schwerer Queries > 70%
- [ ] Gemeinsame Caching-Hilfen (TTL, Key-Strategie)
  - Akzeptanz: Cache invalidierbar; TTL pro Datenart; Hit-/Miss-Metriken verfügbar

### Epic 6 – Alerting
- [ ] Regel-Engine: Schwellen, Fenster, Baselines (einfach)
  - Akzeptanz: Mind. 5 Regeln aus PRD abbildbar; Baselines konfigurierbar; Datenalter p95 < 60s
- [ ] Versand: SMTP-E-Mail (Plain/HTML), dedizierte Alert-Templates
  - Akzeptanz: Test-E-Mail aus Admin-Seite möglich
- [ ] De-Duplizierung/Throttling (pro Ursache/Zeitraum)
  - Akzeptanz: Dedupe-Key = (Typ+Key); Suppression 30m; max 1 Alert/15m je Key
- [ ] Maintenance Windows (konfigurierbar)
  - Akzeptanz: Keine Alerts in Wartungsfenstern; UI-Hinweis während Fenster
- [ ] Alert-Inbox UI (Bestätigen, Stummschalten, Filter)
  - Akzeptanz: Statuswechsel persistiert, Filterbar

### Epic 7 – UI (Streamlit) Seiten & Komponenten
- [ ] UI-Shell: Navbar/Tabs, globale Filter (Zeitraum, Umgebung, AOS)
  - Akzeptanz: Filter wirken global; Zustand behält sich bei Navigation
- [ ] Overview: KPI-Kacheln (Backlog, Fehlerquote, Sessions, Blockings, CPU/IO)
  - Akzeptanz: p95 Ladezeit < 3s; Datenalter p95 < 60s; Zeitstempel sichtbar
- [ ] Batch-Seite: Backlog, Laufzeit-Verteilungen (Boxplot), Fehlerraten, Drilldown
  - Akzeptanz: Drilldown bis Auftragsinstanz; Export CSV; p95 Ladezeit < 3s
- [ ] Sessions-Seite: Aktiv/Inactive, pro AOS, lange Transaktionen
  - Akzeptanz: Sortier-/Suchbare Tabellen; Charts; p95 Ladezeit < 3s
- [ ] Blocking-Seite: Chains als Graph/Sankey, Details mit SQL-Text
  - Akzeptanz: Aktive Chains prominent; Historie filterbar; Erkennung < 10s
- [ ] SQL-Health-Seite: CPU, IO, TempDB, Waits, Top Requests
  - Akzeptanz: Top-N konfigurierbar; Tooltips mit Metrik-Definitionen; p95 Ladezeit < 3s
- [ ] Alerts-Seite: Liste, Statuswechsel, Detail-Panel
  - Akzeptanz: Paginierung, Export
- [ ] Admin-Seite: Datenquellen, Schwellen, Test-Alerts, Health
  - Akzeptanz: Änderungen gespeichert, validiert, protokolliert

### Epic 8 – Sicherheit & RBAC
- [ ] App-Login (einfach), Rollen: Viewer, Power-User, Admin
  - Akzeptanz: Rollen erzwingen Sicht/Handlung (z. B. Admin-only in Admin-Seite)
- [ ] Rollen-Matrix (Seiten/Aktionen)
  - Akzeptanz: Tabelle dokumentiert; Zugriffspfad getestet
- [ ] Audit-Events (Login, Policy-Änderung, Alert-Statuswechsel)
  - Akzeptanz: Audit-Log erfasst Events mit User, Zeit, Kontext
- [ ] Optional: AD/SSO Konzept skizzieren, Feature-Toggle
  - Akzeptanz: Dokumentierte Vorgehensweise, nicht zwingend im MVP
- [ ] Maskierung sensibler Felder (z. B. Login-Namen) optional
  - Akzeptanz: Togglebar pro Umgebung

### Epic 9 – Observability
- [ ] Strukturierte Logs (JSON optional) mit Korrelation (Job-ID/Request-ID)
  - Akzeptanz: Fehlerursachen nachvollziehbar, Query-Dauer geloggt
- [ ] Health-Check Endpunkte/Funktionen (DB, Scheduler, E-Mail)
  - Akzeptanz: Admin-Seite zeigt Status-Widgets

### Epic 10 – Tests & Datenqualität
- [ ] Unit-Tests Service-Layer (SQL-Mocks, Parametrisierung)
  - Akzeptanz: ≥80% Coverage kritischer Pfade
- [ ] Integrations-Tests gegen Test-DB (Read-only)
  - Akzeptanz: Reproduzierbare Daten-Fixtures; Cleanup vorhanden
- [ ] Synthetic Fixtures & Smoke Tests
  - Akzeptanz: `tests/fixtures/` vorhanden; Smoke-Skript prüft Kernpfade erfolgreich
- [ ] Performance-Tests (Ladezeit Dashboards, Scheduler-Laufzeit)
  - Akzeptanz: Overview p95 < 3s; Ingestion-Jobs p95 < 1.5s; Fehlerbudget < 0.1%

### Epic 11 – CI/CD & Deployment
- [ ] Build-Pipeline (Lint, Tests, Paket bauen)
  - Akzeptanz: Pipeline grün; Artefakt erzeugt
- [ ] Deployment: Windows-Dienst oder Container (optional)
  - Akzeptanz: Start/Stop/Restart dokumentiert; Logs zugreifbar
- [ ] Konfigurationsstrategie DEV/TST/PRD (Secrets, `.env`, Azure Key Vault optional)
  - Akzeptanz: Secrets nie im Repo; Rotations-Doku
- [ ] Runbooks in `docs/` (deploy, rollback, alert-triage)
  - Akzeptanz: Dateien vorhanden und verlinkt

---

### Abhängigkeiten & Risiken (kurz)
- DB-Read-Only User und Rechte für AX-Tabellen + DMVs (Ops abhängig)
- Netzwerkzugriff Streamlit-Host → SQL Server 2016
- AX-Kundenerweiterungen: Tabellen-/Feldnamen variieren → Mapping konfigurierbar

### Minimale Roadmap (Vorschlag)
- Sprint 1: Setup, DB-Zugriff, Overview Skeleton, Sessions Ingestion, Basic Health
- Sprint 2: Batch Ingestion + Seite, Blocking/Chains, Caching
- Sprint 3: Alerts (Regeln + E-Mail), Admin-Seite, Tests/CI, Stabilisierung

### Anhang: Kern-SQL (als Referenz; ggf. anpassen)
```sql
-- Blocking (aktuell)
SELECT wt.blocking_session_id AS blocker_spid,
       er.session_id          AS victim_spid,
       er.wait_type,
       er.wait_time           AS wait_ms,
       DB_NAME(er.database_id) AS db_name
FROM sys.dm_exec_requests er
JOIN sys.dm_os_waiting_tasks wt
  ON er.session_id = wt.session_id
WHERE wt.blocking_session_id IS NOT NULL
ORDER BY er.wait_time DESC;
```

```sql
-- Sessions je AOS (AX-Tabellen ggf. kundenspezifisch)
SELECT SERVERID AS aos_name,
       SUM(CASE WHEN STATUS = 1 THEN 1 ELSE 0 END) AS active_sessions,
       SUM(CASE WHEN STATUS <> 1 THEN 1 ELSE 0 END) AS inactive_sessions
FROM SYSCLIENTSESSIONS
GROUP BY SERVERID;
```

```sql
-- Batch-Status (vereinfachtes Beispiel)
SELECT TOP 200
  J.RECID, J.CAPTION AS job_name, J.STATUS, T.SERVERID, T.BATCHCLASSNAME, T.EXECUTESTARTDATETIME
FROM BATCHJOB J
JOIN BATCH T ON T.BATCHJOBID = J.RECID
WHERE J.STATUS IN (1, 3, 4) -- Waiting, Executing, Error
ORDER BY T.EXECUTESTARTDATETIME DESC;
```

### PM-Checklist (Owner / Due-Date Platzhalter)
- [ ] Scope & Out-of-Scope bestätigt (Owner: <Name>, Due: <Datum>)
- [ ] Annahmen dokumentiert und messbar (Owner: <Name>, Due: <Datum>)
- [ ] RACI definiert (PO/Tech Lead/DBA/Ops/Security) (Owner: <Name>, Due: <Datum>)
- [ ] Milestones & Timeline fixiert (Owner: <Name>, Due: <Datum>)
- [ ] Budget & Kapazität freigegeben (Owner: <Name>, Due: <Datum>)
- [ ] Stakeholder-/Kommunikationsplan (Cadence, Kanäle) (Owner: <Name>, Due: <Datum>)
- [ ] RAID-Log erstellt und gepflegt (Owner: <Name>, Due: <Datum>)
- [ ] UAT-Plan, Abnahme, Go-Live-Checkliste, Hypercare, Rollback (Owner: <Name>, Due: <Datum>)
- [ ] Change-/Release-Management (Wartungsfenster, CAB) (Owner: <Name>, Due: <Datum>)
- [ ] Security/Compliance (Datenklass., DPIA, Audit/Retention, Secrets) (Owner: <Name>, Due: <Datum>)
- [ ] SLI/SLO/SLA & Supportmodell (On-Call, Eskalation) (Owner: <Name>, Due: <Datum>)
- [ ] KPI-Baseline & Messplan (Vorher/Nachher) (Owner: <Name>, Due: <Datum>)
- [ ] Environment-Topologie & Kapazitätsplanung (Owner: <Name>, Due: <Datum>)
- [ ] Daten-Governance & Exporte (Owner: <Name>, Due: <Datum>)
- [ ] Training/Enablement (User/Support/Admin) (Owner: <Name>, Due: <Datum>)
- [ ] Abhängigkeiten/Tickets (SMTP, AD/SSO, Firewall, DB-User, ODBC) (Owner: <Name>, Due: <Datum>)
- [ ] Doku vollständig (Architektur, Betrieb, Troubleshooting, Release Notes) (Owner: <Name>, Due: <Datum>)
- [ ] Quality Gates (NFR-Tests, DoR/DoD je Epic) (Owner: <Name>, Due: <Datum>)

### RAID-Log (Template)
| Typ | Titel | Beschreibung | Impact | Owner | Due | Status |
|---|---|---|---|---|---|---|
| Risk | DB-ReadOnly-User fehlt | Rechte auf AX-Tabellen/DMVs noch offen | Hoch | <Name> | <Datum> | Offen |
| Assumption | AX-Schema Standard | Keine kundenspezifischen Abweichungen | Mittel | <Name> | <Datum> | Geprüft |
| Issue | Firewall blockiert DB | Streamlit-Host erreicht SQL nicht | Hoch | <Name> | <Datum> | In Arbeit |
| Dependency | SMTP/Teams-Webhook | Alert-Ausleitung benötigt Freigaben | Mittel | <Name> | <Datum> | Offen |

### Competitive Landscape (Kurzüberblick)

#### Feature-Parität (Minimum für MVP)
- Live-Blocking/Deadlock-Graph inkl. SQL-Text + Historie
- Wait-Stats & Top Queries mit 24h/7d-Trends und Baseline-Abweichung
- Batch-Backlog, Laufzeit P50/P95 je Klasse, Fehlerrate, Instanz-Drilldown
- Alert-Dämpfung, Wartungsfenster, Deduplizierung, E-Mail/Teams Versand
- RBAC (Viewer/Power-User/Admin), optional AD/SSO; CSV/Excel-Export

#### Differenzierung (AX-spezifisch)
- Native Batch-Transparenz (Klassen, Gruppen, AOS-Zuordnung) und SLA-Heatmaps
- Sessions pro AOS, lange Transaktionen, Cross-Company-Sicht
- AX-Playbooks (Blocking-Waits, TempDB-Patterns, Index/Stats-Hinweise)

#### Phase-2 Erweiterungen
- Anomalieerkennung (Saisonalität/ML), Plan-Drift/Regressionserkennung
- Scheduled Reports, API/Webhooks, ITSM/ChatOps-Integrationen
- Multi-Env-/Topologie-Übersichten, End-to-End-Korrelation

### Abhängigkeiten (Owner/ETA)
| Dependency | Beschreibung | Owner | ETA | Status |
|---|---|---|---|---|
| DB Read-Only User | SELECT auf AX-Tabellen + DMVs | <Name> | <Datum> | Offen |
| Netzwerk/Firewall | Streamlit-Host → SQL Server 2016 | <Name> | <Datum> | Offen |
| ODBC Driver 17/18 | Installation auf App-Host | <Name> | <Datum> | Offen |
| SMTP/Teams | Alert-Kanäle freigeschaltet | <Name> | <Datum> | Offen |
| Deadlock XE Pfad | Ordnerrechte, Pfad für XE-Dateien | <Name> | <Datum> | Offen |

### RBAC Matrix (Seiten/Aktionen)
| Bereich | Aktion | Viewer | Power-User | Admin |
|---|---|---:|---:|---:|
| Overview | Anzeigen | ✓ | ✓ | ✓ |
| Batch | Anzeigen, Export | ✓ | ✓ | ✓ |
| Sessions | Anzeigen | ✓ | ✓ | ✓ |
| Blocking | Anzeigen, SQL-Text | ✓ | ✓ | ✓ |
| SQL Health | Anzeigen | ✓ | ✓ | ✓ |
| Alerts | Anzeigen | ✓ | ✓ | ✓ |
| Alerts | Bestätigen/Stummschalten |  | ✓ | ✓ |
| Admin | Datenquellen/Schwellen/Tests |  |  | ✓ |
| Admin | Benutzer/Rollen verwalten |  |  | ✓ |

Audit-Events: Login, Policy-Änderung, Alert-Statuswechsel, Admin-Konfig-Änderung.

### KPI Baseline & Messplan
- Baseline-Fenster: 14 Tage rollierend je Umgebung
- Speicherung: Tagesaggregate in `fact_*_daily` + optionale `baseline_*` Tabellen (P50/P95/P99)
- Erhebung: Nightly Aggregation; bei Schemawechsel Baseline neu berechnen
- Nutzung: Alert-Regeln referenzieren Baseline + Offset (z. B. +30%)

### Performance Budgets & Load Tests
- Budgets: Overview p95 < 3s; Datenalter p95 < 60s; Ingestion p95 < 1.5s; Fehlerbudget < 0.1%
- Tooling: einfacher Streamlit-Client-Skript (Threaded) + SQL Last-Queries begrenzen (TOP, Filter)
- Szenarien: 7-Tage Daten, 10 gleichzeitige Nutzer, 15 min Laufzeit
- Abnahme: p95 Ziele erreicht, keine DB-Lastspitzen (CPU/IO im grünen Bereich)



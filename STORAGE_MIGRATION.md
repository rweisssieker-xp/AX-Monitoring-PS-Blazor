# Storage Migration Guide

## Overview

Das AX Monitoring System wurde von Datenbank-basierter zu **Datei-basierter Speicherung** migriert.

### Warum File-Storage?

✅ **Read-Only Garantie für AX-DB** - Keine versehentlichen Schreibzugriffe auf Produktionsdatenbank
✅ **Einfaches Backup** - Files können einfach kopiert/gesichert werden
✅ **Keine DB-Lizenzen** - Keine zusätzliche Staging-Datenbank nötig
✅ **Git-freundlich** - Konfigurationen können versioniert werden
✅ **Performance** - CSV ist optimal für Zeitreihen-Daten

---

## Architektur

### Alte Architektur (DB-basiert)
```
AX-DB (Read) → Staging-DB (Read/Write) ← App
                    ↓
              INSERT/UPDATE/DELETE
```

### Neue Architektur (File-basiert)
```
AX-DB (Read-Only) → App → JSON/CSV Files
                           ↓
                     data/
                     ├── history/      (CSV - Metriken, Jobs, Sessions)
                     ├── alerts/       (JSON - Rules, Active Alerts)
                     ├── audit/        (JSONL - Audit-Events)
                     ├── dashboards/   (JSON - Dashboard-Configs)
                     └── analytics/    (JSON - RCA-Daten)
```

---

## File-Struktur

```
data/
├── history/
│   ├── metrics_2025-01.csv          # Metriken Januar 2025
│   ├── metrics_2025-02.csv          # Metriken Februar 2025
│   ├── batch_jobs_2025-01.csv       # Batch-Jobs Historie
│   ├── sessions_2025-01.csv         # Sessions Historie
│   ├── sql_health_2025-01.csv       # SQL Health Metriken
│   └── blocking_2025-01.csv         # Blocking-Events
│
├── alerts/
│   ├── rules.json                   # Alert-Regeln (config)
│   ├── active_alerts.json           # Aktive Alerts
│   └── history_2025-01.jsonl        # Alert-Historie (JSON Lines)
│
├── audit/
│   └── events_2025-01.jsonl         # Audit-Events
│
├── dashboards/
│   ├── configs.json                 # Dashboard-Konfigurationen
│   └── user_preferences.json        # User-Einstellungen
│
└── analytics/
    ├── rca_events_2025-01.json      # Root-Cause-Analysis
    └── correlations.json            # Event-Korrelationen
```

---

## Query-Validierung

**WICHTIG:** Alle Queries gegen die AX-DB werden validiert!

### Erlaubt ✅
```sql
SELECT * FROM BATCHJOB WHERE STATUS = 1
SELECT CAPTION, STARTDATETIME FROM BrasBatchJobHistoryTable
```

### Blockiert ❌
```sql
INSERT INTO BATCHJOB ...           -- BLOCKIERT
UPDATE SYSCLIENTSESSIONS ...       -- BLOCKIERT
DELETE FROM BATCHJOB ...           -- BLOCKIERT
SELECT * INTO new_table FROM ...   -- BLOCKIERT
EXEC sp_executesql ...             -- BLOCKIERT
```

---

## Konfiguration

### Environment Variables (.env.storage)

```bash
# KRITISCH - Immer auf true lassen!
AX_DB_READ_ONLY=true

# Storage-Typ
STORAGE_TYPE=file

# Basis-Verzeichnis
DATA_DIR=./data

# Retention-Policies
HISTORY_RETENTION_MONTHS=12
ALERT_HISTORY_MONTHS=6
BACKUP_RETENTION_DAYS=7
```

---

## Migration bestehender Daten

### Schritt 1: Export aus alter DB

```bash
python scripts/export_db_to_files.py --source sqlite://staging.db --target ./data
```

### Schritt 2: Daten prüfen

```bash
# Prüfe exportierte Dateien
ls -lh data/history/
ls -lh data/alerts/
```

### Schritt 3: App neu starten

```bash
# Mit neuer Storage-Config
export $(cat .env.storage | xargs)
streamlit run app/main.py
```

---

## API-Änderungen

### History Storage

**Alt (DB):**
```python
from app.db.history_storage import history_storage

history_storage.init_database()  # CREATE TABLE
history_storage.store_metrics(metrics)  # INSERT
```

**Neu (CSV):**
```python
from app.db.history_storage_csv import history_storage

# Kein init nötig - Files werden automatisch erstellt
history_storage.store_metrics(metrics)  # Append to CSV
```

### Alerts Engine

**Alt (DB):**
```python
from app.alerts.rules_engine import alert_engine

alert_engine.init_database()  # CREATE TABLE
alert_engine.create_rule(rule_data)  # INSERT
```

**Neu (JSON):**
```python
from app.alerts.rules_engine_json import alert_engine

# Kein init nötig - JSON wird automatisch erstellt
alert_engine.create_rule(rule_data)  # Append to JSON
```

---

## Backup & Recovery

### Automatische Backups

Vor jedem Schreibvorgang wird automatisch ein Backup erstellt:

```
data/alerts/rules_backup_20250123_143022.json
```

### Manuelles Backup

```bash
# Gesamtes data/ Verzeichnis sichern
tar -czf backup_$(date +%Y%m%d).tar.gz data/

# Oder nur wichtige Dateien
cp -r data/alerts/ backups/alerts_$(date +%Y%m%d)/
```

### Recovery

```bash
# Letzte Version wiederherstellen
cp data/alerts/rules_backup_20250123_143022.json data/alerts/rules.json

# Oder komplettes Backup
tar -xzf backup_20250123.tar.gz
```

---

## Performance

### CSV-Optimierungen

1. **Monatliche Rotation** - Jeder Monat = separate Datei
2. **Append-Only** - Keine rewrites, nur append
3. **Batch-Writes** - 1000 Zeilen auf einmal schreiben

### JSON-Optimierungen

1. **Atomic Writes** - Temp-File + Rename
2. **Pretty-Print nur für Config** - Historie: kein indent
3. **JSONL für Streams** - Ein JSON-Object pro Zeile

---

## Monitoring

### Storage-Health-Check

```python
from app.db.history_storage_csv import history_storage

stats = history_storage.get_storage_stats()
print(f"Total files: {stats['total_files']}")
print(f"Total size: {stats['total_size_mb']} MB")
```

### Rotation Status

```python
from app.db.history_storage_csv import history_storage

# Alte Dateien löschen (älter als 12 Monate)
history_storage.rotate_old_files(keep_months=12)
```

---

## Troubleshooting

### Problem: "Permission Denied" beim Schreiben

**Lösung:**
```bash
# Prüfe Berechtigungen
ls -la data/

# Setze korrekte Rechte
chmod -R 755 data/
```

### Problem: "Query validation failed: Write operation detected"

**Ursache:** Query enthält INSERT/UPDATE/DELETE

**Lösung:**
```python
# ❌ Falsch
query = "UPDATE BATCHJOB SET STATUS = 1"

# ✅ Korrekt
query = "SELECT * FROM BATCHJOB WHERE STATUS = 1"
```

### Problem: CSV-Datei korrupt

**Lösung:**
```bash
# Prüfe auf Backup
ls data/history/*backup*

# Restore letzte Version
cp data/history/metrics_backup_20250123.csv data/history/metrics_2025-01.csv
```

---

## Testing

### Unit-Tests für File-Storage

```bash
pytest tests/test_file_storage.py -v
```

### Integration-Tests

```bash
pytest tests/test_history_storage_csv.py -v
pytest tests/test_alerts_engine_json.py -v
```

### Query-Validator Tests

```bash
pytest tests/test_query_validator.py -v
```

---

## Rollback-Plan

Falls die File-Storage Probleme macht:

### Option 1: Zurück zu DB-Storage

```bash
# 1. Alte DB-Module reaktivieren
mv app/db/history_storage.py.bak app/db/history_storage.py
mv app/alerts/rules_engine.py.bak app/alerts/rules_engine.py

# 2. Env-Var ändern
export STORAGE_TYPE=database

# 3. App neu starten
streamlit run app/main.py
```

### Option 2: Hybrid-Modus

```bash
# Nur kritische Daten in DB, Rest in Files
export STORAGE_TYPE=hybrid
export CRITICAL_STORAGE=database
export HISTORY_STORAGE=file
```

---

## Best Practices

### DO ✅

- **Regelmäßige Backups** - täglich `tar -czf`
- **Rotation aktiviert** - alte Dateien automatisch löschen
- **Query-Validator immer an** - `AX_DB_READ_ONLY=true`
- **Monitoring** - Storage-Stats regelmäßig prüfen

### DON'T ❌

- **Nie manuell in CSV/JSON editieren** - Nur über API
- **Keine großen Dateien in Git** - data/ in .gitignore
- **Read-Only nicht deaktivieren** - AX-DB muss read-only bleiben!
- **Alte Dateien nicht manuell löschen** - nutze `rotate_old_files()`

---

## Support

Bei Fragen oder Problemen:

1. Prüfe Logs: `./logs/storage.log`
2. Teste Query-Validator: `python -m app.db.query_validator`
3. Storage-Stats: `python -c "from app.db.history_storage_csv import history_storage; print(history_storage.get_storage_stats())"`

---

## Changelog

### Version 2.0 (2025-01-23)
- ✨ File-Storage implementiert (JSON/CSV/JSONL)
- ✨ Query-Validator für AX-DB
- ✨ Automatische Backups
- ✨ Rotation für alte Dateien
- 🔒 Read-Only Enforcement
- 📚 Migration-Scripts

### Version 1.0 (2024-12-01)
- Legacy: DB-basierte Storage (deprecated)

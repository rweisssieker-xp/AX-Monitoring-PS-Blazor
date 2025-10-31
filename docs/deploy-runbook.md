## Deploy Runbook (Streamlit + Python)

1. Pre-Checks
   - DB RO-User vorhanden, Netzwerkzugriff verifiziert
   - `.env` für Zielumgebung vollständig (ohne Secrets im Repo)
   - ODBC Driver 17/18 installiert
2. Build
   - Lint/Tests grün; Artefakt bauen (pip/poetry)
3. Deploy
   - Paket auf Zielhost kopieren
   - Abhängigkeiten installieren
   - Dienst/Container starten
4. Verify
   - Health-Checks (DB, Scheduler, SMTP) grün
   - Smoke-Tests (Overview, Alerts) ok
5. Handover
   - Monitoring aktiv, Logs zugänglich


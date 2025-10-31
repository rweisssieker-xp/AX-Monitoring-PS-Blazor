## Deadlock Capture with Extended Events (SQL Server 2016)

1. Create storage folder on SQL Server host (e.g., `C:\XE\deadlocks`) with write access for SQL Server service account.
2. Run `docs/sql/xevents-deadlock-setup.sql` and replace `<PATH>` accordingly.
3. Validate session is running: `SELECT * FROM sys.dm_xe_sessions WHERE name='DeadlockMonitor';`
4. Read events:
   - Option A (file target): run `docs/sql/xevents-deadlock-read.sql` with `<PATH>`.
   - Option B (ring buffer): query `sys.dm_xe_session_targets` (if enabled).
5. App access (read-only): grant rights to read the target folder via proxy/read proc or expose a DBA-owned SQL view that reads `sys.fn_xe_file_target_read_file`.
6. Ops: rotate files (max_rollover_files) and include path in backup/monitoring if needed.

Notes
- XE is low overhead; ensure MAX_DISPATCH_LATENCY small (5s) for near-real-time.
- Keep file target on fast local disk; avoid system drive when possible.


-- SQL Server 2016: Extended Events session for deadlock capture
-- NOTE: Replace <PATH> with a secure folder path accessible by the SQL Server service account
-- e.g., C:\XE\deadlocks (pre-create the directory on the server)

IF EXISTS (SELECT 1 FROM sys.server_event_sessions WHERE name = 'DeadlockMonitor')
BEGIN
    ALTER EVENT SESSION [DeadlockMonitor] ON SERVER STATE = STOP;
    DROP EVENT SESSION [DeadlockMonitor] ON SERVER;
END
GO

CREATE EVENT SESSION [DeadlockMonitor] ON SERVER
ADD EVENT sqlserver.xml_deadlock_report
ADD TARGET package0.event_file (
    SET filename = N'<PATH>\\deadlock',
        max_file_size = (50),
        max_rollover_files = (5)
)
WITH (
    MAX_MEMORY = 16MB,
    EVENT_RETENTION_MODE = ALLOW_SINGLE_EVENT_LOSS,
    MAX_DISPATCH_LATENCY = 5 SECONDS,
    TRACK_CAUSALITY = ON,
    STARTUP_STATE = ON
);
GO

ALTER EVENT SESSION [DeadlockMonitor] ON SERVER STATE = START;
GO

-- Optional: also add an in-memory ring buffer target (not persisted across restarts)
-- ALTER EVENT SESSION [DeadlockMonitor] ON SERVER
-- ADD TARGET package0.ring_buffer(SET max_memory = 16MB);

-- Maintenance
-- To stop: ALTER EVENT SESSION [DeadlockMonitor] ON SERVER STATE = STOP;
-- To drop: DROP EVENT SESSION [DeadlockMonitor] ON SERVER;


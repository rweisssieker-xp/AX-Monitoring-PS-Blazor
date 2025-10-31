-- DBA-created view to expose deadlocks via Extended Events to RO users
-- Replace <PATH> with the XE file folder. View should be created in a DBA-owned utility DB.

CREATE OR ALTER VIEW dbo.v_Deadlocks
AS
SELECT TOP 1000
    CAST(event_data AS XML) AS event_xml,
    TRY_CAST(event_data AS XML).value('(event/@timestamp)[1]','datetime2') AS event_ts,
    TRY_CAST(event_data AS XML).value('(event/data/value/deadlock/@victim)[1]','varchar(10)') AS victim
FROM sys.fn_xe_file_target_read_file(N'<PATH>\\deadlock*.xel', NULL, NULL, NULL);
GO

-- Grant read to monitoring RO login/user
-- GRANT SELECT ON dbo.v_Deadlocks TO [ax_ro];


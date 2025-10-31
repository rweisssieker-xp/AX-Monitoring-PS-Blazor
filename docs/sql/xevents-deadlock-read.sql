-- Read recent deadlocks from Extended Events file target
-- Replace <PATH> with the folder used in xevents-deadlock-setup.sql

WITH src AS (
    SELECT
        CAST(event_data AS XML) AS x,
        DATEADD(hh, DATEDIFF(hh, GETUTCDATE(), SYSDATETIMEOFFSET()),
                DATEADD(ms, (n.value('@timestamp','bigint')- (SELECT CAST(SERVERPROPERTY('ProductVersion') AS VARCHAR(20))) , '1970-01-01')) ) AS ts -- placeholder if needed
    FROM sys.fn_xe_file_target_read_file(N'<PATH>\\deadlock*.xel', NULL, NULL, NULL)
), parsed AS (
    SELECT
        x.value('(event/@timestamp)[1]','datetime2') AS event_ts,
        x.value('(event/data/value/deadlock/@victim)[1]','varchar(10)') AS victim,
        x.query('(event/data/value/deadlock)[1]') AS deadlock_xml
    FROM src
)
SELECT TOP 100
    event_ts,
    victim,
    deadlock_xml
FROM parsed
ORDER BY event_ts DESC;


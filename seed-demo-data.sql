-- Demo-Daten für AX Monitoring
-- SQLite Script zum Einfügen von Testdaten

-- Batch Jobs einfügen
INSERT INTO BatchJobs (BatchJobId, Name, Status, AosServer, StartTime, EndTime, CreatedAt, UpdatedAt, Progress) VALUES
('AX7654321', 'CustInvoicePost - Daily', 'Completed', 'bras3333', datetime('now', '-2 hours'), datetime('now', '-1 hour'), datetime('now', '-3 hours'), datetime('now'), 100),
('AX7654322', 'SalesOrderInvoicing', 'Running', 'bras3333', datetime('now', '-30 minutes'), NULL, datetime('now', '-35 minutes'), datetime('now'), 65),
('AX7654323', 'PurchInvoicePost', 'Error', 'bras3333', datetime('now', '-4 hours'), datetime('now', '-3 hours'), datetime('now', '-5 hours'), datetime('now'), 0),
('AX7654324', 'InventCounting', 'Waiting', 'bras3333', NULL, NULL, datetime('now', '-1 hour'), datetime('now'), 0),
('AX7654325', 'GLJournalPost - Monthly', 'Completed', 'bras3333', datetime('now', '-1 day'), datetime('now', '-1 day', '+2 hours'), datetime('now', '-1 day', '-1 hour'), datetime('now'), 100),
('AX7654326', 'BOMCalc', 'Running', 'bras3333', datetime('now', '-15 minutes'), NULL, datetime('now', '-20 minutes'), datetime('now'), 42),
('AX7654327', 'MasterPlanScheduling', 'Completed', 'bras3333', datetime('now', '-6 hours'), datetime('now', '-4 hours'), datetime('now', '-7 hours'), datetime('now'), 100),
('AX7654328', 'CustCollectionLetter', 'Waiting', 'bras3333', NULL, NULL, datetime('now', '-30 minutes'), datetime('now'), 0),
('AX7654329', 'VendPaymentProposal', 'Error', 'bras3333', datetime('now', '-8 hours'), datetime('now', '-7 hours'), datetime('now', '-9 hours'), datetime('now'), 0),
('AX7654330', 'IntercompanySync', 'Completed', 'bras3333', datetime('now', '-12 hours'), datetime('now', '-11 hours'), datetime('now', '-13 hours'), datetime('now'), 100);

-- Alerts einfügen
INSERT INTO Alerts (Severity, Title, Message, Source, Environment, Timestamp, Acknowledged, AcknowledgedBy, AcknowledgedAt, ResolvedAt) VALUES
('Critical', 'High CPU Usage on bras3333', 'CPU usage exceeded 90% for 5 minutes', 'AX_Monitor', 'DEV', datetime('now', '-1 hour'), 0, NULL, NULL, NULL),
('Warning', 'Batch Job Failed: PurchInvoicePost', 'Batch job failed with error code -1', 'AX_Monitor', 'DEV', datetime('now', '-4 hours'), 1, 'admin', datetime('now', '-3 hours'), NULL),
('Info', 'Daily Backup Completed', 'Database backup completed successfully', 'AX_Monitor', 'DEV', datetime('now', '-2 hours'), 1, 'system', datetime('now', '-2 hours'), datetime('now', '-2 hours')),
('Critical', 'Deadlock Detected', 'Deadlock detected between sessions S123456 and S123457', 'AX_Monitor', 'DEV', datetime('now', '-30 minutes'), 0, NULL, NULL, NULL),
('Warning', 'High Memory Usage', 'Memory usage exceeded 85% threshold', 'AX_Monitor', 'DEV', datetime('now', '-6 hours'), 1, 'admin', datetime('now', '-5 hours'), NULL),
('Info', 'Monitoring Service Started', 'Background monitoring service initialized', 'AX_Monitor', 'DEV', datetime('now', '-12 hours'), 1, 'system', datetime('now', '-12 hours'), datetime('now', '-12 hours')),
('Warning', 'Long Running Query Detected', 'Query running for over 120 seconds', 'AX_Monitor', 'DEV', datetime('now', '-3 hours'), 0, NULL, NULL, NULL),
('Critical', 'Disk Space Low', 'Disk space below 10% on drive C:', 'AX_Monitor', 'DEV', datetime('now', '-24 hours'), 1, 'admin', datetime('now', '-20 hours'), datetime('now', '-18 hours'));

-- Sessions einfügen
INSERT INTO Sessions (SessionId, UserId, UserName, ClientType, ServerId, LoginTime, LastActivity, Status, CpuTime, MemoryUsage) VALUES
('S123456', 'admin', 'Administrator', 'AX Client', 'bras3333', datetime('now', '-8 hours'), datetime('now', '-5 minutes'), 'Active', 1234, 256),
('S123457', 'jdoe', 'John Doe', 'Web', 'bras3333', datetime('now', '-4 hours'), datetime('now', '-2 minutes'), 'Active', 567, 128),
('S123458', 'msmith', 'Mary Smith', 'Service', 'bras3333', datetime('now', '-12 hours'), datetime('now', '-1 hour'), 'Idle', 890, 64),
('S123459', 'bjones', 'Bob Jones', 'AX Client', 'bras3333', datetime('now', '-2 hours'), datetime('now', '-1 minute'), 'Active', 234, 192),
('S123460', 'skumar', 'Sarah Kumar', 'Web', 'bras3333', datetime('now', '-6 hours'), datetime('now', '-30 minutes'), 'Idle', 456, 96),
('S123461', 'awilson', 'Alice Wilson', 'Batch', 'bras3333', datetime('now', '-1 hour'), datetime('now', '-3 minutes'), 'Active', 789, 320),
('S123462', 'rchen', 'Robert Chen', 'AX Client', 'bras3333', datetime('now', '-10 hours'), datetime('now', '-2 hours'), 'Idle', 123, 48),
('S123463', 'pmiller', 'Patricia Miller', 'Web', 'bras3333', datetime('now', '-3 hours'), datetime('now', '-1 minute'), 'Active', 345, 144);

-- Batch Job History einfügen (mehr historische Daten)
INSERT INTO BatchJobHistories (BatchJobId, Caption, Status, StartDateTime, EndDateTime, ExecutedBy, RecordCount, Duration) VALUES
('AX7654301', 'CustInvoicePost - 2024-11-07', 'Completed', datetime('now', '-24 hours'), datetime('now', '-23 hours'), 'BatchUser', 1250, 3600),
('AX7654302', 'SalesOrderInvoicing - 2024-11-07', 'Completed', datetime('now', '-22 hours'), datetime('now', '-21 hours'), 'BatchUser', 890, 3200),
('AX7654303', 'GLJournalPost - 2024-11-06', 'Completed', datetime('now', '-48 hours'), datetime('now', '-46 hours'), 'BatchUser', 2100, 7200),
('AX7654304', 'InventCounting - 2024-11-06', 'Error', datetime('now', '-36 hours'), datetime('now', '-35 hours'), 'BatchUser', 0, 600),
('AX7654305', 'VendPaymentProposal - 2024-11-07', 'Completed', datetime('now', '-20 hours'), datetime('now', '-19 hours'), 'BatchUser', 567, 2800);

SELECT 'Demo-Daten erfolgreich eingefügt!' as Status;
SELECT COUNT(*) as BatchJobs FROM BatchJobs;
SELECT COUNT(*) as Alerts FROM Alerts;
SELECT COUNT(*) as Sessions FROM Sessions;

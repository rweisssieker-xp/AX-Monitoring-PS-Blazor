# Analytics Features Documentation

## Overview

The AX Monitoring BU system provides comprehensive analytics capabilities for monitoring and analyzing Microsoft Dynamics 365 AX (Finance & Operations) batch jobs, system performance, and error patterns.

## Analytics Dashboards

### 1. System Load Analytics

**Route:** `/system-load-analytics`
**API Endpoints:** `/api/v1/analytics/system/`

#### Features

- **Hourly Load Distribution**
  - Visual heatmap of system load across 24 hours
  - Peak hours identification
  - Resource utilization patterns

- **Job Distribution by Status**
  - Success/Error/Running job counts
  - Status trends over time
  - Success rate percentage

- **AOS Server Performance**
  - Server-by-server load comparison
  - Average job duration per server
  - Server health indicators

- **Resource Bottlenecks**
  - Top 20 resource-intensive jobs
  - Peak load times identification
  - Bottleneck recommendations

#### Data Refresh

- Real-time data updates every 30 seconds
- Configurable date range filtering (default: last 7 days)
- Automatic caching (5-15 minutes) for performance optimization

#### SQL Queries

All queries use `WITH (NOLOCK)` hint for read-uncommitted isolation to minimize locking impact.

---

### 2. Performance Analytics

**Route:** `/performance-analytics`
**API Endpoints:** `/api/v1/analytics/performance/`

#### Features

- **Job Duration Trends**
  - Track job performance over time
  - Statistical metrics: Average, Min, Max, Standard Deviation
  - Trend visualization per job
  - Execution frequency tracking

- **Baseline Comparison**
  - Compare current performance vs 30-day historical baseline
  - Degraded/Improved job identification
  - Percentage change calculations
  - Alert threshold detection (>20% degradation = Warning, >50% = Critical)

- **Slowest Operations**
  - Top 20 slowest job executions
  - Duration in seconds
  - Server and company identification
  - Execution time stamps

- **Predictive Warnings**
  - Early warning system for performance degradation
  - Trend analysis: Increasing Duration, Rising Error Rate
  - Severity levels: Low, Medium, High
  - Confidence scores (0-100)
  - Predicted impact dates

#### Baseline Calculation

```sql
-- 30-day lookback window
-- Excludes current date
-- Statistical analysis: Mean, StdDev
-- Anomaly detection threshold: 2 standard deviations
```

#### Status Classification

| Duration Change | Status |
|----------------|---------|
| < 10% | Normal |
| 10-20% | Warning |
| 20-50% | Alert |
| > 50% | Critical |
| < -10% (improvement) | Improved |

---

### 3. Error Analytics

**Route:** `/error-analytics`
**API Endpoints:** `/api/v1/analytics/errors/`

#### Features

- **Root Cause Analysis**
  - Automatic error categorization by pattern matching
  - Categories:
    - Timeout Issues
    - Connection Issues
    - Permission Issues
    - Memory Issues
    - Locking Issues
    - Data Issues
    - Other Issues
  - Occurrence count and percentage
  - Suggested remediation actions
  - Last occurrence tracking

- **Error Correlations**
  - Jobs that tend to fail together
  - Time-window analysis (60-minute window)
  - Correlation strength (0-100%)
  - Co-occurrence count
  - Potential root cause identification

- **MTTR (Mean Time To Repair) Metrics**
  - Time from failure to successful execution
  - Average, Median, Min, Max repair times
  - Resolution status tracking
  - Trend analysis: Improving, Stable, Degrading
  - Performance indicators:
    - Good: < 15 minutes
    - Fair: 15-60 minutes
    - Poor: > 60 minutes

- **Business Impact Assessment**
  - Criticality scoring (0-100)
  - Affected users estimation
  - Downtime tracking (minutes)
  - Cost estimation (based on error frequency × baseline cost)
  - Impact levels:
    - Low: < 25 criticality
    - Medium: 25-50 criticality
    - High: 50-75 criticality
    - Critical: 75-100 criticality
  - Priority recommendations

#### Error Categorization Logic

```csharp
CASE
    WHEN Caption LIKE '%timeout%' THEN 'Timeout Issues'
    WHEN Caption LIKE '%connection%' THEN 'Connection Issues'
    WHEN Caption LIKE '%permission%' OR Caption LIKE '%access%' THEN 'Permission Issues'
    WHEN Caption LIKE '%memory%' THEN 'Memory Issues'
    WHEN Caption LIKE '%lock%' OR Caption LIKE '%deadlock%' THEN 'Locking Issues'
    WHEN Caption LIKE '%data%' OR Caption LIKE '%validation%' THEN 'Data Issues'
    ELSE 'Other Issues'
END
```

#### Business Impact Calculation

```csharp
// Criticality Score Factors:
// - Error frequency (40%)
// - Job type criticality (30%)
// - Recent trend (20%)
// - Historical baseline deviation (10%)

// Cost Estimation:
// Base cost per error: $100
// Multiplier based on criticality: 1x-5x
// Downtime cost per hour: $1000
```

---

## ML Predictions

**Route:** `/ml-predictions`
**API Endpoints:** `/api/v1/predictions/`

### Features

- **Batch Job Runtime Prediction**
  - Input parameters:
    - Job Complexity (0-100%)
    - Data Volume (0-100%)
    - AOS Server Load (0-100%)
    - Time of Day (Morning/Afternoon/Evening/Night)
  - Prediction output:
    - Predicted runtime (minutes)
    - Confidence level
    - Model performance metrics (R², MAE)
    - Key influencing factor

- **Anomaly Detection**
  - Real-time anomaly scoring
  - Historical pattern comparison
  - Outlier identification
  - Automatic alerting for anomalies

### Model Performance Metrics

- **R² (Coefficient of Determination)**: 0-1, higher is better
- **MAE (Mean Absolute Error)**: Lower is better
- **RMSE (Root Mean Square Error)**: Lower is better

---

## Data Caching Strategy

| Analytics Type | Cache Duration | Invalidation |
|---------------|----------------|--------------|
| System Load | 5 minutes | Time-based |
| Performance | 10 minutes | Time-based |
| Error Analytics | 10 minutes | Time-based |
| ML Predictions | 15 minutes | Time-based |
| Historical Data | 30 minutes | Time-based |

---

## Export Functionality

### Supported Formats

- **CSV**: Comma-separated values for Excel/spreadsheet import
- **Excel**: Formatted XLSX with headers and auto-fit columns
- **JSON**: Raw data export for API integration

### Export Service

```csharp
// Generic export methods
Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, string fileName);
Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName);
```

### API Usage

```http
GET /api/v1/analytics/performance/duration-trends/export?format=csv&startDate=2024-01-01&endDate=2024-01-31
GET /api/v1/analytics/errors/root-causes/export?format=excel&startDate=2024-01-01
```

---

## Performance Optimization

### Database Query Optimization

1. **Read-Uncommitted Isolation**
   - All analytics queries use `WITH (NOLOCK)`
   - Minimizes locking impact on production database

2. **Indexing Recommendations**
   ```sql
   -- Recommended indexes for optimal performance
   CREATE NONCLUSTERED INDEX IX_BATCHJOB_CREATEDDATETIME ON BATCHJOB(CREATEDDATETIME) INCLUDE (STATUS, CAPTION);
   CREATE NONCLUSTERED INDEX IX_BATCHJOB_STATUS_TIME ON BATCHJOB(STATUS, CREATEDDATETIME);
   ```

3. **Query Timeouts**
   - Default: 60 seconds
   - Configurable per query

### In-Memory Caching

```csharp
// IMemoryCache implementation
var cacheKey = $"analytics_{type}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
var cacheOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheMinutes)
};
```

---

## API Versioning

All analytics endpoints support API versioning:

- **Header-based**: `x-api-version: 1.0`
- **Query string**: `?api-version=1.0`
- **Default**: v1.0

---

## Security & Access Control

### Authentication

- JWT Bearer token authentication required
- Token expiration: Configurable (default 24 hours)

### Authorization

- Role-based access control (RBAC)
- Required roles for analytics:
  - `Analytics.Read` - View analytics data
  - `Analytics.Export` - Export analytics data
  - `Analytics.Admin` - Configure analytics settings

---

## Real-Time Updates

### SignalR Integration

Analytics data is automatically pushed to connected clients:

```javascript
// SignalR hub: /monitoringHub
connection.on("ReceiveMetricsUpdate", function (data) {
    // Handle real-time update
});
```

### Update Frequency

- System metrics: Every 30 seconds
- Batch job status: Real-time
- Analytics calculations: Every 5-15 minutes (depending on cache)

---

## Troubleshooting

### Common Issues

1. **Slow Analytics Queries**
   - Check database indexes
   - Verify connection string timeout settings
   - Review query execution plans
   - Consider date range reduction

2. **Missing Data**
   - Verify AX database connectivity
   - Check batch job history retention
   - Validate date range parameters
   - Review error logs in `logs/axmonitoring-*.json`

3. **Cache Invalidation**
   - Manual cache clear via API: `POST /api/v1/cache/clear`
   - Restart application to clear all caches
   - Adjust cache duration in configuration

### Logging

```bash
# View analytics logs
tail -f logs/axmonitoring-$(date +%Y%m%d).json | jq 'select(.SourceContext | contains("Analytics"))'
```

---

## Configuration

### appsettings.json

```json
{
  "Analytics": {
    "CacheDuration": {
      "SystemLoad": 5,
      "Performance": 10,
      "Errors": 10,
      "Predictions": 15
    },
    "QueryTimeout": 60,
    "DefaultDateRange": 7,
    "MaxExportRows": 100000,
    "EnableRealTimeUpdates": true
  }
}
```

### Environment Variables

```bash
# Override cache duration (minutes)
export ANALYTICS_CACHE_DURATION=10

# Override query timeout (seconds)
export ANALYTICS_QUERY_TIMEOUT=120

# Disable real-time updates
export ANALYTICS_REALTIME_ENABLED=false
```

---

## Best Practices

1. **Date Range Selection**
   - Use smallest date range necessary
   - Avoid queries spanning > 30 days for detailed analytics
   - Use aggregated views for long-term trends

2. **Performance Monitoring**
   - Monitor query execution times
   - Review cache hit rates
   - Optimize slow queries

3. **Data Retention**
   - Archive old analytics data regularly
   - Maintain 90-day rolling window for detailed analysis
   - Keep summary data for longer periods

4. **Error Handling**
   - All API endpoints return proper HTTP status codes
   - Error responses include actionable messages
   - Check logs for detailed error information

---

## Future Enhancements

- [ ] Advanced ML models (LSTM for time series)
- [ ] Predictive maintenance scheduling
- [ ] Automated remediation workflows
- [ ] Custom dashboard builder
- [ ] Mobile app support
- [ ] Integration with Microsoft Teams/Slack
- [ ] Advanced alerting rules engine

---

## Support

For issues or questions:
- GitHub: https://github.com/anthropics/claude-code/issues
- Documentation: `/docs`
- API Documentation: `/swagger` (Development only)

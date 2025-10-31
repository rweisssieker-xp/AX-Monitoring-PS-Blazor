// AX Monitor Dashboard JavaScript with Authentication

let resourceChart = null;
let batchChart = null;
let refreshInterval = null;

// Check authentication status
async function checkAuthStatus() {
    try {
        const response = await axios.get('/api/auth/me');
        const user = response.data;
        
        // Update UI with user info
        document.getElementById('user-info').textContent = user.username;
        document.getElementById('user-role').textContent = user.role;
        
        // Show/hide elements based on role
        updateUIForRole(user.role);
    } catch (error) {
        // If unauthorized, redirect to login
        if (error.response && error.response.status === 401) {
            window.location.href = '/login';
        }
        console.error('Authentication check failed:', error);
    }
}

// Update UI elements based on user role
function updateUIForRole(role) {
    // Hide/show admin-only elements
    const adminElements = document.querySelectorAll('.admin-only');
    adminElements.forEach(el => {
        el.style.display = (role === 'Admin') ? 'block' : 'none';
    });
    
    // Hide/show power-user elements
    const powerUserElements = document.querySelectorAll('.power-user-only');
    powerUserElements.forEach(el => {
        el.style.display = (role === 'Power-User' || role === 'Admin') ? 'block' : 'none';
    });
    
    // All roles can see viewer elements, so no need to hide them
}

// Initialize dashboard
document.addEventListener('DOMContentLoaded', async function() {
    // Check authentication status
    await checkAuthStatus();
    
    initCharts();
    loadDashboardData();
    startAutoRefresh();
    
    // Add logout functionality
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', async function() {
            try {
                await axios.post('/api/auth/logout');
                localStorage.removeItem('authToken');
                window.location.href = '/login';
            } catch (error) {
                console.error('Logout error:', error);
                // Even if logout API fails, redirect to login
                localStorage.removeItem('authToken');
                window.location.href = '/login';
            }
        });
    }
});

// Initialize charts
function initCharts() {
    // Resource Chart (CPU & Memory)
    const resourceCtx = document.getElementById('resourceChart');
    if (resourceCtx) {
        resourceChart = new Chart(resourceCtx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [
                    {
                        label: 'CPU Usage %',
                        data: [],
                        borderColor: '#1f77b4',
                        backgroundColor: 'rgba(31, 119, 180, 0.1)',
                        tension: 0.4
                    },
                    {
                        label: 'Memory Usage %',
                        data: [],
                        borderColor: '#ff7f0e',
                        backgroundColor: 'rgba(255, 127, 14, 0.1)',
                        tension: 0.4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        max: 100
                    }
                }
            }
        });
    }

    // Batch Chart
    const batchCtx = document.getElementById('batchChart');
    if (batchCtx) {
        batchChart = new Chart(batchCtx, {
            type: 'doughnut',
            data: {
                labels: ['Running', 'Waiting', 'Completed', 'Error'],
                datasets: [{
                    data: [0, 0, 0, 0],
                    backgroundColor: [
                        '#1f77b4',
                        '#ffc107',
                        '#28a745',
                        '#dc3545'
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                    }
                }
            }
        });
    }
}

// Load dashboard data
async function loadDashboardData() {
    try {
        // Load KPI data
        const kpiResponse = await axios.get('/api/kpi');
        updateKPIs(kpiResponse.data);

        // Load alerts
        const alertsResponse = await axios.get('/api/alerts');
        updateAlerts(alertsResponse.data.data);

        // Update timestamp
        updateTimestamp();
    } catch (error) {
        console.error('Error loading dashboard data:', error);
        
        // Check if it's an authentication error
        if (error.response && error.response.status === 401) {
            window.location.href = '/login';
            return;
        }
        
        showError('Failed to load dashboard data');
    }
}

// Update KPI cards
function updateKPIs(data) {
    // Batch Backlog
    document.getElementById('batchBacklog').textContent = data.BatchBacklog || 0;
    
    // Error Rate
    const errorRate = data.ErrorRate || 0;
    document.getElementById('errorRate').textContent = errorRate.toFixed(1) + '%';
    document.getElementById('errorRate').className = errorRate > 10 ? 'kpi-value text-danger' : 'kpi-value';
    
    // Active Sessions
    document.getElementById('activeSessions').textContent = data.ActiveSessions || 0;
    
    // Blocking Chains
    const blockingChains = data.BlockingChains || 0;
    document.getElementById('blockingChains').textContent = blockingChains;
    document.getElementById('blockingChains').className = blockingChains > 0 ? 'kpi-value text-warning' : 'kpi-value';

    // Update health metrics
    updateHealthMetrics(data);

    // Update charts
    updateCharts(data);
}

// Update health metrics
function updateHealthMetrics(data) {
    // CPU Usage
    const cpuUsage = data.CPUUsage || 0;
    document.getElementById('cpuUsage').textContent = cpuUsage.toFixed(1) + '%';
    document.getElementById('cpuBar').style.width = cpuUsage + '%';
    document.getElementById('cpuBar').style.background = getHealthColor(cpuUsage);

    // Memory Usage
    const memoryUsage = data.MemoryUsage || 0;
    document.getElementById('memoryUsage').textContent = memoryUsage.toFixed(1) + '%';
    document.getElementById('memoryBar').style.width = memoryUsage + '%';
    document.getElementById('memoryBar').style.background = getHealthColor(memoryUsage);

    // Active Connections
    document.getElementById('activeConnections').textContent = data.ActiveConnections || 0;

    // Longest Query
    const longestQuery = data.LongestQueryMinutes || 0;
    document.getElementById('longestQuery').textContent = longestQuery + ' min';
}

// Get health color based on percentage
function getHealthColor(percentage) {
    if (percentage < 60) return '#28a745';
    if (percentage < 80) return '#ffc107';
    return '#dc3545';
}

// Update charts
function updateCharts(data) {
    // Update resource chart
    if (resourceChart) {
        const now = new Date().toLocaleTimeString();
        
        // Keep last 20 data points
        if (resourceChart.data.labels.length > 20) {
            resourceChart.data.labels.shift();
            resourceChart.data.datasets[0].data.shift();
            resourceChart.data.datasets[1].data.shift();
        }
        
        resourceChart.data.labels.push(now);
        resourceChart.data.datasets[0].data.push(data.CPUUsage || 0);
        resourceChart.data.datasets[1].data.push(data.MemoryUsage || 0);
        resourceChart.update();
    }

    // Update batch chart (would need batch statistics endpoint)
    if (batchChart) {
        // Placeholder data - would come from batch statistics endpoint
        batchChart.data.datasets[0].data = [
            data.BatchBacklog || 0,
            Math.floor(Math.random() * 5),
            Math.floor(Math.random() * 10),
            Math.floor(data.ErrorRate || 0)
        ];
        batchChart.update();
    }
}

// Update alerts list
function updateAlerts(alerts) {
    const alertsList = document.getElementById('alertsList');
    
    if (!alerts || alerts.length === 0) {
        alertsList.innerHTML = '<div class="alert-item info"><div class="alert-message">✅ No active alerts</div></div>';
        return;
    }

    // Show only recent active alerts (last 5)
    const recentAlerts = alerts
        .filter(a => a.Status === 'Active')
        .slice(0, 5);

    if (recentAlerts.length === 0) {
        alertsList.innerHTML = '<div class="alert-item info"><div class="alert-message">✅ No active alerts</div></div>';
        return;
    }

    alertsList.innerHTML = recentAlerts.map(alert => {
        const severityClass = alert.Severity.toLowerCase();
        const icon = getSeverityIcon(alert.Severity);
        const time = new Date(alert.CreatedAt).toLocaleTimeString();

        return `
            <div class="alert-item ${severityClass}">
                <div class="alert-header">
                    <div class="alert-title">
                        <span>${icon}</span>
                        <strong>${alert.AlertType}</strong>
                    </div>
                    <div class="alert-time">${time}</div>
                </div>
                <div class="alert-message">${alert.Message}</div>
            </div>
        `;
    }).join('');
}

// Get severity icon
function getSeverityIcon(severity) {
    switch (severity) {
        case 'Critical': return '🔴';
        case 'Warning': return '🟡';
        case 'Info': return '🔵';
        default: return '⚪';
    }
}

// Update timestamp
function updateTimestamp() {
    const now = new Date();
    document.getElementById('lastUpdate').textContent = now.toLocaleTimeString();
}

// Refresh data
function refreshData() {
    loadDashboardData();
}

// Start auto-refresh
function startAutoRefresh() {
    // Refresh every 30 seconds
    refreshInterval = setInterval(() => {
        loadDashboardData();
    }, 30000);
}

// Stop auto-refresh
function stopAutoRefresh() {
    if (refreshInterval) {
        clearInterval(refreshInterval);
        refreshInterval = null;
    }
}

// Show error message
function showError(message) {
    console.error(message);
    // Could implement a toast notification here
}

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    stopAutoRefresh();
});

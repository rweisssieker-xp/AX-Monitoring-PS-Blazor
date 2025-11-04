// Chart.js helper for Blazor (using global Chart.js from CDN)
window.chartHelper = {
    createChart: (canvasId, chartType, data, options) => {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element with id ${canvasId} not found`);
            return null;
        }

        if (typeof Chart === 'undefined') {
            console.error('Chart.js is not loaded. Please include Chart.js script.');
            return null;
        }

        const ctx = canvas.getContext('2d');
        
        const chartOptions = {
            type: chartType,
            data: {
                labels: data.labels || [],
                datasets: (data.datasets || []).map(dataset => ({
                    label: dataset.label || '',
                    data: dataset.data || [],
                    backgroundColor: Array.isArray(dataset.backgroundColor) 
                        ? dataset.backgroundColor 
                        : (typeof dataset.backgroundColor === 'string' && dataset.backgroundColor.includes(',')
                            ? dataset.backgroundColor.split(',').map(c => c.trim())
                            : dataset.backgroundColor || 'rgba(54, 162, 235, 0.2)'),
                    borderColor: dataset.borderColor || 'rgba(54, 162, 235, 1)',
                    borderWidth: dataset.borderWidth || 1
                }))
            },
            options: {
                responsive: options?.responsive !== false,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: options?.plugins?.legend?.display !== false,
                        position: options?.plugins?.legend?.position || 'top'
                    },
                    title: {
                        display: options?.plugins?.title?.display !== false,
                        text: options?.plugins?.title?.text || ''
                    }
                },
                scales: options?.scales ? {
                    y: {
                        beginAtZero: options.scales.y?.beginAtZero !== false,
                        max: options.scales.y?.max || undefined
                    }
                } : undefined
            }
        };

        return new Chart(ctx, chartOptions);
    },

    updateChart: (chartInstance, data) => {
        if (!chartInstance) return;

        chartInstance.data.labels = data.labels || [];
        chartInstance.data.datasets = (data.datasets || []).map(dataset => ({
            label: dataset.label || '',
            data: dataset.data || [],
            backgroundColor: Array.isArray(dataset.backgroundColor) 
                ? dataset.backgroundColor 
                : (typeof dataset.backgroundColor === 'string' && dataset.backgroundColor.includes(',')
                    ? dataset.backgroundColor.split(',').map(c => c.trim())
                    : dataset.backgroundColor || 'rgba(54, 162, 235, 0.2)'),
            borderColor: dataset.borderColor || 'rgba(54, 162, 235, 1)',
            borderWidth: dataset.borderWidth || 1
        }));

        chartInstance.update();
    },

    destroyChart: (chartInstance) => {
        if (chartInstance) {
            chartInstance.destroy();
        }
    }
};



/**
 * 农户销售统计工具
 * @description 处理农户中心销售数据分析和可视化
 */

const FarmerStatistics = {
    /**
     * 加载销售概览数据
     * @param {number} farmerId - 农户ID
     * @returns {Promise<object>} 返回销售概览数据
     */
    async loadSalesOverview(farmerId) {
        try {
            const response = await http.get(`/api/statistics/farmer/${farmerId}/overview`);
            return response.data;
        } catch (error) {
            console.error('加载销售概览失败:', error);
            throw error;
        }
    },
    
    /**
     * 加载销售趋势数据
     * @param {number} farmerId - 农户ID
     * @param {string} period - 时间周期 (day, week, month, year)
     * @param {string} startDate - 开始日期 (YYYY-MM-DD)
     * @param {string} endDate - 结束日期 (YYYY-MM-DD)
     * @returns {Promise<object>} 返回销售趋势数据
     */
    async loadSalesTrends(farmerId, period = 'month', startDate = null, endDate = null) {
        try {
            let url = `/api/statistics/farmer/${farmerId}/trends?period=${period}`;
            
            if (startDate) {
                url += `&startDate=${startDate}`;
            }
            
            if (endDate) {
                url += `&endDate=${endDate}`;
            }
            
            const response = await http.get(url);
            return response.data;
        } catch (error) {
            console.error('加载销售趋势失败:', error);
            throw error;
        }
    },
    
    /**
     * 加载商品销售排名
     * @param {number} farmerId - 农户ID
     * @param {string} sortBy - 排序依据 (revenue, quantity)
     * @param {number} limit - 返回数量限制
     * @returns {Promise<Array>} 返回商品销售排名数据
     */
    async loadProductRanking(farmerId, sortBy = 'revenue', limit = 10) {
        try {
            const response = await http.get(`/api/statistics/farmer/${farmerId}/ranking?sortBy=${sortBy}&limit=${limit}`);
            return response.data;
        } catch (error) {
            console.error('加载商品排名失败:', error);
            throw error;
        }
    },
    
    /**
     * 加载客户分布数据
     * @param {number} farmerId - 农户ID
     * @returns {Promise<object>} 返回客户分布数据
     */
    async loadCustomerDistribution(farmerId) {
        try {
            const response = await http.get(`/api/statistics/farmer/${farmerId}/customers`);
            return response.data;
        } catch (error) {
            console.error('加载客户分布失败:', error);
            throw error;
        }
    },
    
    /**
     * 渲染销售趋势图表
     * @param {string} canvasId - Canvas元素ID
     * @param {object} data - 趋势数据
     */
    renderSalesTrendsChart(canvasId, data) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        
        // 销毁现有图表（如果存在）
        if (window.salesTrendsChart) {
            window.salesTrendsChart.destroy();
        }
        
        // 转换日期格式
        const labels = data.map(item => {
            const date = new Date(item.date);
            return date.toLocaleDateString('zh-CN');
        });
        
        // 创建新图表
        window.salesTrendsChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: '销售额(元)',
                        data: data.map(item => item.revenue),
                        backgroundColor: 'rgba(75, 192, 192, 0.2)',
                        borderColor: 'rgba(75, 192, 192, 1)',
                        borderWidth: 2,
                        tension: 0.1
                    },
                    {
                        label: '订单数量',
                        data: data.map(item => item.orderCount),
                        backgroundColor: 'rgba(153, 102, 255, 0.2)',
                        borderColor: 'rgba(153, 102, 255, 1)',
                        borderWidth: 2,
                        tension: 0.1
                    }
                ]
            },
            options: {
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: '销售趋势'
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        callbacks: {
                            label: function(context) {
                                let label = context.dataset.label || '';
                                if (label) {
                                    label += ': ';
                                }
                                if (context.parsed.y !== null) {
                                    if (label.includes('销售额')) {
                                        label += '¥' + context.parsed.y.toFixed(2);
                                    } else {
                                        label += context.parsed.y;
                                    }
                                }
                                return label;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    },
    
    /**
     * 渲染商品销售排名图表
     * @param {string} canvasId - Canvas元素ID
     * @param {Array} data - 排名数据
     */
    renderProductRankingChart(canvasId, data) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        
        // 销毁现有图表（如果存在）
        if (window.productRankingChart) {
            window.productRankingChart.destroy();
        }
        
        // 截取前10项数据
        const chartData = data.slice(0, 10);
        
        // 创建新图表
        window.productRankingChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: chartData.map(item => item.productName),
                datasets: [
                    {
                        label: '销售额(元)',
                        data: chartData.map(item => item.revenue),
                        backgroundColor: 'rgba(255, 159, 64, 0.7)',
                        borderColor: 'rgba(255, 159, 64, 1)',
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                indexAxis: 'y',
                plugins: {
                    title: {
                        display: true,
                        text: '商品销售排名'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                let label = context.dataset.label || '';
                                if (label) {
                                    label += ': ';
                                }
                                if (context.parsed.x !== null) {
                                    label += '¥' + context.parsed.x.toFixed(2);
                                }
                                return label;
                            }
                        }
                    }
                }
            }
        });
    },
    
    /**
     * 渲染客户地区分布图表
     * @param {string} canvasId - Canvas元素ID
     * @param {object} data - 客户分布数据
     */
    renderCustomerDistributionChart(canvasId, data) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        
        // 销毁现有图表（如果存在）
        if (window.customerDistributionChart) {
            window.customerDistributionChart.destroy();
        }
        
        // 创建新图表
        window.customerDistributionChart = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: data.map(item => item.region),
                datasets: [
                    {
                        data: data.map(item => item.count),
                        backgroundColor: [
                            'rgba(255, 99, 132, 0.7)',
                            'rgba(54, 162, 235, 0.7)',
                            'rgba(255, 206, 86, 0.7)',
                            'rgba(75, 192, 192, 0.7)',
                            'rgba(153, 102, 255, 0.7)',
                            'rgba(255, 159, 64, 0.7)',
                            'rgba(199, 199, 199, 0.7)',
                            'rgba(83, 102, 255, 0.7)',
                            'rgba(40, 159, 64, 0.7)',
                            'rgba(210, 105, 30, 0.7)'
                        ],
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: '客户地区分布'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const label = context.label || '';
                                const value = context.formattedValue;
                                const total = context.dataset.data.reduce((acc, val) => acc + val, 0);
                                const percentage = ((context.raw / total) * 100).toFixed(1);
                                return `${label}: ${value} (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    },
    
    /**
     * 渲染销售概览卡片
     * @param {object} data - 概览数据
     * @param {string} containerId - 容器元素ID
     */
    renderOverviewCards(data, containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        const cards = [
            {
                title: '总销售额',
                value: `¥${data.totalRevenue.toFixed(2)}`,
                icon: 'fa-yuan-sign',
                color: 'success'
            },
            {
                title: '已完成订单',
                value: data.completedOrders,
                icon: 'fa-check-circle',
                color: 'primary'
            },
            {
                title: '客户数量',
                value: data.customerCount,
                icon: 'fa-users',
                color: 'info'
            },
            {
                title: '平均订单金额',
                value: `¥${data.avgOrderValue.toFixed(2)}`,
                icon: 'fa-shopping-cart',
                color: 'warning'
            }
        ];
        
        let html = '<div class="row">';
        
        cards.forEach(card => {
            html += `
                <div class="col-md-3 col-sm-6 mb-4">
                    <div class="card border-${card.color} h-100">
                        <div class="card-body text-center">
                            <div class="text-${card.color} mb-3">
                                <i class="fas ${card.icon} fa-3x"></i>
                            </div>
                            <h5 class="card-title">${card.title}</h5>
                            <h3 class="card-text">${card.value}</h3>
                        </div>
                    </div>
                </div>
            `;
        });
        
        html += '</div>';
        container.innerHTML = html;
    }
};

// 导出为全局变量
window.FarmerStatistics = FarmerStatistics; 
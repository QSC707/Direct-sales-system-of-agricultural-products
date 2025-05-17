/**
 * 统计数据处理模块
 * 处理销售统计、数据分析等功能
 */

// 模拟销售概览数据（实际应由后端API提供）
const mockOverviewData = {
    totalSales: 12835.75,
    totalOrders: 135,
    totalProducts: 42,
    avgOrderValue: 95.08
};

// 模拟销售趋势数据（实际应由后端API提供）
const mockTrendsData = {
    week: {
        labels: ['周一', '周二', '周三', '周四', '周五', '周六', '周日'],
        salesData: [1500, 1728, 1350, 1840, 2100, 2450, 1865],
        ordersData: [15, 18, 14, 19, 22, 26, 21]
    },
    month: {
        labels: ['第1周', '第2周', '第3周', '第4周'],
        salesData: [5000, 6800, 7200, 6500],
        ordersData: [52, 71, 75, 68]
    },
    year: {
        labels: ['1月', '2月', '3月', '4月', '5月', '6月', '7月', '8月', '9月', '10月', '11月', '12月'],
        salesData: [10500, 12000, 15500, 13200, 16800, 19200, 18500, 20100, 22800, 21300, 23500, 25800],
        ordersData: [110, 125, 162, 138, 175, 201, 193, 210, 238, 222, 245, 269]
    }
};

// 模拟销量排名数据（实际应由后端API提供）
const mockQuantityRankingData = [
    { productId: 1, productName: '有机胡萝卜', quantity: 328, revenue: 1312.00 },
    { productId: 5, productName: '新鲜猕猴桃', quantity: 215, revenue: 2580.00 },
    { productId: 8, productName: '东北黑木耳', quantity: 189, revenue: 1890.00 },
    { productId: 3, productName: '生态土鸡蛋', quantity: 156, revenue: 936.00 },
    { productId: 12, productName: '有机西红柿', quantity: 142, revenue: 710.00 },
    { productId: 7, productName: '山地小香葱', quantity: 138, revenue: 414.00 },
    { productId: 15, productName: '野生蓝莓', quantity: 126, revenue: 2520.00 },
    { productId: 9, productName: '散养土鸡', quantity: 98, revenue: 4900.00 },
    { productId: 20, productName: '农家红薯', quantity: 95, revenue: 380.00 },
    { productId: 11, productName: '生态大米', quantity: 87, revenue: 1305.00 }
];

// 模拟评分排名数据（实际应由后端API提供）
const mockRatingRankingData = [
    { productId: 15, productName: '野生蓝莓', averageRating: 4.9, reviewCount: 28 },
    { productId: 9, productName: '散养土鸡', averageRating: 4.8, reviewCount: 15 },
    { productId: 5, productName: '新鲜猕猴桃', averageRating: 4.7, reviewCount: 32 },
    { productId: 11, productName: '生态大米', averageRating: 4.6, reviewCount: 21 },
    { productId: 8, productName: '东北黑木耳', averageRating: 4.5, reviewCount: 18 },
    { productId: 3, productName: '生态土鸡蛋', averageRating: 4.4, reviewCount: 25 },
    { productId: 1, productName: '有机胡萝卜', averageRating: 4.3, reviewCount: 42 },
    { productId: 12, productName: '有机西红柿', averageRating: 4.2, reviewCount: 19 },
    { productId: 20, productName: '农家红薯', averageRating: 4.1, reviewCount: 12 },
    { productId: 7, productName: '山地小香葱', averageRating: 4.0, reviewCount: 9 }
];

// 扩展API对象，添加统计相关接口
api.statistics = {
    // 获取销售概览
    getOverview: async () => {
        try {
            // 实际应调用后端API
            /* const response = await fetch(`${window.API_BASE_URL}/statistics/overview`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            return handleResponse(response); */
            
            // 模拟API响应
            return new Promise(resolve => {
                setTimeout(() => {
                    resolve({
                        code: 200,
                        message: '获取销售概览成功',
                        data: mockOverviewData
                    });
                }, 300);
            });
        } catch (error) {
            console.error('获取销售概览失败:', error);
            throw error;
        }
    },
    
    // 获取销售趋势
    getTrends: async (period) => {
        try {
            // 实际应调用后端API
            /* const response = await fetch(`${window.API_BASE_URL}/statistics/trends?period=${period}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            return handleResponse(response); */
            
            // 模拟API响应
            return new Promise(resolve => {
                setTimeout(() => {
                    resolve({
                        code: 200,
                        message: '获取销售趋势成功',
                        data: mockTrendsData[period] || mockTrendsData.month
                    });
                }, 500);
            });
        } catch (error) {
            console.error('获取销售趋势失败:', error);
            throw error;
        }
    },
    
    // 获取排名数据
    getRanking: async (sortBy) => {
        try {
            // 实际应调用后端API
            /* const response = await fetch(`${window.API_BASE_URL}/statistics/ranking?sortBy=${sortBy}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            return handleResponse(response); */
            
            // 模拟API响应
            return new Promise(resolve => {
                setTimeout(() => {
                    let data;
                    
                    // 根据排序类型返回不同的模拟数据
                    switch (sortBy) {
                        case 'quantity':
                            data = [...mockQuantityRankingData];
                            break;
                        case 'revenue':
                            data = [...mockQuantityRankingData].sort((a, b) => b.revenue - a.revenue);
                            break;
                        case 'rating':
                            data = [...mockRatingRankingData];
                            break;
                        case 'reviews':
                            data = [...mockRatingRankingData].sort((a, b) => b.reviewCount - a.reviewCount);
                            break;
                        default:
                            data = sortBy.includes('rating') ? mockRatingRankingData : mockQuantityRankingData;
                    }
                    
                    resolve({
                        code: 200,
                        message: '获取排名数据成功',
                        data: data
                    });
                }, 400);
            });
        } catch (error) {
            console.error('获取排名数据失败:', error);
            throw error;
        }
    }
};

/**
 * 销售统计功能
 */

// 加载销售总览数据
async function loadSalesOverview(containerId) {
    try {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        container.innerHTML = '<div class="text-center"><div class="spinner-border text-success" role="status"><span class="visually-hidden">加载中...</span></div></div>';
        
        const response = await api.statistics.getOverview();
        const overview = response.data;
        
        if (!overview) {
            container.innerHTML = '<div class="alert alert-info">暂无销售数据</div>';
            return;
        }
        
        const html = `
            <div class="row">
                <div class="col-md-3 col-sm-6">
                    <div class="stats-card">
                        <div class="card-body">
                            <div class="d-flex align-items-center justify-content-between mb-3">
                                <div class="text-muted">总销售额</div>
                                <div class="icon"><i class="fas fa-yen-sign"></i></div>
                            </div>
                            <div class="stats-value">￥${(overview.totalSales || 0).toFixed(2)}</div>
                            <div class="text-muted mt-2">较上周 ${formatChangePercent(overview.salesChange)}</div>
                        </div>
                    </div>
                </div>
                <div class="col-md-3 col-sm-6">
                    <div class="stats-card">
                        <div class="card-body">
                            <div class="d-flex align-items-center justify-content-between mb-3">
                                <div class="text-muted">订单数量</div>
                                <div class="icon"><i class="fas fa-shopping-cart"></i></div>
                            </div>
                            <div class="stats-value">${overview.totalOrders || 0}</div>
                            <div class="text-muted mt-2">较上周 ${formatChangePercent(overview.ordersChange)}</div>
                        </div>
                    </div>
                </div>
                <div class="col-md-3 col-sm-6">
                    <div class="stats-card">
                        <div class="card-body">
                            <div class="d-flex align-items-center justify-content-between mb-3">
                                <div class="text-muted">产品数量</div>
                                <div class="icon"><i class="fas fa-box"></i></div>
                            </div>
                            <div class="stats-value">${overview.totalProducts || 0}</div>
                            <div class="text-muted mt-2">总产品数</div>
                        </div>
                    </div>
                </div>
                <div class="col-md-3 col-sm-6">
                    <div class="stats-card">
                        <div class="card-body">
                            <div class="d-flex align-items-center justify-content-between mb-3">
                                <div class="text-muted">客户满意度</div>
                                <div class="icon"><i class="fas fa-smile"></i></div>
                            </div>
                            <div class="stats-value">${(overview.avgRating || 0).toFixed(1)}</div>
                            <div class="text-muted mt-2">平均评分</div>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        container.innerHTML = html;
    } catch (error) {
        console.error('加载销售总览失败:', error);
        document.getElementById(containerId).innerHTML = `<div class="alert alert-danger">加载销售总览失败: ${error.message}</div>`;
    }
}

// 格式化变化百分比
function formatChangePercent(percent) {
    if (!percent && percent !== 0) return '无数据';
    
    const isPositive = percent > 0;
    const icon = isPositive ? '<i class="fas fa-arrow-up text-success"></i>' : '<i class="fas fa-arrow-down text-danger"></i>';
    const formattedPercent = Math.abs(percent).toFixed(1) + '%';
    
    return percent === 0 ? '持平' : `${icon} ${formattedPercent}`;
}

// 加载销售趋势图表
async function loadSalesTrends(chartContainerId, period = 'week') {
    try {
        const container = document.getElementById(chartContainerId);
        if (!container) return;
        
        container.innerHTML = '<div class="text-center"><div class="spinner-border text-success" role="status"><span class="visually-hidden">加载中...</span></div></div>';
        
        const response = await api.statistics.getTrends(period);
        const trends = response.data;
        
        if (!trends || trends.length === 0) {
            container.innerHTML = '<div class="alert alert-info">暂无销售趋势数据</div>';
            return;
        }
        
        // 清空容器
        container.innerHTML = '';
        
        // 格式化日期标签
        const labels = trends.map(item => {
            const date = new Date(item.date);
            if (period === 'day') {
                return date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' });
            } else if (period === 'week') {
                return date.toLocaleDateString('zh-CN', { month: 'numeric', day: 'numeric' });
            } else if (period === 'month') {
                return date.toLocaleDateString('zh-CN', { month: 'long', day: 'numeric' });
            } else {
                return date.toLocaleDateString('zh-CN', { year: 'numeric', month: 'numeric' });
            }
        });
        
        // 准备数据
        const salesData = trends.map(item => item.sales);
        const ordersData = trends.map(item => item.orders);
        
        // 创建图表
        const ctx = document.createElement('canvas');
        ctx.id = 'salesTrendsChart';
        container.appendChild(ctx);
        
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: '销售额',
                        data: salesData,
                        borderColor: 'rgba(40, 167, 69, 1)',
                        backgroundColor: 'rgba(40, 167, 69, 0.1)',
                        borderWidth: 2,
                        tension: 0.3,
                        fill: true,
                        yAxisID: 'y'
                    },
                    {
                        label: '订单数',
                        data: ordersData,
                        borderColor: 'rgba(0, 123, 255, 1)',
                        backgroundColor: 'rgba(0, 123, 255, 0)',
                        borderWidth: 2,
                        tension: 0.3,
                        borderDash: [5, 5],
                        yAxisID: 'y1'
                    }
                ]
            },
            options: {
                responsive: true,
                interaction: {
                    mode: 'index',
                    intersect: false,
                },
                plugins: {
                    legend: {
                        position: 'top',
                    },
                    title: {
                        display: true,
                        text: getChartTitle(period)
                    }
                },
                scales: {
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        title: {
                            display: true,
                            text: '销售额 (元)'
                        }
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        title: {
                            display: true,
                            text: '订单数'
                        },
                        grid: {
                            drawOnChartArea: false
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('加载销售趋势失败:', error);
        document.getElementById(chartContainerId).innerHTML = `<div class="alert alert-danger">加载销售趋势失败: ${error.message}</div>`;
    }
}

// 根据时间段获取图表标题
function getChartTitle(period) {
    switch (period) {
        case 'day': return '今日销售趋势';
        case 'week': return '本周销售趋势';
        case 'month': return '本月销售趋势';
        case 'year': return '全年销售趋势';
        default: return '销售趋势';
    }
}

// 加载销售排名
async function loadSalesRanking(containerId, sortBy = 'sales') {
    try {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        container.innerHTML = '<div class="text-center"><div class="spinner-border text-success" role="status"><span class="visually-hidden">加载中...</span></div></div>';
        
        const response = await api.statistics.getRanking(sortBy);
        const rankings = response.data;
        
        if (!rankings || rankings.length === 0) {
            container.innerHTML = '<div class="alert alert-info">暂无销售排名数据</div>';
            return;
        }
        
        // 创建表格
        let tableHtml = `
            <table class="table table-striped table-hover">
                <thead class="table-light">
                    <tr>
                        <th>排名</th>
                        <th>产品/农户</th>
                        <th>销售额</th>
                        <th>订单数</th>
                        <th>平均评分</th>
                    </tr>
                </thead>
                <tbody>
        `;
        
        // 判断排名类型
        const isProductRanking = rankings[0].hasOwnProperty('productName');
        
        // 生成表格行
        rankings.forEach((item, index) => {
            const name = isProductRanking ? item.productName : item.farmerName;
            const starRating = renderStarRating(item.avgRating);
            
            tableHtml += `
                <tr>
                    <td><span class="badge ${getBadgeClass(index)}">${index + 1}</span></td>
                    <td>${name}</td>
                    <td>￥${item.totalSales.toFixed(2)}</td>
                    <td>${item.orderCount}</td>
                    <td>${starRating}</td>
                </tr>
            `;
        });
        
        tableHtml += `
                </tbody>
            </table>
        `;
        
        container.innerHTML = tableHtml;
    } catch (error) {
        console.error('加载销售排名失败:', error);
        document.getElementById(containerId).innerHTML = `<div class="alert alert-danger">加载销售排名失败: ${error.message}</div>`;
    }
}

// 获取排名徽章样式
function getBadgeClass(index) {
    switch (index) {
        case 0: return 'bg-warning text-dark';
        case 1: return 'bg-secondary';
        case 2: return 'bg-danger';
        default: return 'bg-light text-dark';
    }
}

// 渲染星级评分
function renderStarRating(rating) {
    if (!rating) return '无评分';
    
    const stars = Math.round(rating);
    let html = '';
    
    for (let i = 1; i <= 5; i++) {
        if (i <= stars) {
            html += '<i class="fas fa-star text-warning"></i>';
        } else {
            html += '<i class="fas fa-star text-muted"></i>';
        }
    }
    
    return `${html} (${rating.toFixed(1)})`;
}

// 全局导出
window.statisticsModule = {
    loadSalesOverview,
    loadSalesTrends,
    loadSalesRanking
}; 
 
 
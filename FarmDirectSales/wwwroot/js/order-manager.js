/**
 * 订单管理工具
 * @description 处理订单状态流转、验证和显示
 */

const OrderManager = {
    // 订单状态常量
    STATUS: {
        PENDING_PAYMENT: '待付款',
        PAID: '已付款',
        PROCESSING: '处理中',
        SHIPPED: '已发货',
        DELIVERING: '配送中',
        DELIVERED: '已送达',
        COMPLETED: '已完成',
        CANCELLED: '已取消',
        REFUNDING: '退款中',
        REFUNDED: '已退款',
        COD_PENDING: '货到付款待处理',
        COD_SHIPPING: '货到付款配送中'
    },
    
    // 支付方式常量
    PAYMENT_METHOD: {
        ONLINE: '在线支付',
        COD: '货到付款'
    },
    
    // 状态流转规则
    // 键: 当前状态，值: 允许转换到的状态数组
    STATUS_FLOW: {
        '待付款': ['货到付款配送中', '已取消'],
        '已付款': ['货到付款配送中', '已取消'],
        '货到付款待处理': ['货到付款配送中', '已取消'],
        '货到付款配送中': ['已送达', '已取消'],
        '已送达': ['已完成'],
        '已完成': [],
        '已取消': [],
    },
    
    // 状态转换需要的角色权限
    // 键: 状态转换(from->to)，值: 允许执行此操作的角色数组
    STATUS_PERMISSIONS: {
        '待付款->货到付款配送中': ['farmer'],
        '待付款->已取消': ['customer', 'farmer', 'admin'],
        '已付款->货到付款配送中': ['farmer'],
        '已付款->已取消': ['customer', 'admin'],
        '货到付款待处理->货到付款配送中': ['farmer'],
        '货到付款待处理->已取消': ['customer', 'farmer', 'admin'],
        '货到付款配送中->已送达': ['customer'],
        '货到付款配送中->已取消': ['farmer', 'admin'],
        '已送达->已完成': ['customer', 'system']
    },
    
    /**
     * 获取订单的下一步可能状态
     * @param {string} currentStatus - 当前状态
     * @param {string} userRole - 用户角色
     * @returns {Array} 可转换的状态列表
     */
    getNextStatuses(currentStatus, userRole) {
        const possibleStatuses = this.STATUS_FLOW[currentStatus] || [];
        
        // 根据用户角色过滤可用的状态转换
        return possibleStatuses.filter(nextStatus => {
            const transition = `${currentStatus}->${nextStatus}`;
            const allowedRoles = this.STATUS_PERMISSIONS[transition] || [];
            return allowedRoles.includes(userRole) || allowedRoles.includes('system');
        });
    },
    
    /**
     * 检查状态转换是否允许
     * @param {string} fromStatus - 当前状态
     * @param {string} toStatus - 目标状态
     * @param {string} userRole - 用户角色
     * @returns {boolean} 是否允许转换
     */
    isTransitionAllowed(fromStatus, toStatus, userRole) {
        // 检查是否在流转规则中允许
        const allowedNextStatuses = this.STATUS_FLOW[fromStatus] || [];
        if (!allowedNextStatuses.includes(toStatus)) {
            return false;
        }
        
        // 检查用户角色是否有权限
        const transition = `${fromStatus}->${toStatus}`;
        const allowedRoles = this.STATUS_PERMISSIONS[transition] || [];
        return allowedRoles.includes(userRole) || allowedRoles.includes('system');
    },
    
    /**
     * 更新订单状态
     * @param {number} orderId - 订单ID
     * @param {string} newStatus - 新状态
     * @param {string} remark - 备注信息
     * @returns {Promise<object>} 返回操作结果
     */
    async updateOrderStatus(orderId, newStatus, remark = '') {
        try {
            // 获取当前用户信息
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId || !user.role) {
                throw new Error('用户未登录或信息不完整');
            }
            
            // 获取订单当前状态
            const orderResponse = await http.get(`/api/order/${orderId}`);
            const order = orderResponse.data;
            
            if (!order) {
                throw new Error('订单不存在');
            }
            
            // 检查状态转换是否有效
            if (!this.isTransitionAllowed(order.status, newStatus, user.role)) {
                throw new Error(`无法将订单从"${order.status}"状态更新为"${newStatus}"状态`);
            }

            let response;
            
            // 根据目标状态使用不同的API端点
            if (newStatus === '货到付款配送中') {
                // 使用ship端点开始配送
                response = await http.put(`/api/order/${orderId}/ship`, {
                    farmerId: user.userId
                });
            } else if (newStatus === '已完成') {
                // 使用complete端点确认收货
                response = await http.put(`/api/order/${orderId}/complete`, {
                    userId: user.userId
                });
            } else if (newStatus === '已取消') {
                // 使用cancel端点取消订单
                response = await http.put(`/api/order/${orderId}/cancel`, {
                    userId: user.userId,
                    cancelReason: remark
                });
            } else {
                // 这里可能需要实现其他状态的处理，否则可能会有问题
                // 如果需要通用的状态更新端点，需要在OrderController.cs中添加
                throw new Error(`状态 "${newStatus}" 的更新尚未实现API`);
            }
            
            return response;
        } catch (error) {
            console.error('更新订单状态失败:', error);
            throw error;
        }
    },
    
    /**
     * 获取订单状态的展示样式
     * @param {string} status - 订单状态
     * @returns {object} 样式配置
     */
    getStatusStyle(status) {
        const styles = {
            '待付款': { color: 'warning', icon: 'fa-clock' },
            '已付款': { color: 'info', icon: 'fa-credit-card' },
            '已送达': { color: 'success', icon: 'fa-box-open' },
            '已完成': { color: 'success', icon: 'fa-check-circle' },
            '已取消': { color: 'secondary', icon: 'fa-ban' },
            '货到付款待处理': { color: 'primary', icon: 'fa-box' },
            '货到付款配送中': { color: 'info', icon: 'fa-truck' }
        };
        
        return styles[status] || { color: 'secondary', icon: 'fa-question-circle' };
    },
    
    /**
     * 渲染订单状态标签
     * @param {string} status - 订单状态
     * @returns {string} HTML标签字符串
     */
    renderStatusBadge(status) {
        const style = this.getStatusStyle(status);
        return `<span class="badge bg-${style.color}"><i class="fas ${style.icon} me-1"></i> ${status}</span>`;
    },
    
    /**
     * 判断订单是否可以评分
     * @param {object} order - 订单对象
     * @returns {boolean} 是否可以评分
     */
    canRate(order) {
        return order.status === '已完成' && !order.isRated;
    },
    
    /**
     * 判断是否显示确认收货按钮
     * @param {object} order - 订单对象
     * @param {string} userRole - 用户角色
     * @returns {boolean} 是否显示确认收货按钮
     */
    showConfirmReceiptButton(order, userRole) {
        return userRole === 'customer' && order.status === '货到付款配送中';
    },
    
    /**
     * 判断是否显示开始配送按钮
     * @param {object} order - 订单对象
     * @param {string} userRole - 用户角色
     * @returns {boolean} 是否显示开始配送按钮
     */
    showStartDeliveryButton(order, userRole) {
        return userRole === 'farmer' && 
              (order.status === '待付款' || 
               order.status === '已付款' || 
               order.status === '货到付款待处理');
    },
    
    /**
     * 获取订单状态的进度条信息
     * @param {string} status - 当前状态
     * @param {string} paymentMethod - 支付方式（已不再使用）
     * @returns {object} 进度信息
     */
    getOrderProgress(status) {
        // 所有订单统一使用货到付款流程
        const standardFlow = ['货到付款待处理', '货到付款配送中', '已送达', '已完成'];
        
        // 非标准流程的特殊处理
        if (status === '已取消') {
            return { percent: 0, step: 0, isSpecial: true, status };
        }
        
        // 将所有状态映射到货到付款流程
        let mappedStatus = status;
        if (status === '待付款' || status === '待支付' || status === '已付款') {
            mappedStatus = '货到付款待处理';
        }
        
        // 标准流程的进度计算
        const step = standardFlow.indexOf(mappedStatus);
        if (step === -1) {
            return { percent: 0, step: 0, isSpecial: false, status };
        }
        
        const percent = Math.round((step / (standardFlow.length - 1)) * 100);
        return { percent, step, isSpecial: false, status };
    }
};

// 导出为全局变量
window.OrderManager = OrderManager; 
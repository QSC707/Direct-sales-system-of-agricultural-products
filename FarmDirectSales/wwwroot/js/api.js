/**
 * API调用封装
 * 统一管理所有与后端的接口交互
 */

// API基础URL
const API_BASE_URL = 'http://localhost:5004/api';

// 获取存储的token
const getToken = () => localStorage.getItem('token');

// 统一处理请求头
const getHeaders = () => {
    const headers = {
        'Content-Type': 'application/json'
    };
    
    const token = getToken();
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }
    
    return headers;
};

// 打印登录信息到控制台（调试用）
const printLoginInfo = () => {
    const token = localStorage.getItem('token');
    const userStr = localStorage.getItem('user');
    console.log('=== 登录信息 ===');
    console.log('Token:', token ? '存在' : '不存在');
    console.log('User数据:', userStr);
    
    try {
        if (userStr) {
            const user = JSON.parse(userStr);
            console.log('用户ID:', user.userId);
            console.log('用户名:', user.username);
            console.log('角色:', user.role);
        }
    } catch (e) {
        console.error('解析用户数据失败:', e);
    }
    console.log('================');
};

// 统一处理响应
const handleResponse = async (response) => {
    // 检查响应是否为空
    const text = await response.text();
    if (!text) {
        throw new Error('服务器返回空响应');
    }
    
    // 尝试解析JSON
    let data;
    try {
        data = JSON.parse(text);
    } catch (e) {
        console.error('解析响应JSON失败:', e);
        throw new Error('服务器返回的数据格式不正确');
    }
    
    if (!response.ok) {
        if (response.status === 401) {
            // 未授权，重定向到登录页面
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            
            // 记录当前页面URL，以便登录后返回
            const currentPath = window.location.pathname;
            const redirectUrl = encodeURIComponent(window.location.href);
            
            // 重定向到登录页面，并携带重定向参数
            window.location.href = `/pages/login.html?redirect=${redirectUrl}`;
        }
        
        throw new Error(data.message || '请求失败');
    }
    
    return data;
};

// 辅助函数：检查登录状态
const checkLoginStatus = () => {
    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    
    if (token && user && user.userId) {
        console.log('已登录', {
            token: token.substring(0, 10) + '...',
            user
        });
        return true;
    } else {
        console.log('未登录');
        return false;
    }
};

// 重置登录状态
const resetLoginStatus = () => {
    console.log('重置登录状态');
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    return false;
};

// API封装对象
const api = {
    // 调试功能
    checkLoginStatus,
    printLoginInfo,
    resetLoginStatus,
    
    // 用户认证相关
    auth: {
        // 登录
        login: async (username, password) => {
            try {
                const response = await fetch(`${API_BASE_URL}/auth/login`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ username, password })
                });
                
                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(errorData.message || `登录失败 (${response.status})`);
                }
                
                const data = await response.json();
                
                // 登录成功后处理
                if (data.code === 200) {
                    console.log('登录API响应数据:', data);
                    
                    // 验证返回的数据
                    if (!data.data) {
                        throw new Error('服务器返回的数据格式不正确');
                    }
                    
                    if (!data.data.token) {
                        throw new Error('服务器未返回有效的认证令牌');
                    }
                    
                    if (!data.data.userId) {
                        throw new Error('服务器未返回有效的用户ID');
                    }
                    
                    // 保存登录状态和token
                    localStorage.setItem('token', data.data.token);
                    
                    // 保存用户信息对象
                    const userData = {
                        userId: data.data.userId,
                        username: data.data.username || username,
                        role: data.data.role || 'customer',
                        email: data.data.email,
                        phone: data.data.phone
                    };
                    localStorage.setItem('user', JSON.stringify(userData));
                    
                    console.log('保存的用户数据:', userData);
                }
                
                return data;
            } catch (error) {
                console.error('登录API调用失败:', error);
                throw error;
            }
        },
        
        // 注册
        register: async (userData) => {
            console.log('注册数据:', userData); // 添加日志
            const response = await fetch(`${API_BASE_URL}/auth/register`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    username: userData.username,
                    password: userData.password,
                    role: userData.role,
                    email: userData.email || null,
                    phone: userData.phone,
                    // 农户特有字段
                    farmName: userData.farmName || null,
                    location: userData.location || null,
                    description: userData.description || null,
                    productCategory: userData.productCategory || null,
                    licenseNumber: userData.licenseNumber || null
                })
            });
            
            const result = await handleResponse(response);
            console.log('注册响应:', result); // 添加日志
            return result;
        }
    },
    
    // 产品相关
    products: {
        // 获取产品列表
        getAll: async () => {
            const response = await fetch(`${API_BASE_URL}/product`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取产品详情
        getById: async (productId) => {
            const response = await fetch(`${API_BASE_URL}/product/${productId}`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 添加产品
        add: async (productData) => {
            const response = await fetch(`${API_BASE_URL}/product`, {
                method: 'POST',
                headers: getHeaders(),
                body: JSON.stringify(productData)
            });
            
            return handleResponse(response);
        },
        
        // 更新产品
        update: async (productId, productData) => {
            const response = await fetch(`${API_BASE_URL}/product/${productId}`, {
                method: 'PUT',
                headers: getHeaders(),
                body: JSON.stringify(productData)
            });
            
            return handleResponse(response);
        },
        
        // 删除产品
        delete: async (productId) => {
            const response = await fetch(`${API_BASE_URL}/product/${productId}`, {
                method: 'DELETE',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        }
    },
    
    // 订单相关
    orders: {
        // 获取用户订单
        getUserOrders: async () => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${API_BASE_URL}/order/user/${user.userId}`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取农户订单
        getFarmerOrders: async () => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${API_BASE_URL}/order/farmer/${user.userId}`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 创建订单
        create: async (orderData) => {
            const response = await fetch(`${API_BASE_URL}/order`, {
                method: 'POST',
                headers: getHeaders(),
                body: JSON.stringify(orderData)
            });
            
            return handleResponse(response);
        },
        
        // 获取订单详情
        getById: async (orderId) => {
            const response = await fetch(`${API_BASE_URL}/order/${orderId}`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 支付订单
        payOrder: async (orderId) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${API_BASE_URL}/order/${orderId}/pay`, {
                method: 'PUT',
                headers: getHeaders(),
                body: JSON.stringify({ userId: user.userId })
            });
            
            return handleResponse(response);
        },
        
        // 确认收货
        confirmReceipt: async (orderId) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${API_BASE_URL}/order/${orderId}/complete`, {
                method: 'PUT',
                headers: getHeaders(),
                body: JSON.stringify({ userId: user.userId })
            });
            
            return handleResponse(response);
        },
        
        // 取消订单
        cancelOrder: async (orderId) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${API_BASE_URL}/order/${orderId}/cancel`, {
                method: 'PUT',
                headers: getHeaders(),
                body: JSON.stringify({ userId: user.userId })
            });
            
            return handleResponse(response);
        },
        
        // 从购物车创建订单
        createFromCart: async (addressInfo) => {
            try {
                const user = JSON.parse(localStorage.getItem('user') || '{}');
                if (!user.userId) {
                    throw new Error('用户未登录');
                }
                
                // 1. 获取购物车数据
                const cartResponse = await api.cart.getCart();
                const cartData = cartResponse.data;
                
                if (!cartData || !cartData.items || cartData.items.length === 0) {
                    throw new Error('购物车为空');
                }
                
                // 2. 为每个购物车项创建订单
                const orderPromises = cartData.items.map(item => {
                    return api.orders.create({
                        userId: user.userId,
                        productId: item.product.productId,
                        quantity: item.quantity,
                        shippingAddress: addressInfo.shippingAddress,
                        contactPhone: addressInfo.contactPhone
                    });
                });
                
                const results = await Promise.all(orderPromises);
                
                // 3. 清空购物车
                await api.cart.clearCart();
                
                return {
                    code: 200,
                    message: "订单创建成功",
                    data: results.map(r => r.data)
                };
            } catch (error) {
                console.error('从购物车创建订单失败:', error);
                throw error;
            }
        }
    },
    
    // 评价相关
    reviews: {
        // 获取产品评价
        getByProduct: async (productId) => {
            const response = await fetch(`${API_BASE_URL}/review/product/${productId}`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取用户评价
        getByUser: async () => {
            const response = await fetch(`${API_BASE_URL}/review/user`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 添加评价
        add: async (reviewData) => {
            const response = await fetch(`${API_BASE_URL}/review`, {
                method: 'POST',
                headers: getHeaders(),
                body: JSON.stringify(reviewData)
            });
            
            return handleResponse(response);
        },
        
        // 更新评价
        update: async (reviewId, reviewData) => {
            const response = await fetch(`${API_BASE_URL}/review/${reviewId}`, {
                method: 'PUT',
                headers: getHeaders(),
                body: JSON.stringify(reviewData)
            });
            
            return handleResponse(response);
        },
        
        // 删除评价
        delete: async (reviewId) => {
            const response = await fetch(`${API_BASE_URL}/review/${reviewId}`, {
                method: 'DELETE',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        }
    },
    
    // 统计相关
    statistics: {
        // 获取总览数据
        getOverview: async () => {
            const response = await fetch(`${API_BASE_URL}/statistics/overview`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取销售趋势
        getTrends: async (period) => {
            const response = await fetch(`${API_BASE_URL}/statistics/trends?period=${period}`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取销售排名
        getRanking: async (sortBy) => {
            const response = await fetch(`${API_BASE_URL}/statistics/ranking?sortBy=${sortBy}`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        }
    },
    
    // 购物车相关
    cart: {
        // 获取用户购物车
        getCart: async () => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${API_BASE_URL}/cart/user/${user.userId}`, {
                method: 'GET',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 添加商品到购物车
        addToCart: async (productId, quantity) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${API_BASE_URL}/cart`, {
                method: 'POST',
                headers: getHeaders(),
                body: JSON.stringify({
                    userId: user.userId,
                    productId,
                    quantity
                })
            });
            
            return handleResponse(response);
        },
        
        // 更新购物车项数量
        updateCartItem: async (cartItemId, quantity) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${API_BASE_URL}/cart/${cartItemId}`, {
                method: 'PUT',
                headers: getHeaders(),
                body: JSON.stringify({
                    userId: user.userId,
                    quantity
                })
            });
            
            return handleResponse(response);
        },
        
        // 删除购物车项
        removeCartItem: async (cartItemId) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${API_BASE_URL}/cart/${cartItemId}`, {
                method: 'DELETE',
                headers: getHeaders(),
                body: JSON.stringify({
                    userId: user.userId
                })
            });
            
            return handleResponse(response);
        },
        
        // 清空购物车
        clearCart: async () => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${API_BASE_URL}/cart/clear/${user.userId}`, {
                method: 'DELETE',
                headers: getHeaders()
            });
            
            return handleResponse(response);
        }
    }
};

// 导出API对象
window.api = api; 
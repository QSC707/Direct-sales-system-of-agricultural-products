/**
 * API调用封装
 * 统一管理所有与后端的接口交互
 */

// API基础URL - 使用全局window对象属性，避免重复声明
if (typeof window.API_BASE_URL === 'undefined') {
    window.API_BASE_URL = 'http://localhost:5004/api';
}

// 获取存储的token - 设置为全局函数，避免重复声明
window.getToken = () => localStorage.getItem('token');

// 统一处理请求头 - 设置为全局函数，避免重复声明
window.getHeaders = () => {
    const headers = {
        'Content-Type': 'application/json'
    };
    
    const token = getToken();
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }
    
    return headers;
};

// 打印登录信息到控制台（调试用）- 设置为全局函数，避免重复声明
window.printLoginInfo = () => {
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
        // 解析JWT令牌获取角色信息
        try {
            // JWT令牌格式是以.分隔的三部分
            const parts = token.split('.');
            if (parts.length === 3) {
                // 解码JWT的payload部分（第二部分）
                const payload = JSON.parse(atob(parts[1]));
                // 从payload中获取角色信息
                const tokenRole = payload.role;
                const tokenUserId = payload.nameid;
                
                // 检查令牌中的角色是否与localStorage中的角色匹配
                if (tokenRole && user.role && tokenRole !== user.role) {
                    console.error('令牌角色与存储角色不匹配，重置登录状态:', {
                        tokenRole: tokenRole,
                        localRole: user.role
                    });
                    resetLoginStatus();
                    return false;
                }
                
                // 检查令牌中的用户ID是否与localStorage中的用户ID匹配
                if (tokenUserId && user.userId && tokenUserId != user.userId) {
                    console.error('令牌用户ID与存储用户ID不匹配，重置登录状态:', {
                        tokenUserId: tokenUserId,
                        localUserId: user.userId
                    });
                    resetLoginStatus();
                    return false;
                }
                
                // 检查令牌是否过期
                const currentTime = Math.floor(Date.now() / 1000);
                if (payload.exp && payload.exp < currentTime) {
                    console.error('令牌已过期，重置登录状态');
                    resetLoginStatus();
                    return false;
                }
            }
        } catch (e) {
            console.error('解析JWT令牌出错:', e);
            // 解析失败时，保守起见重置登录状态
            resetLoginStatus();
            return false;
        }
        
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

// 封装带授权的fetch请求
const fetchWithAuth = async (url, options = {}) => {
    // 确保请求头设置正确
    options.headers = options.headers || {};
    Object.assign(options.headers, window.getHeaders());
    
    try {
        const response = await fetch(url, options);
        return handleResponse(response);
    } catch (error) {
        console.error('API请求失败:', error);
        throw error;
    }
};

// API封装对象
const api = {
    // 调试功能
    checkLoginStatus,
    printLoginInfo: window.printLoginInfo,
    resetLoginStatus,
    
    // 用户认证相关
    auth: {
        // 登录 - 支持用户名或手机号码
        login: async (usernameOrPhone, password) => {
            try {
                const response = await fetch(`${window.API_BASE_URL}/auth/login`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ username: usernameOrPhone, password })
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
                    
                    // 保存JWT令牌
                    const token = data.data.token;
                    localStorage.setItem('token', token);
                    
                    // 解析JWT令牌获取实际角色信息
                    let tokenRole = null;
                    try {
                        const parts = token.split('.');
                        if (parts.length === 3) {
                            const payload = JSON.parse(atob(parts[1]));
                            // 从payload中获取角色信息
                            tokenRole = payload.role;
                            if (!tokenRole && payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]) {
                                tokenRole = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
                            }
                            console.log('JWT令牌解析:', payload);
                            console.log('令牌中的角色:', tokenRole);
                        }
                    } catch (e) {
                        console.error('解析JWT令牌失败:', e);
                    }
                    
                    // 保存用户信息对象，优先使用JWT令牌中的角色
                    const userData = {
                        userId: data.data.userId,
                        username: data.data.username || usernameOrPhone,
                        role: tokenRole || data.data.role || 'customer', // 优先使用令牌中的角色
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
            const response = await fetch(`${window.API_BASE_URL}/auth/register`, {
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
    product: {
        // 获取产品列表
        getProducts: async () => {
            try {
            const response = await fetch(`${window.API_BASE_URL}/product`, {
                method: 'GET',
                headers: window.getHeaders()
            });
                return await handleResponse(response);
            } catch (error) {
                throw new Error('获取产品列表失败');
            }
        },
        
        // 获取农户的产品列表
        getFarmerProducts: async (farmerId) => {
            try {
                const response = await fetch(`${window.API_BASE_URL}/product?farmerId=${farmerId}`, {
                    method: 'GET',
                    headers: window.getHeaders()
                });
                return await handleResponse(response);
            } catch (error) {
                throw new Error('获取农户的产品列表失败');
            }
        },
        
        // 获取产品详情
        getProduct: async (productId) => {
            try {
            const response = await fetch(`${window.API_BASE_URL}/product/${productId}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
                return await handleResponse(response);
            } catch (error) {
                throw new Error('获取产品详情失败');
            }
        },
        
        // 添加产品
        addProduct: async (productData) => {
            try {
                // 确保包含所有必要字段
                const formattedData = {
                    productName: productData.productName,
                    description: productData.description,
                    price: productData.price,
                    stock: productData.stock,
                    imageUrl: productData.imageUrl,
                    farmerId: productData.farmerId,
                    category: productData.category,
                    // 添加扩展字段
                    specification: productData.specification,
                    isOrganic: productData.isOrganic,
                    harvestDate: productData.harvestDate,
                    shelfLife: productData.shelfLife,
                    // 添加溯源信息
                    sourcePlace: productData.sourcePlace,
                    plantingMethod: productData.plantingMethod,
                    plantingTime: productData.plantingTime,
                    qualityLevel: productData.qualityLevel,
                    traceInfo: productData.traceInfo
                };
                
            const response = await fetch(`${window.API_BASE_URL}/product`, {
                method: 'POST',
                headers: window.getHeaders(),
                    body: JSON.stringify(formattedData)
            });
                return response;
            } catch (error) {
                throw new Error('添加产品失败');
            }
        },
        
        // 更新产品
        updateProduct: async (productId, productData) => {
            try {
                // 确保包含所有必要字段
                const formattedData = {
                    productName: productData.productName,
                    description: productData.description,
                    price: productData.price,
                    stock: productData.stock,
                    imageUrl: productData.imageUrl,
                    farmerId: productData.farmerId,
                    category: productData.category,
                    isActive: productData.isActive,
                    // 添加扩展字段
                    specification: productData.specification,
                    isOrganic: productData.isOrganic,
                    harvestDate: productData.harvestDate,
                    shelfLife: productData.shelfLife,
                    // 添加溯源信息
                    sourcePlace: productData.sourcePlace,
                    plantingMethod: productData.plantingMethod,
                    plantingTime: productData.plantingTime,
                    qualityLevel: productData.qualityLevel,
                    traceInfo: productData.traceInfo
                };
                
            const response = await fetch(`${window.API_BASE_URL}/product/${productId}`, {
                method: 'PUT',
                headers: window.getHeaders(),
                    body: JSON.stringify(formattedData)
            });
                return response;
            } catch (error) {
                throw new Error('更新产品失败');
            }
        },
        
        // 删除产品
        deleteProduct: async (productId, farmerId, hardDelete = false) => {
            try {
            const response = await fetch(`${window.API_BASE_URL}/product/${productId}`, {
                method: 'DELETE',
                    headers: window.getHeaders(),
                    body: JSON.stringify({
                        farmerId: farmerId,
                        hardDelete: hardDelete
                    })
                });
                return response;
            } catch (error) {
                throw new Error('删除产品失败');
            }
        },
        
        // 搜索产品
        searchProducts: async (keyword) => {
            try {
                const response = await fetch(`${window.API_BASE_URL}/product/search?keyword=${encodeURIComponent(keyword)}`, {
                    method: 'GET',
                    headers: window.getHeaders()
                });
                return await handleResponse(response);
            } catch (error) {
                throw new Error('搜索产品失败');
            }
        },
        
        // 获取分类产品
        getCategoryProducts: async (category) => {
            try {
                const response = await fetch(`${window.API_BASE_URL}/product/category/${encodeURIComponent(category)}`, {
                    method: 'GET',
                    headers: window.getHeaders()
            });
                return await handleResponse(response);
            } catch (error) {
                throw new Error('获取分类产品失败');
            }
        },
        
        // 获取产品销量
        getProductSales: async (productId) => {
            try {
                console.log(`调用销量API: 产品ID=${productId}`);
                const response = await fetch(`${window.API_BASE_URL}/product/${productId}/sales`, {
                    method: 'GET',
                    headers: window.getHeaders()
                });
                
                // 检查响应状态
                if (!response.ok) {
                    console.error(`销量API响应错误: HTTP ${response.status}`);
                    return { code: response.status, message: "获取销量失败", data: 0 };
                }
                
                // 检查响应内容
                const text = await response.text();
                if (!text) {
                    console.error('销量API返回空响应');
                    return { code: 200, message: "无销量数据", data: 0 };
                }
                
                // 尝试解析JSON
                try {
                    const data = JSON.parse(text);
                    console.log('销量API返回数据:', data);
                    return data;
                } catch (parseError) {
                    console.error('解析销量数据失败:', parseError, '原始数据:', text);
                    return { code: 200, message: "销量数据格式错误", data: 0 };
                }
            } catch (error) {
                console.error('获取产品销量失败:', error);
                return { code: 500, message: error.message, data: 0 }; // 返回友好的错误对象而不是抛出异常
            }
        }
    },
    
    // 农户相关
    farmers: {
        // 获取所有农户列表
        getAll: async () => {
            try {
                const response = await fetch(`${window.API_BASE_URL}/farmer`, {
                    method: 'GET',
                    headers: window.getHeaders()
                });
                return await handleResponse(response);
            } catch (error) {
                throw new Error('获取农户列表失败');
            }
        },
        
        // 获取农户详情
        getById: async (farmerId) => {
            try {
                const response = await fetch(`${window.API_BASE_URL}/farmer/${farmerId}`, {
                    method: 'GET',
                    headers: window.getHeaders()
                });
                return await handleResponse(response);
            } catch (error) {
                throw new Error('获取农户详情失败');
            }
        },
        
        // 获取农户的产品列表
        getProducts: async (farmerId) => {
            try {
                const response = await fetch(`${window.API_BASE_URL}/farmer/${farmerId}/products`, {
                    method: 'GET',
                    headers: window.getHeaders()
                });
                return await handleResponse(response);
            } catch (error) {
                throw new Error('获取农户产品列表失败');
            }
        },
        
        // 获取农户的订单列表（需要权限）
        getOrders: async (farmerId, status = null, page = 1, pageSize = 10) => {
            try {
                let url = `${window.API_BASE_URL}/farmer/${farmerId}/orders?page=${page}&pageSize=${pageSize}`;
                if (status) {
                    url += `&status=${status}`;
                }
                
                const response = await fetch(url, {
                    method: 'GET',
                    headers: window.getHeaders()
                });
                return await handleResponse(response);
            } catch (error) {
                throw new Error('获取农户订单列表失败');
            }
        }
    },
    
    // 订单相关
    orders: {
        // 获取订单列表
        getAll: async (status = null, page = 1, pageSize = 10) => {
            let url = `${window.API_BASE_URL}/order?page=${page}&pageSize=${pageSize}`;
            if (status) {
                url += `&status=${status}`;
            }
            
            const response = await fetch(url, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取订单详情
        getById: async (orderId) => {
            const response = await fetch(`${window.API_BASE_URL}/order/${orderId}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 创建订单
        create: async (orderData) => {
            const response = await fetch(`${window.API_BASE_URL}/order`, {
                method: 'POST',
                headers: window.getHeaders(),
                body: JSON.stringify(orderData)
            });
            
            return handleResponse(response);
        },
        
        // 从购物车创建订单
        createFromCart: async function(data) {
            try {
                const token = localStorage.getItem('token');
                const response = await fetch(`${window.API_BASE_URL}/order/from-cart`, {
                    method: 'POST',
                    headers: window.getHeaders(),
                    body: JSON.stringify({
                        userId: data.userId,
                        shippingAddress: data.shippingAddress,
                        contactPhone: data.contactPhone,
                        receiverName: data.receiverName,
                        selectedItems: data.selectedItems || [],
                        shippingFeeId: data.shippingFeeId,
                        deliveryAreaId: data.deliveryAreaId
                    })
                });
                
                return handleResponse(response);
            } catch (error) {
                console.error('创建订单失败', error);
                throw error;
            }
        },
        
        // 立即购买创建订单
        createDirectOrder: async function(data) {
            try {
                const token = localStorage.getItem('token');
                const response = await fetch(`${window.API_BASE_URL}/order/direct-buy`, {
                method: 'POST',
                headers: window.getHeaders(),
                body: JSON.stringify({
                        userId: data.userId,
                        productId: data.productId,
                        quantity: data.quantity,
                        shippingAddress: data.shippingAddress,
                        contactPhone: data.contactPhone,
                        receiverName: data.receiverName,
                        deliveryMethod: data.deliveryMethod,
                        shippingFee: data.shippingFee,
                        shippingFeeId: data.shippingFeeId
                })
            });
            
            return handleResponse(response);
            } catch (error) {
                console.error('创建订单失败', error);
                throw error;
            }
        },
        
        // 更新订单状态
        updateStatus: async (orderId, status, remark = '') => {
            const response = await fetch(`${window.API_BASE_URL}/order/${orderId}/status`, {
                method: 'PUT',
                headers: window.getHeaders(),
                body: JSON.stringify({ 
                    status,
                    remark
                })
            });
            
            return handleResponse(response);
        },
        
        // 取消订单
        cancel: async (orderId, reason) => {
            const response = await fetch(`${window.API_BASE_URL}/order/${orderId}/cancel`, {
                method: 'PUT',
                headers: window.getHeaders(),
                body: JSON.stringify({ reason })
            });
            
            return handleResponse(response);
        },
        
        // 获取农户订单
        getFarmerOrders: async (farmerId, status = null, page = 1, pageSize = 10) => {
            let url = `${window.API_BASE_URL}/order/farmer/${farmerId}?page=${page}&pageSize=${pageSize}`;
            if (status) {
                url += `&status=${status}`;
            }
            
            const response = await fetch(url, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        }
    },
    
    // 订单组相关API
    orderGroups: {
        // 获取用户的订单组列表
        getUserOrderGroups: async (userId) => {
            const response = await fetch(`${window.API_BASE_URL}/ordergroup/user/${userId}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取订单组详情
        getOrderGroupById: async (groupId) => {
            const response = await fetch(`${window.API_BASE_URL}/ordergroup/${groupId}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取订单组的所有订单
        getOrdersInGroup: async (groupId) => {
            const response = await fetch(`${window.API_BASE_URL}/ordergroup/${groupId}/orders`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        }
    },
    
    // 统计相关
    statistics: {
        // 获取总览数据
        getOverview: async () => {
            const response = await fetch(`${window.API_BASE_URL}/statistics/overview`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取销售趋势
        getTrends: async (period) => {
            const response = await fetch(`${window.API_BASE_URL}/statistics/trends?period=${period}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取销售排名
        getRanking: async (sortBy) => {
            const response = await fetch(`${window.API_BASE_URL}/statistics/ranking?sortBy=${sortBy}`, {
                method: 'GET',
                headers: window.getHeaders()
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
            
            const response = await fetch(`${window.API_BASE_URL}/cart/user/${user.userId}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 添加商品到购物车
        addToCart: async (productId, quantity) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${window.API_BASE_URL}/cart`, {
                method: 'POST',
                headers: window.getHeaders(),
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
            
            const response = await fetch(`${window.API_BASE_URL}/cart/${cartItemId}`, {
                method: 'PUT',
                headers: window.getHeaders(),
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
            
            const response = await fetch(`${window.API_BASE_URL}/cart/${cartItemId}`, {
                method: 'DELETE',
                headers: window.getHeaders(),
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
            
            const response = await fetch(`${window.API_BASE_URL}/cart/clear/${user.userId}`, {
                method: 'DELETE',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        }
    },
    
    // 地址相关
    address: {
        // 获取用户保存的地址列表
        getSavedAddresses: async () => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${window.API_BASE_URL}/address/user/${user.userId}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取单个地址详情
        getAddressById: async (addressId) => {
            const response = await fetch(`${window.API_BASE_URL}/address/${addressId}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 添加新地址
        addAddress: async (addressData) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            // 确保addressData包含userId
            addressData.userId = user.userId;
            
            const response = await fetch(`${window.API_BASE_URL}/address`, {
                method: 'POST',
                headers: window.getHeaders(),
                body: JSON.stringify(addressData)
            });
            
            return handleResponse(response);
        },
        
        // 更新地址
        updateAddress: async (addressId, addressData) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            // 确保addressData包含userId
            addressData.userId = user.userId;
            
            const response = await fetch(`${window.API_BASE_URL}/address/${addressId}`, {
                method: 'PUT',
                headers: window.getHeaders(),
                body: JSON.stringify(addressData)
            });
            
            return handleResponse(response);
        },
        
        // 删除地址
        deleteAddress: async (addressId) => {
            const response = await fetch(`${window.API_BASE_URL}/address/${addressId}`, {
                method: 'DELETE',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 设置默认地址
        setDefaultAddress: async (addressId) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (!user.userId) {
                throw new Error('用户未登录');
            }
            
            const response = await fetch(`${window.API_BASE_URL}/address/${addressId}/set-default`, {
                method: 'PUT',
                headers: window.getHeaders(),
                body: JSON.stringify({
                    userId: user.userId
                })
            });
            
            return handleResponse(response);
        }
    },
    
    // 配送相关
    delivery: {
        // 获取当天配送支持的区域
        getSameDayAreas: async () => {
            const response = await fetch(`${window.API_BASE_URL}/delivery/sameday-areas`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 检查地址是否支持当天配送
        checkSameDayDelivery: async (province, city) => {
            const response = await fetch(`${window.API_BASE_URL}/delivery/check-sameday`, {
                method: 'POST',
                headers: window.getHeaders(),
                body: JSON.stringify({
                    province,
                    city
                })
            });
            
            return handleResponse(response);
        },
        
        // 获取配送费用
        getDeliveryFee: async (province, city, deliveryMethod) => {
            const response = await fetch(`${window.API_BASE_URL}/delivery/fee`, {
                method: 'POST',
                headers: window.getHeaders(),
                body: JSON.stringify({
                    province,
                    city,
                    deliveryMethod
                })
            });
            
            return handleResponse(response);
        },
        
        // 获取预计送达时间
        getEstimatedDeliveryTime: async (province, city, deliveryMethod) => {
            const response = await fetch(`${window.API_BASE_URL}/delivery/estimated-time`, {
                method: 'POST',
                headers: window.getHeaders(),
                body: JSON.stringify({
                    province,
                    city,
                    deliveryMethod
                })
            });
            
            return handleResponse(response);
        }
    },
    
    // 配送区域管理相关
    deliveryArea: {
        // 获取所有配送区域
        getAll: async () => {
            const response = await fetch(`${window.API_BASE_URL}/delivery-area`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取当天配送区域
        getSameDayAreas: async () => {
            const response = await fetch(`${window.API_BASE_URL}/delivery-area/sameday`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 添加配送区域（仅管理员）
        add: async (areaData) => {
            const response = await fetch(`${window.API_BASE_URL}/delivery-area`, {
                method: 'POST',
                headers: window.getHeaders(),
                body: JSON.stringify(areaData)
            });
            
            return handleResponse(response);
        },
        
        // 批量导入配送区域（仅管理员）
        batchImport: async (areasData) => {
            const response = await fetch(`${window.API_BASE_URL}/delivery-area/batch`, {
                method: 'POST',
                headers: window.getHeaders(),
                body: JSON.stringify(areasData)
            });
            
            return handleResponse(response);
        },
        
        // 更新配送区域（仅管理员）
        update: async (areaId, areaData) => {
            const response = await fetch(`${window.API_BASE_URL}/delivery-area/${areaId}`, {
                method: 'PUT',
                headers: window.getHeaders(),
                body: JSON.stringify(areaData)
            });
            
            return handleResponse(response);
        },
        
        // 删除配送区域（仅管理员）
        delete: async (areaId) => {
            const response = await fetch(`${window.API_BASE_URL}/delivery-area/${areaId}`, {
                method: 'DELETE',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 初始化默认配送区域（仅管理员）
        initializeDefault: async () => {
            const response = await fetch(`${window.API_BASE_URL}/delivery-area/initialize`, {
                method: 'POST',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 检查地址是否在配送范围内
        checkDeliveryAvailability: async (address) => {
            const params = new URLSearchParams({
                province: address.province,
                city: address.city,
                district: address.district
            });
            
            const response = await fetch(`${window.API_BASE_URL}/delivery-area/check?${params.toString()}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        }
    },
    
    // 运费管理相关
    shippingFee: {
        // 获取所有运费规则
        getAll: async () => {
            const response = await fetch(`${window.API_BASE_URL}/shipping-fee`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 获取单个运费规则
        getById: async (feeId) => {
            const response = await fetch(`${window.API_BASE_URL}/shipping-fee/${feeId}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 添加运费规则（仅管理员）
        add: async (feeData) => {
            const response = await fetch(`${window.API_BASE_URL}/shipping-fee`, {
                method: 'POST',
                headers: window.getHeaders(),
                body: JSON.stringify(feeData)
            });
            
            return handleResponse(response);
        },
        
        // 更新运费规则（仅管理员）
        update: async (feeId, feeData) => {
            const response = await fetch(`${window.API_BASE_URL}/shipping-fee/${feeId}`, {
                method: 'PUT',
                headers: window.getHeaders(),
                body: JSON.stringify(feeData)
            });
            
            return handleResponse(response);
        },
        
        // 删除运费规则（仅管理员）
        delete: async (feeId) => {
            const response = await fetch(`${window.API_BASE_URL}/shipping-fee/${feeId}`, {
                method: 'DELETE',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        },
        
        // 计算运费
        calculate: async (orderData) => {
            const response = await fetch(`${window.API_BASE_URL}/shipping-fee/calculate`, {
                method: 'POST',
                headers: window.getHeaders(),
                body: JSON.stringify(orderData)
            });
            
            return handleResponse(response);
        },
        
        // 获取运费统计数据（仅管理员）
        getStatistics: async (startDate, endDate) => {
            const params = new URLSearchParams();
            if (startDate) params.append('startDate', startDate.toISOString());
            if (endDate) params.append('endDate', endDate.toISOString());
            
            const response = await fetch(`${window.API_BASE_URL}/shipping-fee/statistics?${params.toString()}`, {
                method: 'GET',
                headers: window.getHeaders()
            });
            
            return handleResponse(response);
        }
    },
    
    // 管理员专用功能
    admin: {
        // 获取所有用户
        getAllUsers: async () => {
            return await fetchWithAuth(`${window.API_BASE_URL}/admin/users`, {
                method: 'GET'
            });
        },
        
        // 删除默认商品数据
        clearDefaultProducts: async () => {
            return await fetchWithAuth(`${window.API_BASE_URL}/admin/products/clear-defaults`, {
                method: 'DELETE'
            });
        },
        
        // 获取系统统计数据
        getStatistics: async () => {
            return await fetchWithAuth(`${window.API_BASE_URL}/admin/statistics`, {
                method: 'GET'
            });
        }
    }
};

// 导出API对象
window.api = api; 

const API_ENDPOINTS = {
    // 用户相关
    LOGIN: '/api/auth/login',
    REGISTER: '/api/user/register',
    GET_USER_PROFILE: '/api/user/',
    UPDATE_USER_PROFILE: '/api/user/update',
    
    // 订单相关
    CREATE_ORDER: '/api/order',
    GET_USER_ORDERS: '/api/order/user/',
    COMPLETE_ORDER: '/api/order/{id}/complete',
    CANCEL_ORDER: '/api/order/{id}/cancel',
    REQUEST_REFUND: '/api/order/{id}/refund-request',
    PROCESS_REFUND: '/api/order/{id}/process-refund',
    
    // 产品相关
    GET_PRODUCTS: '/api/product',
    GET_PRODUCT: '/api/product/{id}',
    
    // 购物车相关
    GET_CART: '/api/cart',
    ADD_TO_CART: '/api/cart',
    UPDATE_CART_ITEM: '/api/cart/{id}',
    REMOVE_FROM_CART: '/api/cart/{id}',
    
    // 农户相关
    GET_FARMER_PROFILE: '/api/farmer/{id}',
    GET_FARMER_PRODUCTS: '/api/farmer/{id}/products',

    // 统计相关
    GET_STATISTICS: '/api/statistics'
}; 
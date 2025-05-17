/**
 * Axios配置文件
 * 统一配置Axios实例，处理请求响应拦截，提供通用的HTTP请求方法
 */

// 创建Axios实例
const axiosInstance = axios.create({
    baseURL: window.API_BASE_URL, // 使用全局API_BASE_URL
    timeout: 10000, // 请求超时时间10秒
    headers: {
        'Content-Type': 'application/json'
    }
});

// 使用api.js中已定义的getToken和getHeaders函数，避免重复声明
// const getToken = () => localStorage.getItem('token');
// const getHeaders已在api.js中定义，这里直接使用window.getHeaders

/**
 * 请求拦截器
 * 在请求发送前，自动添加token
 */
axiosInstance.interceptors.request.use(
    config => {
        const token = window.getToken ? window.getToken() : localStorage.getItem('token');
        if (token) {
            config.headers['Authorization'] = `Bearer ${token}`;
        }
        return config;
    },
    error => {
        console.error('请求错误:', error);
        return Promise.reject(error);
    }
);

/**
 * 响应拦截器
 * 统一处理响应数据和错误
 */
axiosInstance.interceptors.response.use(
    response => {
        // 直接返回响应数据
        return response.data;
    },
    error => {
        console.error('响应错误:', error);
        
        // 处理未授权(401)错误，重定向到登录页面
        if (error.response && error.response.status === 401) {
            // 清除登录状态
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            
            // 获取当前URL作为重定向参数
            const redirectUrl = encodeURIComponent(window.location.href);
            window.location.href = `/pages/login.html?redirect=${redirectUrl}`;
        }
        
        // 提取错误信息
        const errorMsg = error.response?.data?.message || '请求失败，请稍后重试';
        return Promise.reject(new Error(errorMsg));
    }
);

/**
 * 检查登录状态
 * @returns {boolean} 是否已登录
 */
const isLoggedIn = () => {
    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    return !!(token && user && user.userId);
};

/**
 * HTTP模块对象
 * 提供通用的HTTP请求方法，使用Axios实例发送请求
 */
const http = {
    /**
     * GET请求
     * @param {string} url 请求URL
     * @param {Object} params 查询参数
     * @returns {Promise} 响应数据
     */
    get: (url, params = {}) => {
        return axiosInstance.get(url, { params });
    },
    
    /**
     * POST请求
     * @param {string} url 请求URL
     * @param {Object} data 请求体数据
     * @returns {Promise} 响应数据
     */
    post: (url, data = {}) => {
        return axiosInstance.post(url, data);
    },
    
    /**
     * PUT请求
     * @param {string} url 请求URL
     * @param {Object} data 请求体数据
     * @returns {Promise} 响应数据
     */
    put: (url, data = {}) => {
        return axiosInstance.put(url, data);
    },
    
    /**
     * DELETE请求
     * @param {string} url 请求URL
     * @param {Object} data 请求体数据
     * @returns {Promise} 响应数据
     */
    delete: (url, data = {}) => {
        return axiosInstance.delete(url, { data });
    }
};

// 导出为全局变量，方便在其他页面使用
window.http = http;
window.isLoggedIn = isLoggedIn; 
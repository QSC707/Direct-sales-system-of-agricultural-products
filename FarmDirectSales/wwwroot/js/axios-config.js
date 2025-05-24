/**
 * Axios配置文件
 * 统一配置Axios实例，处理请求响应拦截，提供通用的HTTP请求方法
 */

// 检查Axios是否已加载
if (typeof axios === 'undefined') {
    console.warn('Axios未加载，正在尝试加载...');
    // 动态加载Axios
    (function() {
        var script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/axios@0.21.1/dist/axios.min.js';
        script.onload = function() {
            console.log('Axios加载成功');
            initAxiosConfig();
        };
        script.onerror = function() {
            console.error('Axios加载失败，请检查网络连接或手动引入axios库');
        };
        document.head.appendChild(script);
    })();
} else {
    // Axios已加载，直接初始化
    initAxiosConfig();
}

/**
 * 初始化Axios配置
 */
function initAxiosConfig() {
// 创建Axios实例
const axiosInstance = axios.create({
    baseURL: 'http://localhost:5004', // 只使用主机地址，避免与API路径重复
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
        
        // 检测是否是FormData类型，如果是，不要设置Content-Type
        if (config.data instanceof FormData) {
            // 删除Content-Type，让浏览器自动设置
            delete config.headers['Content-Type'];
            console.log('检测到FormData，自动移除Content-Type头');
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
 * 处理响应统一格式和错误处理
 */
axiosInstance.interceptors.response.use(
    response => {
        // 如果服务器返回的数据有code字段，检查错误码
        if (response.data && typeof response.data === 'object' && response.data.code !== undefined) {
            if (response.data.code !== 200) {
                // 服务器业务错误
                console.warn('服务器业务错误:', response.data);
                return Promise.resolve(response.data);
            }
        }
        
        // 正常响应
        return response.data;
    },
    error => {
        console.error('响应错误:', error);
        
        // 检查是否是认证错误
        if (error.response && error.response.status === 401) {
            console.warn('认证失效，需要重新登录');
            // 清除本地token
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            
            // 如果不是登录页面，跳转到登录页面
            if (window.location.pathname !== '/login.html' && window.location.pathname !== '/index.html') {
                window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.pathname);
            }
        }
        
        // 构造统一的错误返回
        const errorMessage = error.response && error.response.data && error.response.data.message
            ? error.response.data.message
            : '请求失败，请稍后重试';
            
        return Promise.reject(new Error(errorMessage));
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
 * HTTP接口封装
 */
const http = {
    // Axios实例
    defaults: axiosInstance.defaults,
    
    /**
     * GET请求
     * @param {string} url 请求URL
     * @param {Object} options 选项，包含params查询参数和其他config配置
     * @returns {Promise} 响应数据
     */
    get: (url, options = {}) => {
        // 如果options中有params属性，直接传递给axios
        if (options.params) {
            return axiosInstance.get(url, options);
        }
        // 否则将整个options作为params传递（向后兼容）
        return axiosInstance.get(url, { params: options });
    },
    
    /**
     * POST请求
     * @param {string} url 请求URL
     * @param {Object} data 请求体数据
     * @param {Object} config 其他配置选项
     * @returns {Promise} 响应数据
     */
    post: (url, data = {}, config = {}) => {
        return axiosInstance.post(url, data, config);
    },
    
    /**
     * PUT请求
     * @param {string} url 请求URL
     * @param {Object} data 请求体数据
     * @param {Object} config 其他配置选项
     * @returns {Promise} 响应数据
     */
    put: (url, data = {}, config = {}) => {
        return axiosInstance.put(url, data, config);
    },
    
    /**
     * DELETE请求
     * @param {string} url 请求URL
     * @param {Object} data 请求体数据
     * @param {Object} config 其他配置选项
     * @returns {Promise} 响应数据
     */
    delete: (url, data = {}, config = {}) => {
        return axiosInstance.delete(url, { 
            ...config,
            data: data  // 将数据放在config.data中，这是Axios DELETE请求的正确方式
        });
    }
};

// 导出为全局变量，方便在其他页面使用
window.http = http;
window.isLoggedIn = isLoggedIn; 
} 
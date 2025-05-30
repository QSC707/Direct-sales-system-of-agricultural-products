/**
 * 应用程序配置
 * 支持HTTP和HTTPS环境，优先使用HTTPS
 */
window.AppConfig = {
    // API基础URL配置 - 优先使用HTTPS
    API_BASE_URL: (() => {
        // 检测当前协议
        const protocol = window.location.protocol;
        const hostname = window.location.hostname;
        
        // 优先使用HTTPS协议
        if (protocol === 'https:') {
            return `https://${hostname}:5443`;
        }
        // HTTP环境也可以工作，但推荐HTTPS
        return `http://${hostname}:5004`;
    })(),
    
    // 开发环境配置
    DEVELOPMENT: {
        HTTP_PORT: 5004,
        HTTPS_PORT: 5443,
        API_TIMEOUT: 30000,
        PREFER_HTTPS: true
    },
    
    // 生产环境配置
    PRODUCTION: {
        API_TIMEOUT: 10000,
        ENABLE_HTTPS_REDIRECT: true,
        FORCE_HTTPS: true
    },
    
    // 获取当前环境
    getEnvironment() {
        return window.location.hostname === 'localhost' ? 'development' : 'production';
    },
    
    // 获取API URL
    getApiUrl(endpoint = '') {
        return `${this.API_BASE_URL}/api${endpoint}`;
    },
    
    // 获取完整的基础URL（包含协议和端口）
    getBaseUrl() {
        return this.API_BASE_URL;
    },
    
    // 检查是否为HTTPS
    isHttps() {
        return window.location.protocol === 'https:';
    },
    
    // 获取推荐的HTTPS URL
    getHttpsUrl() {
        const hostname = window.location.hostname;
        return `https://${hostname}:5443`;
    },
    
    // 检查是否应该使用HTTPS
    shouldUseHttps() {
        const env = this.getEnvironment();
        if (env === 'production') {
            return this.PRODUCTION.FORCE_HTTPS;
        }
        return this.DEVELOPMENT.PREFER_HTTPS;
    }
};

console.log('应用配置已加载:', {
    baseUrl: window.AppConfig.API_BASE_URL,
    environment: window.AppConfig.getEnvironment(),
    isHttps: window.AppConfig.isHttps(),
    shouldUseHttps: window.AppConfig.shouldUseHttps(),
    httpsUrl: window.AppConfig.getHttpsUrl()
}); 
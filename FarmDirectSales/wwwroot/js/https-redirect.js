/**
 * HTTPS自动重定向脚本
 * 确保生产环境强制使用HTTPS，开发环境优先使用HTTPS
 */
(function() {
    'use strict';
    
    // 检查是否需要重定向到HTTPS
    function checkHttpsRedirect() {
        const currentProtocol = window.location.protocol;
        const hostname = window.location.hostname;
        const currentPort = window.location.port;
        
        // 在开发环境中，优先使用HTTPS
        if (hostname === 'localhost' || hostname === '127.0.0.1') {
            // 开发环境：如果当前是HTTP且不是5004端口（API端口），推荐使用HTTPS
            if (currentProtocol === 'http:' && currentPort !== '5004') {
                const httpsUrl = `https://${hostname}:5443${window.location.pathname}${window.location.search}${window.location.hash}`;
                
                // 显示HTTPS升级提示（仅在开发环境）
                if (window.AppConfig && window.AppConfig.DEVELOPMENT.PREFER_HTTPS) {
                    console.log('推荐使用HTTPS访问:', httpsUrl);
                    
                    // 可选：自动重定向到HTTPS（取消注释下面的代码）
                    // setTimeout(() => {
                    //     if (confirm('推荐使用HTTPS访问以获得更好的安全性，是否切换？')) {
                    //         window.location.href = httpsUrl;
                    //     }
                    // }, 1000);
                }
            }
        } else {
            // 生产环境：强制HTTPS
            if (currentProtocol === 'http:') {
                const httpsUrl = `https://${hostname}${window.location.pathname}${window.location.search}${window.location.hash}`;
                console.log('生产环境强制重定向到HTTPS:', httpsUrl);
                window.location.href = httpsUrl;
            }
        }
    }
    
    // 设置安全Cookie属性
    function setSecureCookies() {
        if (window.location.protocol === 'https:') {
            // 在HTTPS环境下，确保所有cookie都设置secure属性
            const originalSetCookie = document.cookie.__lookupSetter__ && document.cookie.__lookupSetter__('cookie');
            if (!originalSetCookie) {
                // 如果浏览器不支持cookie setter，创建一个包装器
                const cookieDescriptor = Object.getOwnPropertyDescriptor(Document.prototype, 'cookie') ||
                                       Object.getOwnPropertyDescriptor(HTMLDocument.prototype, 'cookie');
                
                if (cookieDescriptor && cookieDescriptor.set) {
                    const originalSetter = cookieDescriptor.set;
                    
                    Object.defineProperty(document, 'cookie', {
                        set: function(value) {
                            // 如果是HTTPS环境且cookie字符串中没有secure属性，添加它
                            if (window.location.protocol === 'https:' && value && !value.includes('secure')) {
                                value += '; secure';
                            }
                            originalSetter.call(this, value);
                        },
                        get: cookieDescriptor.get
                    });
                }
            }
        }
    }
    
    // 增强LocalStorage和SessionStorage的安全性
    function enhanceStorageSecurity() {
        if (window.location.protocol === 'https:') {
            // 在HTTPS环境下，为敏感数据添加额外的安全标记
            const originalSetItem = localStorage.setItem;
            const sensitiveKeys = ['token', 'password', 'auth', 'session'];
            
            localStorage.setItem = function(key, value) {
                if (sensitiveKeys.some(sensitive => key.toLowerCase().includes(sensitive))) {
                    // 为敏感数据添加HTTPS标记
                    const secureData = {
                        data: value,
                        secure: true,
                        timestamp: Date.now(),
                        protocol: 'https'
                    };
                    originalSetItem.call(this, key, JSON.stringify(secureData));
                } else {
                    originalSetItem.call(this, key, value);
                }
            };
            
            const originalGetItem = localStorage.getItem;
            localStorage.getItem = function(key) {
                const value = originalGetItem.call(this, key);
                if (value && sensitiveKeys.some(sensitive => key.toLowerCase().includes(sensitive))) {
                    try {
                        const secureData = JSON.parse(value);
                        if (secureData && secureData.secure && secureData.protocol === 'https') {
                            return secureData.data;
                        }
                    } catch (e) {
                        // 如果解析失败，返回原始值（向后兼容）
                        return value;
                    }
                }
                return value;
            };
        }
    }
    
    // 设置Content Security Policy报告
    function setupCSPReporting() {
        // 监听CSP违规事件
        document.addEventListener('securitypolicyviolation', function(e) {
            console.warn('CSP违规报告:', {
                blockedURI: e.blockedURI,
                violatedDirective: e.violatedDirective,
                originalPolicy: e.originalPolicy,
                referrer: e.referrer,
                sourceFile: e.sourceFile,
                lineNumber: e.lineNumber
            });
            
            // 如果是混合内容问题，提供解决建议
            if (e.violatedDirective.includes('mixed-content')) {
                console.error('检测到混合内容问题，请确保所有资源都使用HTTPS');
            }
        });
    }
    
    // 检查并报告HTTPS状态
    function reportHttpsStatus() {
        const isHttps = window.location.protocol === 'https:';
        const hostname = window.location.hostname;
        const isLocal = hostname === 'localhost' || hostname === '127.0.0.1';
        
        console.log('HTTPS状态报告:', {
            protocol: window.location.protocol,
            isHttps: isHttps,
            isLocal: isLocal,
            port: window.location.port,
            secureContext: window.isSecureContext,
            timestamp: new Date().toISOString()
        });
        
        // 在开发环境提供HTTPS访问链接
        if (isLocal && !isHttps) {
            const httpsUrl = `https://${hostname}:5443${window.location.pathname}${window.location.search}`;
            console.log('📌 HTTPS访问链接:', httpsUrl);
        }
        
        // 检查是否支持现代Web API
        if (isHttps || window.isSecureContext) {
            console.log('✅ 安全上下文已启用，支持现代Web API');
        } else {
            console.warn('⚠️ 非安全上下文，部分现代Web API可能不可用');
        }
    }
    
    // 初始化HTTPS功能
    function initHttpsSecurity() {
        try {
            checkHttpsRedirect();
            setSecureCookies();
            enhanceStorageSecurity();
            setupCSPReporting();
            reportHttpsStatus();
            
            // 设置全局HTTPS状态
            window.HTTPS_STATUS = {
                isSecure: window.location.protocol === 'https:',
                isLocal: window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1',
                port: window.location.port,
                secureContext: window.isSecureContext
            };
            
        } catch (error) {
            console.error('HTTPS安全功能初始化失败:', error);
        }
    }
    
    // 页面加载时初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initHttpsSecurity);
    } else {
        initHttpsSecurity();
    }
    
    // 导出工具函数
    window.httpsUtils = {
        getHttpsUrl: function(path = '') {
            const hostname = window.location.hostname;
            const port = hostname === 'localhost' ? ':5443' : '';
            return `https://${hostname}${port}${path}`;
        },
        
        isSecureContext: function() {
            return window.isSecureContext || window.location.protocol === 'https:';
        },
        
        checkMixedContent: function() {
            const resources = document.querySelectorAll('img, script, link, iframe, embed, object');
            const mixedContent = [];
            
            resources.forEach(element => {
                let src = element.src || element.href;
                if (src && src.startsWith('http:') && window.location.protocol === 'https:') {
                    mixedContent.push({
                        element: element.tagName,
                        url: src
                    });
                }
            });
            
            return mixedContent;
        }
    };
    
})(); 
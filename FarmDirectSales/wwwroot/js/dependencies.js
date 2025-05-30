/**
 * 依赖管理脚本
 * 解决项目中缺少的依赖库问题
 */

// 创建一个全局变量来跟踪依赖加载状态
window.dependenciesLoaded = {
    jquery: false,
    bootstrap: false,
    fontawesome: false
};

// 创建一个依赖加载队列
window.dependencyCallbacks = [];

// 最大重试次数
const MAX_RETRY_COUNT = 3;
let jqueryRetryCount = 0;

/**
 * 异步加载jQuery并执行回调
 */
(function() {
    // 检查jQuery是否已存在
    if (typeof jQuery !== 'undefined') {
        console.log('jQuery 已存在');
        window.dependenciesLoaded.jquery = true;
        window.$ = jQuery; // 确保$ 全局变量设置
        loadBootstrap();
        executeCallbacks();
        return;
    }
    
    loadJQuery();
})();

/**
 * 加载jQuery的函数
 */
function loadJQuery() {
    const jqueryCDNs = [
        'https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js',
        'https://code.jquery.com/jquery-3.6.0.min.js',
        'https://ajax.googleapis.com/ajax/libs/jquery/3.6.0/jquery.min.js'
    ];
    
    function tryLoadJQuery(cdnIndex = 0) {
        if (cdnIndex >= jqueryCDNs.length) {
            console.error('所有jQuery CDN加载失败，尝试本地加载');
            tryLocalJQuery();
            return;
        }
        
        const script = document.createElement('script');
    script.type = 'text/javascript';
        script.src = jqueryCDNs[cdnIndex];
    script.async = false; // 保持加载顺序
        
    script.onload = function() {
            console.log(`jQuery 从CDN ${cdnIndex + 1} 加载成功: ${jqueryCDNs[cdnIndex]}`);
            window.dependenciesLoaded.jquery = true;
            window.$ = jQuery; // 确保$ 全局变量设置
            
            // 验证jQuery是否真正可用
            if (typeof $ === 'function' && typeof $.fn === 'object') {
                console.log('jQuery 验证成功，版本:', $.fn.jquery);
            loadBootstrap();
            executeCallbacks();
            } else {
                console.error('jQuery 加载后验证失败，尝试下一个CDN');
                tryLoadJQuery(cdnIndex + 1);
            }
        };
        
        script.onerror = function() {
            console.error(`jQuery CDN ${cdnIndex + 1} 加载失败: ${jqueryCDNs[cdnIndex]}`);
            tryLoadJQuery(cdnIndex + 1);
        };
        
        // 设置超时
        setTimeout(() => {
            if (!window.dependenciesLoaded.jquery) {
                console.error(`jQuery CDN ${cdnIndex + 1} 加载超时`);
                script.onerror();
            }
        }, 10000); // 10秒超时
        
        document.head.appendChild(script);
    }
    
    function tryLocalJQuery() {
        const localScript = document.createElement('script');
            localScript.type = 'text/javascript';
            localScript.src = '/lib/jquery/jquery.min.js';
            localScript.async = false;
        
            localScript.onload = function() {
                console.log('jQuery 从本地加载成功');
                window.dependenciesLoaded.jquery = true;
                window.$ = jQuery; // 确保$ 全局变量设置
                loadBootstrap();
                executeCallbacks();
            };
        
        localScript.onerror = function() {
            console.error('本地jQuery加载也失败，jQuery加载完全失败');
            // 即使jQuery加载失败，也要执行回调，让页面能够继续工作
            executeCallbacks();
        };
        
            document.head.appendChild(localScript);
    }
    
    tryLoadJQuery();
}

// 在页面加载时添加其他依赖
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM加载完成，检查依赖状态');
    
    // 如果jQuery还未加载，等待jQuery加载结束
    if (!window.dependenciesLoaded.jquery) {
        console.log('等待jQuery加载...');
        return;
    }
    
    // jQuery已加载，确保Bootstrap加载
    if (!window.dependenciesLoaded.bootstrap) {
        loadBootstrap();
    }
    
    // 加载Font Awesome
    if (!window.dependenciesLoaded.fontawesome) {
        loadStylesheet('https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0-beta3/css/all.min.css');
        window.dependenciesLoaded.fontawesome = true;
    }
});

/**
 * 初始化依赖
 * 在脚本中调用此函数以确保依赖已加载
 * @param {Function} callback - 所有依赖加载完成后的回调函数
 */
window.initDependencies = function(callback) {
    // 确保callback是函数
    if (typeof callback !== 'function') {
        console.error('initDependencies: callback必须是一个函数');
        return;
    }
    
        // 如果jQuery未加载，添加到回调队列
        if (!window.dependenciesLoaded.jquery) {
        console.log('jQuery未加载，添加回调到队列');
            window.dependencyCallbacks.push(callback);
            return;
        }
        
        // 如果bootstrap已加载则直接执行回调
        if (window.dependenciesLoaded.bootstrap) {
        console.log('所有依赖已加载，立即执行回调');
            try {
                callback();
            } catch (e) {
                console.error('执行回调时出错:', e);
            }
        } else {
            // 否则等待bootstrap加载完成
        console.log('等待Bootstrap加载完成');
            window.dependencyCallbacks.push(callback);
    }
};

/**
 * 执行所有回调
 */
function executeCallbacks() {
    console.log(`执行 ${window.dependencyCallbacks.length} 个回调函数`);
    
    if (window.dependencyCallbacks && window.dependencyCallbacks.length > 0) {
        // 复制回调队列
        var callbacks = window.dependencyCallbacks.slice();
        // 清空队列
        window.dependencyCallbacks = [];
        
        // 执行所有回调
        callbacks.forEach(function(callback, index) {
            try {
                console.log(`执行回调 ${index + 1}/${callbacks.length}`);
                callback();
            } catch (e) {
                console.error(`执行回调 ${index + 1} 时出错:`, e);
            }
        });
    }
}

/**
 * 加载Bootstrap库 - 更新到5.1.3版本
 */
function loadBootstrap() {
    console.log('开始加载Bootstrap');
    
    // 检查Bootstrap是否已经加载
    if (typeof bootstrap !== 'undefined') {
        console.log('Bootstrap已存在');
        window.dependenciesLoaded.bootstrap = true;
        executeCallbacks();
        return;
    }
    
    // Bootstrap 5不再依赖jQuery，可以独立加载
    loadStylesheet('https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css');
    
    // 尝试多个Bootstrap CDN
    const bootstrapCDNs = [
        'https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js',
        'https://cdnjs.cloudflare.com/ajax/libs/bootstrap/5.1.3/js/bootstrap.bundle.min.js',
        'https://unpkg.com/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js'
    ];
    
    function tryLoadBootstrap(cdnIndex = 0) {
        if (cdnIndex >= bootstrapCDNs.length) {
            console.error('所有Bootstrap CDN加载失败');
            // 即使Bootstrap加载失败，也要执行回调
            executeCallbacks();
            return;
        }
        
        loadScript(bootstrapCDNs[cdnIndex], function(error) {
            if (!error && typeof bootstrap !== 'undefined') {
        window.dependenciesLoaded.bootstrap = true;
                console.log(`Bootstrap 5.1.3 从CDN ${cdnIndex + 1} 加载成功`);
                
                // 验证Bootstrap功能
                try {
                    // 测试Bootstrap的Dropdown类是否可用
                    if (typeof bootstrap.Dropdown === 'function') {
                        console.log('Bootstrap Dropdown组件验证成功');
                    } else {
                        console.warn('Bootstrap Dropdown组件不可用');
                    }
                } catch (e) {
                    console.warn('Bootstrap组件验证时出错:', e);
                }
                
        executeCallbacks();
            } else {
                console.error(`Bootstrap CDN ${cdnIndex + 1} 加载失败:`, error);
                tryLoadBootstrap(cdnIndex + 1);
            }
    });
    }
    
    tryLoadBootstrap();
}

/**
 * 动态加载JavaScript文件
 * @param {string} url - 脚本URL
 * @param {Function} callback - 加载完成后的回调函数
 */
function loadScript(url, callback) {
    var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = url;
    script.async = false; // 确保按顺序加载
    
    var loaded = false;
    var timeoutId;
    
    function handleLoad() {
        if (loaded) return;
        loaded = true;
        
        if (timeoutId) {
            clearTimeout(timeoutId);
        }
        
        console.log(`脚本加载成功: ${url}`);
        if (callback) {
            setTimeout(() => callback(), 50); // 稍微延迟确保脚本完全执行
        }
    }
    
    function handleError(error) {
        if (loaded) return;
        loaded = true;
        
        if (timeoutId) {
            clearTimeout(timeoutId);
        }
        
        console.error(`加载脚本失败: ${url}`, error);
        if (callback) {
            callback(new Error(`加载脚本失败: ${url}`));
        }
    }
    
    script.onload = handleLoad;
    script.onreadystatechange = function() {
        if (this.readyState === 'loaded' || this.readyState === 'complete') {
            handleLoad();
        }
    };
    script.onerror = handleError;
    
    // 设置超时
    timeoutId = setTimeout(() => {
        if (!loaded) {
            console.error(`脚本加载超时: ${url}`);
            handleError(new Error(`脚本加载超时: ${url}`));
        }
    }, 15000); // 15秒超时
    
    document.head.appendChild(script);
}

/**
 * 动态加载CSS文件
 * @param {string} url - 样式表URL 
 */
function loadStylesheet(url) {
    // 检查是否已经加载过这个样式表
    const existingLink = document.querySelector(`link[href="${url}"]`);
    if (existingLink) {
        console.log(`样式表已存在: ${url}`);
        return;
    }
    
    var link = document.createElement('link');
    link.rel = 'stylesheet';
    link.type = 'text/css';
    link.href = url;
    
    link.onload = function() {
        console.log(`样式表加载成功: ${url}`);
    };
    
    link.onerror = function() {
        console.error(`样式表加载失败: ${url}`);
    };
    
    document.head.appendChild(link);
} 
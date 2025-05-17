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

/**
 * 异步加载jQuery并执行回调
 */
(function() {
    // 检查jQuery是否已存在
    if (typeof jQuery !== 'undefined') {
        console.log('jQuery 已存在');
        window.dependenciesLoaded.jquery = true;
        loadBootstrap();
        executeCallbacks();
        return;
    }
    
    // 异步方式加载jQuery
    var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = 'https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js';
    script.async = false; // 保持加载顺序
    script.onload = function() {
        console.log('jQuery 加载成功');
        window.dependenciesLoaded.jquery = true;
        window.$ = jQuery; // 确保$ 全局变量设置
        loadBootstrap();
        executeCallbacks();
    };
    script.onerror = function() {
        console.error('jQuery 加载失败');
        // 尝试使用备用CDN
        var backupScript = document.createElement('script');
        backupScript.type = 'text/javascript';
        backupScript.src = 'https://code.jquery.com/jquery-3.6.0.min.js';
        backupScript.async = false;
        backupScript.onload = function() {
            console.log('jQuery 从备用CDN加载成功');
            window.dependenciesLoaded.jquery = true;
            window.$ = jQuery; // 确保$ 全局变量设置
            loadBootstrap();
            executeCallbacks();
        };
        backupScript.onerror = function() {
            console.error('所有jQuery CDN加载失败');
            // 最后一次尝试从本地加载
            var localScript = document.createElement('script');
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
            document.head.appendChild(localScript);
        };
        document.head.appendChild(backupScript);
    };
    document.head.appendChild(script);
})();

// 在页面加载时添加其他依赖
document.addEventListener('DOMContentLoaded', function() {
    // 如果jQuery还未加载，等待jQuery加载结束
    if (!window.dependenciesLoaded.jquery) {
        return;
    }
    
    // jQuery已加载，确保Bootstrap加载
    if (!window.dependenciesLoaded.bootstrap) {
        loadBootstrap();
    }
    
    // 加载Font Awesome
    if (!window.dependenciesLoaded.fontawesome) {
        loadStylesheet('https://cdn.jsdelivr.net/npm/@fortawesome/fontawesome-free@5.15.4/css/all.min.css');
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
    if (typeof callback === 'function') {
        // 如果jQuery未加载，添加到回调队列
        if (!window.dependenciesLoaded.jquery) {
            window.dependencyCallbacks.push(callback);
            return;
        }
        
        // 如果bootstrap已加载则直接执行回调
        if (window.dependenciesLoaded.bootstrap) {
            try {
                callback();
            } catch (e) {
                console.error('执行回调时出错:', e);
            }
        } else {
            // 否则等待bootstrap加载完成
            window.dependencyCallbacks.push(callback);
        }
    }
};

/**
 * 执行所有回调
 */
function executeCallbacks() {
    if (window.dependencyCallbacks && window.dependencyCallbacks.length > 0) {
        // 复制回调队列
        var callbacks = window.dependencyCallbacks.slice();
        // 清空队列
        window.dependencyCallbacks = [];
        
        // 执行所有回调
        callbacks.forEach(function(callback) {
            try {
                callback();
            } catch (e) {
                console.error('执行回调时出错:', e);
            }
        });
    }
}

/**
 * 加载Bootstrap库
 */
function loadBootstrap() {
    loadStylesheet('https://cdn.jsdelivr.net/npm/bootstrap@4.6.0/dist/css/bootstrap.min.css');
    
    // 检查jQuery是否已加载，Bootstrap依赖jQuery
    if (!window.dependenciesLoaded.jquery) {
        console.warn('无法加载Bootstrap，jQuery尚未加载');
        return;
    }
    
    loadScript('https://cdn.jsdelivr.net/npm/bootstrap@4.6.0/dist/js/bootstrap.bundle.min.js', function() {
        window.dependenciesLoaded.bootstrap = true;
        console.log('Bootstrap 加载成功');
        executeCallbacks();
    });
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
    script.onload = function() {
        if (callback) callback();
    };
    script.onerror = function() {
        console.error(`加载脚本失败: ${url}`);
        if (callback) callback(new Error(`加载脚本失败: ${url}`));
    };
    document.head.appendChild(script);
}

/**
 * 动态加载CSS文件
 * @param {string} url - 样式表URL 
 */
function loadStylesheet(url) {
    var link = document.createElement('link');
    link.rel = 'stylesheet';
    link.type = 'text/css';
    link.href = url;
    document.head.appendChild(link);
} 
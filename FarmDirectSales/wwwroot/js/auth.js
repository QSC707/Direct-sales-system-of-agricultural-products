/**
 * 认证相关功能
 * 处理用户登录、注册、权限管理等
 */

/**
 * 检查用户登录状态
 * @param {Function} callback 回调函数，参数为用户信息或null
 */
function checkLogin(callback) {
    // 从localStorage获取token和用户信息
    const token = localStorage.getItem('token');
    
    // 从localStorage中获取用户角色
    const userStr = localStorage.getItem('user');
    let user = null;
    
    if (userStr) {
        try {
            user = JSON.parse(userStr);
        } catch (e) {
            console.error('解析用户数据失败:', e);
        }
    }
    
    if (token && user && user.userId) {
        // 尝试从API获取用户信息
        if (typeof api !== 'undefined' && api.checkLoginStatus()) {
            // 使用API中的用户信息
            updateNavbar(user);
            if (callback) callback(user);
            return;
        } else {
            // 回退到localStorage中的信息
            updateNavbar(user);
            if (callback) callback(user);
            return;
        }
    }
    
    // 兼容旧版：检查localStorage中的token和用户名
    const username = localStorage.getItem('username');
    const role = localStorage.getItem('role');
    
    if (token && username && role) {
        const user = {
            username: username,
            role: role
        };
        
        updateNavbar(user);
        if (callback) callback(user);
    } else {
        updateNavbar(null);
        if (callback) callback(null);
    }
}

/**
 * 更新导航栏状态
 * @param {Object} user 用户信息
 */
function updateNavbar(user) {
    if (typeof $ === 'undefined') {
        console.error('未加载jQuery，无法更新导航栏');
        return;
    }
    
    if (user) {
        // 已登录状态
        $('#currentUsername').text(user.username);
        $('#userDropdown').show();
        $('#loginItem, #registerItem').hide();
        
        // 获取当前页面路径，判断是在哪个管理界面
        const currentPath = window.location.pathname;
        
        // 删除之前可能存在的特定角色链接
        $('.admin-dashboard-link, .farmer-dashboard-link').remove();
        
        // 根据用户角色和当前所在页面类型处理导航栏
        if (user.role === 'admin') {
            // 如果是管理员，且不在admin页面，则增加跳转到管理中心的链接
            if (!currentPath.includes('/admin/')) {
                // 在普通页面，添加管理中心链接
                $('#userDropdown .dropdown-menu').prepend('<a class="dropdown-item admin-dashboard-link" href="/pages/admin/dashboard.html"><i class="fa fa-tachometer-alt mr-1"></i>管理中心</a>');
            }
        } else if (user.role === 'farmer') {
            // 如果是农户，且不在farmer页面，则增加跳转到农户中心的链接
            if (!currentPath.includes('/farmer/')) {
                // 在普通页面，添加农户中心链接
                $('#userDropdown .dropdown-menu').prepend('<a class="dropdown-item farmer-dashboard-link" href="/pages/farmer/dashboard.html"><i class="fa fa-tachometer-alt mr-1"></i>农户中心</a>');
            }
        }
    } else {
        // 未登录状态
        $('#userDropdown').hide();
        $('#loginItem, #registerItem').show();
        
        // 移除特定角色的链接
        $('.admin-dashboard-link, .farmer-dashboard-link').remove();
    }
}

/**
 * 用户登录
 * @param {string} username 用户名
 * @param {string} password 密码
 * @param {Function} callback 回调函数，参数为成功与否
 */
function login(username, password, callback) {
    // 检查API对象是否可用
    if (typeof api !== 'undefined' && api.auth && api.auth.login) {
        // 使用API模块进行登录
        api.auth.login(username, password)
            .then(function(response) {
                if (response.code === 200) {
                    // 读取存储的用户信息
                    const userDataStr = localStorage.getItem('user');
                    let userData = null;
                    
                    try {
                        userData = JSON.parse(userDataStr);
                    } catch (e) {
                        console.error('解析用户数据失败:', e);
                        if (callback) callback(false, '用户数据解析失败');
                        return;
                    }
                    
                    if (!userData) {
                        console.error('登录成功但未能获取用户数据');
                        if (callback) callback(false, '未能获取用户数据');
                        return;
                    }
                    
                    // 回调通知登录成功
                    if (callback) callback(true, userData);
                } else {
                    // 登录失败
                    if (callback) callback(false, response.message || '登录失败');
                }
            })
            .catch(function(error) {
                console.error('API登录错误:', error);
                if (callback) callback(false, error.message || '登录过程中发生错误');
            });
    } else {
        // API未加载时的模拟实现
        console.warn('API模块未加载，使用模拟登录');
        
        // 判断用户名的前缀来模拟不同角色
        let role = 'customer';
        if (username === 'admin') {
            role = 'admin';
        } else if (username.startsWith('farmer')) {
            role = 'farmer';
        }
        
        // 模拟成功登录
        const userData = {
            userId: Math.floor(Math.random() * 1000) + 1,
            username: username,
            role: role
        };
        
        // 存储登录信息
        localStorage.setItem('token', 'demo-token-' + new Date().getTime());
        localStorage.setItem('userId', userData.userId);
        localStorage.setItem('username', userData.username);
        localStorage.setItem('role', userData.role);
        localStorage.setItem('user', JSON.stringify(userData));
        
        if (callback) callback(true, userData);
    }
}

/**
 * 用户注册
 * @param {Object} userData 用户数据
 * @param {Function} callback 回调函数，参数为成功与否
 */
function register(userData, callback) {
    // 检查API对象是否可用
    if (typeof api !== 'undefined' && api.auth && api.auth.register) {
        // 使用API模块进行注册
        api.auth.register(userData)
            .then(function(response) {
                if (response.code === 200) {
                    if (callback) callback(true, response.data || {message: '注册成功'});
                } else {
                    if (callback) callback(false, response.message || '注册失败');
                }
            })
            .catch(function(error) {
                console.error('API注册错误:', error);
                if (callback) callback(false, error.message || '注册过程中发生错误');
            });
    } else {
        // API未加载时的模拟实现
        console.warn('API模块未加载，使用模拟注册');
        
        // 模拟注册成功
        setTimeout(function() {
            if (callback) callback(true, {message: '注册成功'});
        }, 500);
    }
}

/**
 * 检查用户是否具有特定角色权限
 * @param {string} requiredRole 需要的角色
 * @param {Function} callback 回调函数，参数为是否有权限
 */
function checkRole(requiredRole, callback) {
    checkLogin(function(user) {
        if (!user) {
            // 未登录
            if (callback) callback(false, null);
            return;
        }
        
        // 检查角色
        const hasPermission = (user.role === requiredRole);
        if (callback) callback(hasPermission, user);
    });
}

/**
 * 用户注销
 */
function logout() {
    // 获取当前角色，在跳转前保存
    const userStr = localStorage.getItem('user');
    let role = 'customer';
    
    try {
        if (userStr) {
            const userData = JSON.parse(userStr);
            role = userData.role || 'customer';
        }
    } catch (e) {
        console.error('解析用户数据失败:', e);
    }
    
    // 清除本地存储的认证信息
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
    localStorage.removeItem('username');
    localStorage.removeItem('role');
    localStorage.removeItem('user');
    
    // 记住上次的角色，便于用户再次登录时直接选择相同角色
    sessionStorage.setItem('last_role', role);
    
    // 重定向到登录页面，并传递提示参数和之前的角色
    window.location.href = '/pages/login.html?reason=logout&last_role=' + role;
}

// 将logout函数设置为全局函数，这样其他页面可以直接调用
window.logout = logout;

// 确保jQuery加载后初始化
if (typeof jQuery !== 'undefined') {
    // jQuery已加载
    $(document).ready(function() {
        // 绑定退出登录按钮点击事件
        $('#logoutBtn').on('click', function(e) {
            e.preventDefault();
            logout();
        });
        
        // 检查登录状态
        checkLogin();
    });
} else if (typeof window.initDependencies === 'function') {
    // 等待依赖加载完成
    window.initDependencies(function() {
        $(document).ready(function() {
            // 绑定退出登录按钮点击事件
            $('#logoutBtn').on('click', function(e) {
                e.preventDefault();
                logout();
            });
            
            // 检查登录状态
            checkLogin();
        });
    });
} 
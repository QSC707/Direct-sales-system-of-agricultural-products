/**
 * 登录状态修复脚本
 * 解决部分页面登录状态不正确显示的问题
 */

document.addEventListener('DOMContentLoaded', function() {
    // 检查并修复登录状态显示
    fixLoginStateDisplay();
    
    // 监听存储变化，当登录状态改变时更新界面
    window.addEventListener('storage', function(e) {
        if (e.key === 'token' || e.key === 'user') {
            fixLoginStateDisplay();
        }
    });
});

/**
 * 修复登录状态显示
 */
function fixLoginStateDisplay() {
    // 从localStorage获取token和用户信息
    const token = localStorage.getItem('token');
    const userStr = localStorage.getItem('user');
    
    // 获取需要操作的DOM元素
    const unauthenticatedMenu = document.getElementById('unauthenticated-menu');
    const authenticatedMenu = document.getElementById('authenticated-menu');
    
    if (!unauthenticatedMenu || !authenticatedMenu) {
        console.warn('未找到登录状态菜单元素');
        return;
    }
    
    if (token && userStr) {
        // 已登录状态
        try {
            const user = JSON.parse(userStr);
            
            // 更新用户名显示
            const usernameElement = document.getElementById('username');
            if (usernameElement) {
                usernameElement.textContent = user.username || '用户';
            }
            
            // 显示已登录菜单，隐藏未登录菜单
            unauthenticatedMenu.classList.add('d-none');
            authenticatedMenu.classList.remove('d-none');
            
            // 根据用户角色显示或隐藏特定菜单项
            const farmerMenuItem = document.getElementById('farmer-menu-item');
            const adminMenuItem = document.getElementById('admin-menu-item');
            
            if (farmerMenuItem && user.role === 'farmer') {
                farmerMenuItem.classList.remove('d-none');
            }
            
            if (adminMenuItem && user.role === 'admin') {
                adminMenuItem.classList.remove('d-none');
            }
        } catch (e) {
            console.error('解析用户数据失败:', e);
            // 出错时显示未登录状态
            unauthenticatedMenu.classList.remove('d-none');
            authenticatedMenu.classList.add('d-none');
        }
    } else {
        // 未登录状态
        unauthenticatedMenu.classList.remove('d-none');
        authenticatedMenu.classList.add('d-none');
    }
} 
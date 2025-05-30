/**
 * 导航栏样式统一脚本
 * 用于统一所有页面的导航栏样式和按钮
 */

document.addEventListener('DOMContentLoaded', function() {
    // 更新导航栏样式
    const navbar = document.querySelector('.navbar');
    if (navbar) {
        // 移除所有可能不一致的样式类
        navbar.classList.remove('navbar-light', 'bg-light', 'navbar-dark', 'bg-primary');
        
        // 添加统一的padding样式确保高度一致
        navbar.style.padding = '0.75rem 0';
        
        // 确保有统一的样式类
        if (!navbar.classList.contains('sticky-top')) {
            navbar.classList.add('sticky-top');
        }
        
        // 移除可能存在的导航栏过渡效果
        navbar.style.transition = 'none';
    }
    
    // 统一登录注册按钮样式
    const unauthenticatedMenu = document.getElementById('unauthenticated-menu');
    if (unauthenticatedMenu) {
        // 找到所有登录注册按钮
        const loginBtn = unauthenticatedMenu.querySelector('a[href="/pages/login.html"]');
        const registerBtn = unauthenticatedMenu.querySelector('a[href="/pages/register.html"]');
        
        // 先添加或确保间距类存在
        if (!unauthenticatedMenu.classList.contains('gap-2')) {
            unauthenticatedMenu.classList.add('d-flex', 'gap-2');
        }
        
        // 移除可能存在的margin类，统一使用gap
        unauthenticatedMenu.querySelectorAll('.me-2').forEach(element => {
            element.classList.remove('me-2');
        });
        
        if (loginBtn) {
            // 更新登录按钮样式
            loginBtn.className = 'btn btn-outline-primary';
            // 移除过渡效果
            loginBtn.style.transition = 'none';
        }
        
        if (registerBtn) {
            // 更新注册按钮样式
            registerBtn.className = 'btn btn-primary';
            // 移除过渡效果
            registerBtn.style.transition = 'none';
        }
    }
    
    // 统一已登录状态按钮样式
    const authenticatedMenu = document.getElementById('authenticated-menu');
    if (authenticatedMenu) {
        // 找到购物车按钮
        const cartBtn = authenticatedMenu.querySelector('a[href="/pages/cart.html"]');
        if (cartBtn) {
            cartBtn.className = 'btn btn-outline-primary me-2';
            cartBtn.style.transition = 'none';
        }
        
        // 找到用户菜单按钮
        const userMenuBtn = authenticatedMenu.querySelector('.dropdown-toggle');
        if (userMenuBtn) {
            userMenuBtn.className = 'btn btn-outline-primary dropdown-toggle';
            userMenuBtn.style.transition = 'none';
            
            // 移除可能存在的已登录标签
            const badge = userMenuBtn.querySelector('.badge');
            if (badge) {
                badge.remove();
            }
        }
        
        // 更新下拉菜单样式
        const dropdownMenu = authenticatedMenu.querySelector('.dropdown-menu');
        if (dropdownMenu) {
            dropdownMenu.className = 'dropdown-menu dropdown-menu-end shadow-lg border-0';
            dropdownMenu.style.transition = 'none';
        }
    }
    
    // 统一导航链接样式
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        // 移除过渡效果
        link.style.transition = 'none';
        link.style.fontWeight = '500';
        
        // 添加悬停效果监听，但不使用过渡
        link.addEventListener('mouseenter', function() {
            this.style.color = 'var(--primary-color)';
        });
        
        link.addEventListener('mouseleave', function() {
            if (!this.classList.contains('active')) {
                this.style.color = 'var(--text-color)';
            }
        });
        
        // 如果是活动链接，应用活动样式
        if (link.classList.contains('active')) {
            link.style.color = 'var(--primary-color)';
        }
    });
    
    // 为下拉菜单项添加图标颜色
    const dropdownIcons = document.querySelectorAll('.dropdown-item i');
    dropdownIcons.forEach(icon => {
        icon.style.color = 'var(--primary-color)';
    });
    
    // 统一导航栏品牌图标样式
    const brandIcon = navbar.querySelector('.navbar-brand i');
    if (brandIcon) {
        brandIcon.className = 'fas fa-leaf me-2';
        brandIcon.style.color = 'var(--primary-color)';
    }
    
    // 移除导航栏子元素的所有过渡效果
    const navbarElements = navbar.querySelectorAll('*');
    navbarElements.forEach(element => {
        element.style.transition = 'none';
    });
    
    // 移除下拉菜单的动画效果
    const allDropdowns = document.querySelectorAll('.dropdown-menu');
    allDropdowns.forEach(dropdown => {
        dropdown.style.transition = 'none';
        // 添加Bootstrap类来禁用动画
        dropdown.classList.add('animate-without-transition');
        
        // 移除下拉菜单项的过渡效果
        const dropdownItems = dropdown.querySelectorAll('.dropdown-item');
        dropdownItems.forEach(item => {
            item.style.transition = 'none';
        });
    });
    
    // 修复下拉菜单点击问题
    const dropdownToggles = document.querySelectorAll('.dropdown-toggle');
    dropdownToggles.forEach(toggle => {
        // 确保下拉菜单可以被点击
        toggle.setAttribute('data-bs-toggle', 'dropdown');
        toggle.setAttribute('aria-expanded', 'false');
        
        // 为已有的下拉菜单重新初始化Bootstrap功能
        if (typeof bootstrap !== 'undefined') {
            new bootstrap.Dropdown(toggle);
        }
        
        // 添加点击事件监听，确保下拉菜单能正确显示
        toggle.addEventListener('click', function(e) {
            // 仅当Bootstrap未正确处理时手动处理
            const dropdownMenu = this.nextElementSibling;
            if (dropdownMenu && dropdownMenu.classList.contains('dropdown-menu')) {
                if (!dropdownMenu.classList.contains('show')) {
                    // 关闭所有其他下拉菜单
                    document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                        if (menu !== dropdownMenu) {
                            menu.classList.remove('show');
                        }
                    });
                    
                    // 显示当前下拉菜单
                    dropdownMenu.classList.add('show');
                    this.setAttribute('aria-expanded', 'true');
                    
                    // 阻止冒泡以防止立即关闭
                    e.stopPropagation();
                }
            }
        });
    });
    
    // 添加点击其他区域关闭下拉菜单的功能
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.dropdown')) {
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                menu.classList.remove('show');
            });
            document.querySelectorAll('.dropdown-toggle[aria-expanded="true"]').forEach(toggle => {
                toggle.setAttribute('aria-expanded', 'false');
            });
        }
    });
}); 
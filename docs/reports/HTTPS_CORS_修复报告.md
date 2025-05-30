# HTTPS配置和CORS问题修复报告

## 🚨 问题描述

用户在使用HTTPS环境登录时遇到CORS跨域错误：

```
Access to fetch at 'http://localhost:5004/api/auth/login' from origin 'https://localhost:5443' has been blocked by CORS policy: Response to preflight request doesn't pass access control check: Redirect is not allowed for a preflight request.
```

## 🔍 问题分析

### 根本原因
1. **协议不匹配**: 前端页面运行在HTTPS (5443端口)，但API调用指向HTTP (5004端口)
2. **配置冲突**: `api.js`中硬编码了HTTP基础URL，与`config.js`中的动态配置冲突
3. **加载顺序问题**: 登录页面没有正确引入`config.js`，导致API配置错误

### 技术细节
- 浏览器的同源策略阻止了HTTPS页面向HTTP端点发起请求
- `api.js`第7行硬编码: `window.API_BASE_URL = 'http://localhost:5004/api'`
- `config.js`动态配置被忽略，导致协议不一致

## ✅ 修复方案

### 1. 修复API基础URL配置

**修改文件**: `FarmDirectSales/wwwroot/js/api.js`

**修改前**:
```javascript
// API基础URL - 使用全局window对象属性，避免重复声明
if (typeof window.API_BASE_URL === 'undefined') {
    window.API_BASE_URL = 'http://localhost:5004/api';
}
```

**修改后**:
```javascript
// 使用config.js中的动态API基础URL配置
if (typeof window.AppConfig === 'undefined') {
    console.error('AppConfig未加载，请确保config.js已正确引入');
    // 提供一个后备配置
    window.AppConfig = {
        getApiUrl: (endpoint = '') => `http://localhost:5004/api${endpoint}`
    };
}

// 设置API基础URL，使用config.js中的配置
window.API_BASE_URL = window.AppConfig.getApiUrl('/api');

console.log('API基础URL已设置为:', window.API_BASE_URL);
```

### 2. 修复登录页面脚本引入顺序

**修改文件**: `FarmDirectSales/wwwroot/pages/login.html`

**修改前**:
```html
<!-- 首先加载依赖管理器 -->
<script src="../js/dependencies.js"></script>
```

**修改后**:
```html
<!-- 首先加载应用配置 -->
<script src="../js/config.js"></script>
<!-- 然后加载API接口 -->
<script src="../js/api.js"></script>
<!-- 加载依赖管理器 -->
<script src="../js/dependencies.js"></script>
```

## 🔧 配置验证

### HTTPS端口配置
- **HTTP端口**: http://localhost:5004
- **HTTPS端口**: https://localhost:5443
- **Swagger文档**: https://localhost:5443/swagger

### 动态API配置逻辑
```javascript
API_BASE_URL: (() => {
    // 检测当前协议
    const protocol = window.location.protocol;
    const hostname = window.location.hostname;
    
    // 如果是HTTPS，使用HTTPS端口
    if (protocol === 'https:') {
        return `https://${hostname}:5443`;
    }
    // 否则使用HTTP端口
    return `http://${hostname}:5004`;
})()
```

## 🎯 修复效果

### ✅ 解决的问题
1. **CORS跨域错误**: 消除了协议不匹配导致的跨域问题
2. **API调用一致性**: 确保前端和API使用相同协议
3. **配置统一性**: 所有页面使用统一的动态配置

### ✅ 技术改进
1. **自动协议检测**: 根据当前页面协议自动选择API端点
2. **配置集中管理**: 通过`config.js`统一管理所有配置
3. **错误处理增强**: 添加了配置加载失败的后备机制

## 🚀 应用状态

### 当前运行状态
- ✅ HTTP端口 (5004): 正常监听
- ✅ HTTPS端口 (5443): 正常监听  
- ✅ SSL证书: 开发环境证书已信任
- ✅ CORS配置: 已正确配置跨域支持

### 测试建议
1. **HTTPS环境测试**: 访问 https://localhost:5443 进行登录测试
2. **HTTP环境测试**: 访问 http://localhost:5004 进行登录测试
3. **跨页面测试**: 验证所有页面的API调用是否正常

## 📋 后续维护

### 注意事项
1. 确保所有新页面都正确引入`config.js`
2. 避免在其他文件中硬编码API基础URL
3. 生产环境部署时需要更新`config.js`中的配置

### 监控要点
- API调用的协议一致性
- CORS错误的出现频率
- 不同环境下的配置正确性

---

**修复完成时间**: ${new Date().toLocaleString('zh-CN')}
**修复状态**: ✅ 已完成
**测试状态**: 🔄 待用户验证 
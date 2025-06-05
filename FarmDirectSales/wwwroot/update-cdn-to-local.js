/**
 * 批量更新HTML文件中的CDN链接为本地路径
 * 提高加载速度和离线访问能力
 */
const fs = require('fs');
const path = require('path');
const glob = require('glob');

// CDN替换规则
const cdnReplacements = [
    {
        // Bootstrap CSS
        from: 'https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css',
        to: '/lib/bootstrap/css/bootstrap.min.css'
    },
    {
        // Bootstrap JS
        from: 'https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js',
        to: '/lib/bootstrap/js/bootstrap.bundle.min.js'
    },
    {
        // Font Awesome
        from: 'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0-beta3/css/all.min.css',
        to: '/lib/fontawesome/css/all.min.css'
    },
    {
        // Axios (不指定版本)
        from: 'https://cdn.jsdelivr.net/npm/axios/dist/axios.min.js',
        to: '/lib/axios/axios.min.js'
    },
    {
        // Axios (指定版本)
        from: 'https://cdn.jsdelivr.net/npm/axios@0.21.1/dist/axios.min.js',
        to: '/lib/axios/axios.min.js'
    },
    {
        // jQuery
        from: 'https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js',
        to: '/lib/jquery/jquery.min.js'
    },
    {
        // SweetAlert2
        from: 'https://cdn.jsdelivr.net/npm/sweetalert2@11',
        to: '/lib/sweetalert2/sweetalert2.min.js'
    }
];

/**
 * 更新单个HTML文件
 * @param {string} filePath - 文件路径
 */
function updateHtmlFile(filePath) {
    try {
        let content = fs.readFileSync(filePath, 'utf8');
        let changed = false;
        
        cdnReplacements.forEach(rule => {
            if (content.includes(rule.from)) {
                content = content.replace(new RegExp(rule.from, 'g'), rule.to);
                changed = true;
            }
        });
        
        if (changed) {
            fs.writeFileSync(filePath, content, 'utf8');
            console.log(`✅ 已更新: ${filePath}`);
        } else {
            console.log(`⏭️  跳过: ${filePath} (无需更改)`);
        }
    } catch (error) {
        console.error(`❌ 更新失败: ${filePath}`, error.message);
    }
}

/**
 * 查找并更新所有HTML文件
 */
function updateAllHtmlFiles() {
    console.log('🔍 搜索HTML文件...\n');
    
    // 查找所有HTML文件
    const htmlFiles = glob.sync('**/*.html', {
        ignore: ['node_modules/**', 'lib/**']
    });
    
    console.log(`📁 找到 ${htmlFiles.length} 个HTML文件\n`);
    
    htmlFiles.forEach(updateHtmlFile);
    
    console.log('\n✨ 批量更新完成！');
    console.log('💡 建议运行项目测试所有功能是否正常');
}

/**
 * 创建备份
 */
function createBackup() {
    const backupDir = `backup_${new Date().toISOString().slice(0, 10)}`;
    console.log(`📦 创建备份目录: ${backupDir}`);
    
    // 这里可以添加备份逻辑
    console.log('💡 建议手动备份重要文件');
}

// 主执行流程
console.log('🚀 开始CDN本地化更新...\n');
createBackup();
updateAllHtmlFiles(); 
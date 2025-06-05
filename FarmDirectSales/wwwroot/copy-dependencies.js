/**
 * 复制node_modules中的依赖文件到本地lib目录
 * 用于离线部署和提高加载速度
 */
const fs = require('fs-extra');
const path = require('path');

// 定义需要复制的文件映射
const filesToCopy = [
    {
        source: 'node_modules/bootstrap/dist/css/bootstrap.min.css',
        dest: 'lib/bootstrap/css/bootstrap.min.css'
    },
    {
        source: 'node_modules/bootstrap/dist/js/bootstrap.bundle.min.js',
        dest: 'lib/bootstrap/js/bootstrap.bundle.min.js'
    },
    {
        source: 'node_modules/@fortawesome/fontawesome-free/css/all.min.css',
        dest: 'lib/fontawesome/css/all.min.css'
    },
    {
        source: 'node_modules/@fortawesome/fontawesome-free/webfonts',
        dest: 'lib/fontawesome/webfonts'
    },
    {
        source: 'node_modules/axios/dist/axios.min.js',
        dest: 'lib/axios/axios.min.js'
    },
    {
        source: 'node_modules/jquery/dist/jquery.min.js',
        dest: 'lib/jquery/jquery.min.js'
    },
    {
        source: 'node_modules/sweetalert2/dist/sweetalert2.min.js',
        dest: 'lib/sweetalert2/sweetalert2.min.js'
    },
    {
        source: 'node_modules/sweetalert2/dist/sweetalert2.min.css',
        dest: 'lib/sweetalert2/sweetalert2.min.css'
    }
];

/**
 * 复制单个文件或目录
 * @param {string} source - 源路径
 * @param {string} dest - 目标路径
 */
async function copyFile(source, dest) {
    try {
        await fs.ensureDir(path.dirname(dest));
        await fs.copy(source, dest);
        console.log(`✅ 已复制: ${source} -> ${dest}`);
    } catch (error) {
        console.error(`❌ 复制失败: ${source}`, error.message);
    }
}

/**
 * 主执行函数
 */
async function main() {
    console.log('🚀 开始复制依赖文件...\n');
    
    for (const file of filesToCopy) {
        await copyFile(file.source, file.dest);
    }
    
    console.log('\n✨ 依赖文件复制完成！');
    console.log('💡 现在可以更新HTML文件中的CDN链接为本地路径了');
}

// 执行脚本
main().catch(console.error); 
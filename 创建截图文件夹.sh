#!/bin/bash

# 农产品直销系统截图文件夹创建脚本
# 使用中文命名规范

echo "🚀 开始创建截图文件夹结构..."

# 创建主截图目录
mkdir -p screenshots

# 创建各个功能模块的文件夹
mkdir -p screenshots/01_系统概览
mkdir -p screenshots/02_消费者功能
mkdir -p screenshots/03_农户功能
mkdir -p screenshots/04_管理员功能
mkdir -p screenshots/05_界面细节
mkdir -p screenshots/06_响应式设计

echo "📁 文件夹结构创建完成！"
echo ""
echo "📂 创建的文件夹结构："
echo "screenshots/"
echo "├── 01_系统概览/              # 系统整体大图"
echo "├── 02_消费者功能/            # 消费者端功能截图"
echo "├── 03_农户功能/              # 农户端功能截图"
echo "├── 04_管理员功能/            # 管理员端功能截图"
echo "├── 05_界面细节/              # 交互细节截图"
echo "└── 06_响应式设计/            # 响应式设计截图"
echo ""
echo "✅ 现在可以开始按照清单进行截图拍摄了！"
echo ""
echo "📋 拍摄清单文件："
echo "- 截图拍摄清单_中文版.md (详细清单)"
echo "- 项目截图拍摄计划.md (完整规划)"
echo ""
echo "🎯 重点截图提醒："
echo "- 农户_02_添加产品弹窗.png (简约弹窗设计)"
echo "- 农户_08_批量操作弹窗.png (批量操作功能)"
echo "- 消费者_05_分页组件.png (分页功能)"
echo "- 响应式设计系列 (多设备适配)" 
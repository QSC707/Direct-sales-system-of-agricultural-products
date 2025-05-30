#!/bin/bash

# 导航栏样式统一脚本

# 定义要处理的HTML文件目录
HTML_DIR="FarmDirectSales/wwwroot"

# 检查脚本文件是否存在
if [ ! -f "${HTML_DIR}/pages/update-navbar.js" ]; then
    echo "错误: 导航栏统一脚本文件不存在!"
    exit 1
fi

# 查找所有HTML文件
echo "正在查找HTML文件..."
HTML_FILES=$(find "${HTML_DIR}" -name "*.html")
TOTAL_FILES=$(echo "${HTML_FILES}" | wc -l)
echo "找到 ${TOTAL_FILES} 个HTML文件。"

# 计数器
UPDATED=0

# 为每个HTML文件添加导航栏统一脚本
for FILE in ${HTML_FILES}; do
    # 检查文件是否已包含统一脚本
    if grep -q "update-navbar.js" "${FILE}"; then
        echo "跳过 ${FILE} (已包含脚本)"
        continue
    fi
    
    # 检查文件是否包含导航栏
    if grep -q "<nav class=\"navbar" "${FILE}"; then
        # 在</body>标签前添加脚本引用
        sed -i '' 's|</body>|    <script src="/pages/update-navbar.js"></script>\n</body>|' "${FILE}"
        echo "已更新 ${FILE}"
        UPDATED=$((UPDATED + 1))
    else
        echo "跳过 ${FILE} (未发现导航栏)"
    fi
done

echo "完成! 已更新 ${UPDATED} 个文件。"
echo "导航栏样式已统一。" 
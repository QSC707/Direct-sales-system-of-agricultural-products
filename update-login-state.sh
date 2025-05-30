#!/bin/bash

# 登录状态修复脚本添加工具

# 需要修复的HTML文件列表
HTML_FILES=(
    "FarmDirectSales/wwwroot/pages/faq.html"
    "FarmDirectSales/wwwroot/pages/shipping.html"
    "FarmDirectSales/wwwroot/pages/return.html"
    "FarmDirectSales/wwwroot/pages/contact.html"
)

# 计数器
UPDATED=0

echo "开始更新登录状态修复脚本..."

# 遍历每个文件并添加修复脚本
for FILE in "${HTML_FILES[@]}"; do
    # 检查文件是否存在
    if [ ! -f "$FILE" ]; then
        echo "错误: 文件 $FILE 不存在!"
        continue
    fi
    
    # 检查文件是否已包含修复脚本
    if grep -q "fix-login-state.js" "$FILE"; then
        echo "跳过 $FILE (已包含修复脚本)"
        continue
    fi
    
    # 检查文件是否包含update-navbar.js引用，在其前面添加修复脚本
    if grep -q "update-navbar.js" "$FILE"; then
        sed -i '' 's|<script src="/pages/update-navbar.js"></script>|<script src="/js/fix-login-state.js"></script>\n    <script src="/pages/update-navbar.js"></script>|' "$FILE"
        echo "已更新 $FILE"
        UPDATED=$((UPDATED + 1))
    else
        # 如果没有找到update-navbar.js，则在body结束标签前添加
        sed -i '' 's|</body>|    <script src="/js/fix-login-state.js"></script>\n</body>|' "$FILE"
        echo "已更新 $FILE"
        UPDATED=$((UPDATED + 1))
    fi
done

echo "完成! 已更新 $UPDATED 个文件。"

# 将登录状态修复脚本复制到正确位置
mkdir -p FarmDirectSales/wwwroot/js
cp -f FarmDirectSales/wwwroot/js/fix-login-state.js FarmDirectSales/wwwroot/js/

echo "登录状态修复脚本已部署，重新加载页面后生效。" 
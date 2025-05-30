#!/bin/bash

# 脚本：优化页脚HTML结构
# 作用：移除页脚中的冗余元素，进一步减少高度

# 颜色定义
GREEN="\033[0;32m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
RESET="\033[0m"

echo -e "${GREEN}开始优化页脚HTML结构...${RESET}"

# 查找所有HTML文件
HTML_FILES=$(find FarmDirectSales/wwwroot -type f -name "*.html")

# 计数器
TOTAL=0
UPDATED=0

# 处理每个HTML文件
for FILE in $HTML_FILES; do
    TOTAL=$((TOTAL + 1))
    
    # 检查文件中是否包含页脚
    if grep -q '<footer class="footer-section">' "$FILE"; then
        echo -e "${YELLOW}处理文件: ${FILE}${RESET}"
        
        # 1. 移除社交链接中的不必要的mb-4类
        sed -i '' 's/<div class="social-links mb-4">/<div class="social-links">/' "$FILE"
        
        # 2. 移除页脚品牌描述中的多余mb-4
        sed -i '' 's/<p class="text-muted mb-4">/<p class="text-muted">/' "$FILE"
        
        # 3. 移除淡入动画类(fade-in)，因为它会占用额外的渲染时间
        perl -i -pe 's/class="col-lg-(\d+) col-md-(\d+) fade-in"/class="col-lg-$1 col-md-$2"/g' "$FILE"
        perl -i -pe 's/class="col-lg-(\d+) col-md-(\d+) col-(\d+) fade-in"/class="col-lg-$1 col-md-$2 col-$3"/g' "$FILE"
        
        # 4. 移除列间距的g-4，改为更小的g-3
        sed -i '' 's/class="row g-4"/class="row g-3"/' "$FILE"
        
        echo -e "${GREEN}成功优化页脚 - ${FILE}${RESET}"
        UPDATED=$((UPDATED + 1))
    else
        echo -e "${RED}跳过文件: ${FILE} (未找到页脚)${RESET}"
    fi
done

echo -e "${GREEN}完成！${RESET}"
echo -e "总共处理 ${TOTAL} 个文件，更新了 ${UPDATED} 个文件。" 
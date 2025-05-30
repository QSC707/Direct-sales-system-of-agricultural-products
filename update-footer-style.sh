#!/bin/bash

# 脚本：更新页脚样式
# 作用：为所有HTML页面添加紧凑型页脚样式

# 颜色定义
GREEN="\033[0;32m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
RESET="\033[0m"

echo -e "${GREEN}开始更新页脚样式...${RESET}"

# 确保CSS文件目录存在
STYLE_DIR="FarmDirectSales/wwwroot/css"
if [ ! -d "$STYLE_DIR" ]; then
    echo -e "${RED}错误: CSS目录不存在!${RESET}"
    exit 1
fi

# 确保紧凑型页脚样式文件存在
FOOTER_CSS="$STYLE_DIR/footer-compact.css"
if [ ! -f "$FOOTER_CSS" ]; then
    echo -e "${RED}错误: 紧凑型页脚样式文件不存在!${RESET}"
    exit 1
fi

# 查找所有HTML文件
HTML_FILES=$(find FarmDirectSales/wwwroot -type f -name "*.html")

# 计数器
TOTAL=0
UPDATED=0

# 处理每个HTML文件
for FILE in $HTML_FILES; do
    TOTAL=$((TOTAL + 1))
    
    # 检查文件中是否已包含紧凑型页脚样式引用
    if grep -q "footer-compact.css" "$FILE"; then
        echo -e "${YELLOW}跳过文件: ${FILE} (已包含紧凑型页脚样式)${RESET}"
        continue
    fi
    
    # 检查文件是否包含样式表引用区域
    if grep -q "<link.*style.css" "$FILE"; then
        echo -e "${YELLOW}处理文件: ${FILE}${RESET}"
        
        # 在样式表后添加紧凑型页脚样式引用
        sed -i '' '/<link.*style.css/a \
    <link href="/css/footer-compact.css" rel="stylesheet">
' "$FILE"
        
        # 验证更新
        if grep -q "footer-compact.css" "$FILE"; then
            echo -e "${GREEN}成功添加紧凑型页脚样式 - ${FILE}${RESET}"
            UPDATED=$((UPDATED + 1))
        else
            echo -e "${RED}更新失败 - ${FILE}${RESET}"
        fi
    else
        echo -e "${RED}跳过文件: ${FILE} (未找到样式表引用区域)${RESET}"
    fi
done

echo -e "${GREEN}完成！${RESET}"
echo -e "总共处理 ${TOTAL} 个文件，更新了 ${UPDATED} 个文件。" 
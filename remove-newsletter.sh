#!/bin/bash

# 脚本：移除所有页面中的订阅新闻模块
# 作用：遍历所有HTML文件，移除"订阅我们的新闻"模块

# 颜色定义
GREEN="\033[0;32m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
RESET="\033[0m"

echo -e "${GREEN}开始移除订阅新闻模块...${RESET}"

# 查找HTML文件
HTML_FILES=$(find FarmDirectSales/wwwroot -type f -name "*.html")

# 计数器
TOTAL=0
UPDATED=0

# 处理每个HTML文件
for FILE in $HTML_FILES; do
    TOTAL=$((TOTAL + 1))
    
    # 检查文件中是否包含newsletter模块
    if grep -q "订阅我们的新闻" "$FILE"; then
        echo -e "${YELLOW}处理文件: ${FILE}${RESET}"
        
        # 使用sed删除newsletter div模块
        # 使用多行删除模式匹配从<div class="newsletter开始到</div>结束的整个模块
        sed -i '' '/<div class="newsletter/,/<\/div>/{
            /<div class="newsletter/!{
                /<\/div>/!d
            }
            /<div class="newsletter/{
                d
            }
            /<\/div>/{
                d
            }
        }' "$FILE"
        
        # 验证修改
        if ! grep -q "订阅我们的新闻" "$FILE"; then
            echo -e "${GREEN}成功移除新闻订阅模块 - ${FILE}${RESET}"
            UPDATED=$((UPDATED + 1))
        else
            echo -e "${RED}移除失败 - ${FILE}${RESET}"
        fi
    fi
done

echo -e "${GREEN}完成！${RESET}"
echo -e "总共处理 ${TOTAL} 个文件，更新了 ${UPDATED} 个文件。" 
#!/bin/bash

# 更新页脚的脚本
# 新页脚模板已保存在 FarmDirectSales/wwwroot/footer-template.html

# 定义新页脚开始和结束标记
FOOTER_START="    <!-- 页脚 -->\n    <!-- 使用新设计的现代简洁页脚 -->"
FOOTER_END="    </footer>"

# 读取新页脚内容
NEW_FOOTER=$(cat FarmDirectSales/wwwroot/footer-template.html)

# 要更新的页面列表
PAGES=(
  "FarmDirectSales/wwwroot/pages/register.html"
  "FarmDirectSales/wwwroot/pages/login.html"
  "FarmDirectSales/wwwroot/pages/contact.html"
  "FarmDirectSales/wwwroot/pages/about.html"
  "FarmDirectSales/wwwroot/pages/faq.html"
  "FarmDirectSales/wwwroot/pages/shipping.html"
  "FarmDirectSales/wwwroot/pages/return.html"
  "FarmDirectSales/wwwroot/pages/cart.html"
  "FarmDirectSales/wwwroot/pages/products.html"
  "FarmDirectSales/wwwroot/pages/farmers.html"
  "FarmDirectSales/wwwroot/index.html"
)

# 对每个页面进行处理
for page in "${PAGES[@]}"; do
  # 检查文件是否存在
  if [ -f "$page" ]; then
    echo "正在更新页脚: $page"
    
    # 创建备份
    cp "$page" "${page}.bak"
    
    # 使用awk提取页脚之前和之后的内容
    awk 'BEGIN {printing=1} 
      /<!-- 页脚 -->/ {printing=0} 
      {if(printing) print} 
      /<\/footer>/ {printing=1; next}' "$page" > "${page}.temp1"
    
    # 使用awk提取页脚之后的内容
    awk 'BEGIN {printing=0} 
      /<\/footer>/ {printing=1; next} 
      {if(printing) print}' "$page" > "${page}.temp2"
    
    # 合并文件：页脚前 + 新页脚 + 页脚后
    cat "${page}.temp1" > "${page}.new"
    echo -e "$FOOTER_START" >> "${page}.new"
    echo "$NEW_FOOTER" | sed '1d;$d' >> "${page}.new"  # 删除第一行和最后一行（HTML注释和footer标签）
    echo -e "$FOOTER_END" >> "${page}.new"
    cat "${page}.temp2" >> "${page}.new"
    
    # 替换原文件
    mv "${page}.new" "$page"
    
    # 清理临时文件
    rm "${page}.temp1" "${page}.temp2"
    
    echo "✅ 成功更新页脚: $page"
  else
    echo "❌ 文件不存在: $page"
  fi
done

echo "所有页脚更新完成！" 
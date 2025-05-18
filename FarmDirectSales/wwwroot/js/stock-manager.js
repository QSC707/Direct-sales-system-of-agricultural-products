/**
 * 库存管理工具
 * @description 处理商品库存的验证、更新和提醒功能
 */

const StockManager = {
    /**
     * 检查库存是否充足
     * @param {number} productId - 商品ID 
     * @param {number} quantity - 需要的数量
     * @returns {Promise<boolean>} 返回是否库存充足
     */
    async checkStockAvailability(productId, quantity) {
        try {
            // 获取最新的商品信息
            const response = await http.get(`/api/product/${productId}`);
            const product = response.data;
            
            if (!product) {
                console.error('商品不存在:', productId);
                return false;
            }
            
            // 检查库存是否足够
            return product.stock >= quantity;
        } catch (error) {
            console.error('检查库存失败:', error);
            return false;
        }
    },
    
    /**
     * 获取商品的当前库存
     * @param {number} productId - 商品ID
     * @returns {Promise<number>} 返回当前库存数量，如果出错则返回0
     */
    async getCurrentStock(productId) {
        try {
            const response = await http.get(`/api/product/${productId}`);
            const product = response.data;
            
            if (!product) {
                console.error('商品不存在:', productId);
                return 0;
            }
            
            return product.stock;
        } catch (error) {
            console.error('获取库存失败:', error);
            return 0;
        }
    },
    
    /**
     * 根据库存量更新UI显示状态
     * @param {number} productId - 商品ID
     * @param {HTMLElement} stockElement - 显示库存的DOM元素
     * @param {HTMLElement} buttonElement - 添加到购物车的按钮元素
     */
    async updateStockUI(productId, stockElement, buttonElement) {
        try {
            const stock = await this.getCurrentStock(productId);
            
            // 更新库存显示
            if (stockElement) {
                stockElement.textContent = `库存: ${stock}`;
                
                // 根据库存状态添加不同的样式
                stockElement.className = 'badge ms-2';
                if (stock > 10) {
                    stockElement.classList.add('bg-success');
                } else if (stock > 0) {
                    stockElement.classList.add('bg-warning', 'text-dark');
                } else {
                    stockElement.classList.add('bg-danger');
                    stockElement.textContent = '已售罄';
                }
            }
            
            // 更新按钮状态
            if (buttonElement) {
                if (stock <= 0) {
                    buttonElement.disabled = true;
                    buttonElement.innerHTML = '<i class="fas fa-ban me-2"></i>已售罄';
                    buttonElement.classList.remove('btn-success');
                    buttonElement.classList.add('btn-secondary');
                } else {
                    buttonElement.disabled = false;
                    buttonElement.innerHTML = '<i class="fas fa-shopping-cart me-2"></i>加入购物车';
                    buttonElement.classList.remove('btn-secondary');
                    buttonElement.classList.add('btn-success');
                }
            }
            
            return stock;
        } catch (error) {
            console.error('更新库存UI失败:', error);
            return 0;
        }
    },
    
    /**
     * 检查购物车中所有商品的库存状态
     * @returns {Promise<{valid: boolean, invalidItems: Array}>} 返回检查结果
     */
    async validateCartStock() {
        try {
            // 获取购物车数据
            const cartResponse = await http.get('/api/cart');
            const cartItems = cartResponse.data.items || [];
            
            if (cartItems.length === 0) {
                return { valid: true, invalidItems: [] };
            }
            
            const invalidItems = [];
            
            // 检查每个购物车项的库存
            for (const item of cartItems) {
                const stockAvailable = await this.getCurrentStock(item.product.productId);
                
                if (stockAvailable < item.quantity) {
                    invalidItems.push({
                        cartItemId: item.cartItemId,
                        productId: item.product.productId,
                        productName: item.product.productName,
                        requestedQuantity: item.quantity,
                        availableStock: stockAvailable
                    });
                }
            }
            
            return {
                valid: invalidItems.length === 0,
                invalidItems
            };
        } catch (error) {
            console.error('验证购物车库存失败:', error);
            return { valid: false, invalidItems: [] };
        }
    },
    
    /**
     * 显示库存不足警告
     * @param {Array} invalidItems - 库存不足的商品列表
     */
    showStockWarnings(invalidItems) {
        if (!invalidItems || invalidItems.length === 0) {
            return;
        }
        
        let warningMessage = '以下商品库存不足，请调整数量：\n';
        
        invalidItems.forEach(item => {
            warningMessage += `- ${item.productName}: 当前库存${item.availableStock}，您需要${item.requestedQuantity}\n`;
        });
        
        alert(warningMessage);
    }
};

// 导出为全局变量
window.StockManager = StockManager; 
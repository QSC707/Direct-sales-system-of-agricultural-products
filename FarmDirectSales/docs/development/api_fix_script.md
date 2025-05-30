# API路径修复清单

## 需要修复的文件和API调用

### 1. FarmDirectSales/wwwroot/pages/user/orders.html
- 第1453行: `/order/${orderId}` → `/api/order/${orderId}`
- 第1560行: `/order/${orderId}/complete` → `/api/order/${orderId}/complete`
- 第1653行: `/order/${orderId}/cancel` → `/api/order/${orderId}/cancel`
- 第2149行: `/Order/${orderId}` → `/api/order/${orderId}` (注意大小写)
- 第2284行: `/order/${orderId}/refund-request` → `/api/order/${orderId}/refund-request`

### 2. FarmDirectSales/wwwroot/pages/admin/delivery-areas.html
- 第1055行: `/delivery-area/${areaId}` → `/api/delivery-area/${areaId}`
- 第1076行: `/shipping-fee/${shippingFeeId}` → `/api/shipping-fee/${shippingFeeId}`
- 第1181行: `/delivery-area/${areaId}` → `/api/delivery-area/${areaId}`
- 第1190行: `/shipping-fee/${shippingFeeId}` → `/api/shipping-fee/${shippingFeeId}`

### 3. FarmDirectSales/wwwroot/pages/admin/users.html
- 第1037行: `/logs/user/${userId}` → `/api/log/user/${userId}` (注意LogController使用log不是logs)

### 4. FarmDirectSales/wwwroot/pages/admin/orders.html
- 第834行: `/orders/${orderId}` → `/api/order/${orderId}` (注意OrderController使用order不是orders)
- 第948行: `/orders/${orderId}/ship` → `/api/order/${orderId}/ship`
- 第977行: `/orders/${orderId}/cancel` → `/api/order/${orderId}/cancel`

### 5. FarmDirectSales/wwwroot/pages/admin/products.html
- 第743行: `/products/${productId}` → `/api/product/${productId}` (注意ProductController使用product不是products)
- 第793行: `/products/${productId}` → `/api/product/${productId}`
- 第820行: `/products/${productId}/status` → `/api/product/${productId}/status`

### 6. FarmDirectSales/wwwroot/pages/farmer/products.html
- 第1121行: `/product?${params.toString()}` → `/api/product?${params.toString()}`
- 第1223行: `/product/${productId}` → `/api/product/${productId}`
- 第1240行: `/product/${productId}/sales` → `/api/product/${productId}/sales`
- 第1421行: `/product/${productId}` → `/api/product/${productId}`
- 第1518行: `/product/${productId}` → `/api/product/${productId}`
- 第1864行: `/product/${productId}` → `/api/product/${productId}`
- 第1992行: `/product/${productId}` → `/api/product/${productId}`
- 第2139行: `/product/${productId}` → `/api/product/${productId}`
- 第2352行: `/product/${productId}` → `/api/product/${productId}`

### 7. FarmDirectSales/wwwroot/pages/admin/logs_new.html
- 第460行: `/users` → `/api/user` (注意UserController使用user不是users)
- 第590行: `/users/${userId}` → `/api/user/${userId}`
- 第631行: `/logs/user/${userId}` → `/api/log/user/${userId}`
- 第674行: `/users/${userId}` → `/api/user/${userId}`
- 第713行: `/users/${userId}` → `/api/user/${userId}`
- 第847行: `/users/${userId}` → `/api/user/${userId}`

## 控制器路由映射
- OrderController: `/api/order`
- ProductController: `/api/product`
- UserController: `/api/user`
- LogController: `/api/log`
- DeliveryAreaController: `/api/delivery-area`
- ShippingFeeController: `/api/shipping-fee`
- StatisticsController: `/statistics` (不使用api前缀) 
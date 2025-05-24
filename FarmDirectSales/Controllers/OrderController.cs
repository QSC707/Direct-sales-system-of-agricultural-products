using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FarmDirectSales.Data;
using FarmDirectSales.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 订单管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 创建订单
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                // 检查用户是否存在
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                // 检查产品是否存在
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == request.ProductId && p.IsActive);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 检查库存是否充足
                if (product.Stock < request.Quantity)
                {
                    return BadRequest(new { code = 400, message = "库存不足" });
                }

                // 计算总价
                decimal totalPrice = product.Price * request.Quantity;

                // 创建订单
                var order = new Order
                {
                    UserId = request.UserId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    TotalPrice = totalPrice,
                    Status = "货到付款待处理",
                    CreateTime = DateTime.Now,
                    PayTime = null,
                    ShipTime = null,
                    CompleteTime = null,
                    ShippingAddress = request.ShippingAddress,
                    ContactPhone = request.ContactPhone
                };

                // 减少库存
                product.Stock -= request.Quantity;
                product.UpdateTime = DateTime.Now;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "创建订单成功",
                    data = new
                    {
                        order.OrderId,
                        order.UserId,
                        order.ProductId,
                        order.Quantity,
                        order.TotalPrice,
                        order.Status,
                        order.CreateTime,
                        order.ShippingAddress,
                        order.ContactPhone
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取用户订单列表
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUserOrders(int userId)
        {
            try
            {
                // 验证当前用户是否有权限查看此用户的订单
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || (currentUser.UserId != userId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限查看此用户的订单" });
                }

                // 检查用户是否存在
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                var orders = await _context.Orders
                    .Include(o => o.Product)
                    .ThenInclude(p => p.Farmer)
                    .Where(o => o.UserId == userId && !o.IsDeleted) // 排除软删除的订单
                    .OrderByDescending(o => o.CreateTime)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取订单列表成功",
                    data = orders.Select(o => new
                    {
                        o.OrderId,
                        o.UserId,
                        Product = new
                        {
                            o.Product.ProductId,
                            o.Product.ProductName,
                            o.Product.Price,
                            o.Product.ImageUrl,
                            Farmer = new
                            {
                                o.Product.Farmer.UserId,
                                o.Product.Farmer.Username,
                                o.Product.Farmer.Phone
                            }
                        },
                        o.Quantity,
                        o.TotalPrice,
                        o.Status,
                        o.CreateTime,
                        o.PayTime,
                        o.ShipTime,
                        o.CompleteTime,
                        o.CancelTime,
                        o.CancelReason,
                        o.CancelBy,
                        o.CancelByType,
                        o.ShippingAddress,
                        o.ContactPhone,
                        o.DeliveryInfo,
                        o.DeliveryContact,
                        o.DeliveryPhone,
                        o.EstimatedDeliveryTime
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取农户订单列表
        /// </summary>
        [HttpGet("farmer/{farmerId}")]
        public async Task<IActionResult> GetFarmerOrders(int farmerId, 
            [FromQuery] string status = "", 
            [FromQuery] string keyword = "",
            [FromQuery] string searchType = "all",
            [FromQuery] bool fuzzySearch = false,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // 参数验证
                if (pageSize <= 0 || pageSize > 100)
                {
                    pageSize = 50; // 限制每页最大100条记录以提升性能
                }
                if (page <= 0)
                {
                    page = 1;
                }
                
                Console.WriteLine($"GetFarmerOrders接口被调用：farmerId={farmerId}, status={status}, keyword={keyword}, searchType={searchType}, fuzzySearch={fuzzySearch}, startDate={startDate}, endDate={endDate}");
                
                // 检查农户是否存在
                var farmer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == farmerId && u.Role == "farmer");
                if (farmer == null)
                {
                    Console.WriteLine($"农户ID {farmerId} 不存在或不是农户角色");
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                Console.WriteLine($"找到农户：{farmer.Username}");

                // 构建基础查询
                var query = _context.Orders
                    .Include(o => o.Product)
                    .Include(o => o.User)
                    .Where(o => o.Product.FarmerId == farmerId);

                Console.WriteLine($"初始查询构建完成，开始应用筛选条件");
                
                // 按状态筛选
                if (!string.IsNullOrEmpty(status))
                {
                    Console.WriteLine($"应用状态筛选: status='{status}'");
                    query = query.Where(o => o.Status == status);
                    Console.WriteLine($"状态筛选应用完成");
                }

                // 按关键词搜索 - 优化后的搜索逻辑
                if (!string.IsNullOrEmpty(keyword))
                {
                    Console.WriteLine($"应用关键词筛选: keyword='{keyword}', searchType='{searchType}', fuzzySearch={fuzzySearch}");
                    
                    // 确保关键词安全（防止SQL注入）
                    keyword = keyword.Trim();
                    if (keyword.Length > 100)
                    {
                        keyword = keyword.Substring(0, 100); // 限制关键词长度
                    }
                    
                    switch (searchType.ToLower())
                    {
                        case "order":
                            // 只搜索订单号
                            if (fuzzySearch)
                            {
                                query = query.Where(o => o.OrderId.ToString().Contains(keyword));
                            }
                            else
                            {
                                // 精确匹配订单号
                                if (int.TryParse(keyword, out int orderId))
                                {
                                    query = query.Where(o => o.OrderId == orderId);
                                }
                                else
                                {
                                    // 如果关键词不是数字，返回空结果
                                    query = query.Where(o => false);
                                }
                            }
                            break;
                            
                        case "user":
                            // 只搜索客户名称
                            if (fuzzySearch)
                            {
                                query = query.Where(o => o.User.Username.Contains(keyword));
                            }
                            else
                            {
                                query = query.Where(o => o.User.Username == keyword);
                            }
                            break;
                            
                        case "product":
                            // 只搜索商品名称
                            if (fuzzySearch)
                            {
                                query = query.Where(o => o.Product.ProductName.Contains(keyword));
                            }
                            else
                            {
                                query = query.Where(o => o.Product.ProductName == keyword);
                            }
                            break;
                            
                        default: // "all"
                            // 搜索所有字段
                            if (fuzzySearch)
                            {
                                query = query.Where(o => 
                                    o.OrderId.ToString().Contains(keyword) || 
                                    o.User.Username.Contains(keyword) ||
                                    o.Product.ProductName.Contains(keyword));
                            }
                            else
                            {
                                // 精确匹配
                                if (int.TryParse(keyword, out int orderIdExact))
                                {
                                    query = query.Where(o => 
                                        o.OrderId == orderIdExact ||
                                        o.User.Username == keyword ||
                                        o.Product.ProductName == keyword);
                                }
                                else
                                {
                                    query = query.Where(o => 
                                        o.User.Username == keyword ||
                                        o.Product.ProductName == keyword);
                                }
                            }
                            break;
                    }
                    Console.WriteLine($"关键词筛选应用完成");
                }

                // 按日期筛选
                if (startDate.HasValue)
                {
                    Console.WriteLine($"应用开始日期筛选: startDate={startDate}");
                    query = query.Where(o => o.CreateTime >= startDate.Value);
                    Console.WriteLine($"开始日期筛选应用完成");
                }

                if (endDate.HasValue)
                {
                    Console.WriteLine($"应用结束日期筛选: endDate={endDate}");
                    // 包含当天，所以结束时间要加1天
                    var endDateInclusive = endDate.Value.AddDays(1);
                    query = query.Where(o => o.CreateTime < endDateInclusive);
                    Console.WriteLine($"结束日期筛选应用完成");
                }

                // 获取总数量（用于分页）
                var totalCount = await query.CountAsync();
                Console.WriteLine($"查询总数量: {totalCount}");

                // 应用分页并执行查询
                var orders = await query
                    .OrderByDescending(o => o.CreateTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                    
                Console.WriteLine($"查询结果：找到 {orders.Count} 个订单（第{page}页，每页{pageSize}条）");

                return Ok(new
                {
                    code = 200,
                    message = "获取订单列表成功",
                    data = orders.Select(o => new
                    {
                        o.OrderId,
                        User = new
                        {
                            o.User.UserId,
                            o.User.Username
                        },
                        Product = new
                        {
                            o.Product.ProductId,
                            o.Product.ProductName,
                            o.Product.Price,
                            o.Product.ImageUrl
                        },
                        o.Quantity,
                        o.TotalPrice,
                        o.Status,
                        o.CreateTime,
                        o.PayTime,
                        o.ShipTime,
                        o.CompleteTime,
                        o.CancelTime,
                        o.CancelReason,
                        o.CancelBy,
                        o.CancelByType,
                        o.ShippingAddress,
                        o.ContactPhone,
                        o.DeliveryInfo,
                        o.DeliveryContact,
                        o.DeliveryPhone,
                        o.EstimatedDeliveryTime
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                        hasNext = page * pageSize < totalCount,
                        hasPrevious = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取农户订单列表出错：{ex.Message}");
                Console.WriteLine($"异常详情：{ex}");
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取订单详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Product)
                    .ThenInclude(p => p.Farmer)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }

                return Ok(new
                {
                    code = 200,
                    message = "获取订单详情成功",
                    data = new
                    {
                        order.OrderId,
                        User = new
                        {
                            order.User.UserId,
                            order.User.Username
                        },
                        Product = new
                        {
                            order.Product.ProductId,
                            order.Product.ProductName,
                            order.Product.Description,
                            order.Product.Price,
                            order.Product.ImageUrl,
                            Farmer = new
                            {
                                order.Product.Farmer.UserId,
                                order.Product.Farmer.Username
                            }
                        },
                        order.Quantity,
                        order.TotalPrice,
                        order.Status,
                        order.CreateTime,
                        order.PayTime,
                        order.ShipTime,
                        order.CompleteTime,
                        order.CancelTime,
                        order.CancelReason,
                        order.CancelBy,
                        order.CancelByType,
                        order.ShippingAddress,
                        order.ContactPhone,
                        order.DeliveryInfo,
                        order.DeliveryContact,
                        order.DeliveryPhone,
                        order.EstimatedDeliveryTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 支付订单
        /// </summary>
        [HttpPut("{id}/pay")]
        public async Task<IActionResult> PayOrder(int id, [FromBody] PayOrderRequest request)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }

                // 检查是否是订单所有者
                if (order.UserId != request.UserId)
                {
                    return BadRequest(new { code = 400, message = "只有订单所有者才能支付订单" });
                }

                // 检查订单状态
                if (order.Status != "待支付")
                {
                    return BadRequest(new { code = 400, message = $"订单状态为 {order.Status}，不能支付" });
                }

                // 支付订单（实际支付逻辑在此省略）
                order.Status = "待发货";
                order.PayTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "支付订单成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 开始配送订单（已移除发货功能，改为直接开始配送）
        /// </summary>
        [HttpPut("{id}/ship")]
        public async Task<IActionResult> ShipOrder(int id, [FromBody] ShipOrderRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }

                // 检查是否是产品所有者
                if (order.Product.FarmerId != request.FarmerId)
                {
                    return BadRequest(new { code = 400, message = "只有产品所有者才能操作订单" });
                }

                // 检查订单状态
                if (order.Status != "货到付款待处理" && order.Status != "待付款" && order.Status != "已付款" && order.Status != "待发货")
                {
                    return BadRequest(new { code = 400, message = $"订单状态为 {order.Status}，不能开始配送" });
                }

                // 设置默认配送信息
                string defaultDeliveryInfo = "货到付款，请保持电话畅通，配送员会提前联系您确认收货时间。";
                string defaultContact = "配送员";
                string defaultPhone = "将在配送前联系";
                string defaultEstimatedTime = "1-3个工作日内送达";

                // 直接更新为货到付款配送中状态
                order.Status = "货到付款配送中";
                order.ShipTime = DateTime.Now;
                
                // 保存配送信息，如果没有提供则使用默认值
                order.DeliveryInfo = !string.IsNullOrWhiteSpace(request.DeliveryInfo) ? request.DeliveryInfo : defaultDeliveryInfo;
                order.DeliveryContact = !string.IsNullOrWhiteSpace(request.DeliveryContact) ? request.DeliveryContact : defaultContact;
                order.DeliveryPhone = !string.IsNullOrWhiteSpace(request.DeliveryPhone) ? request.DeliveryPhone : defaultPhone;
                order.EstimatedDeliveryTime = !string.IsNullOrWhiteSpace(request.EstimatedDeliveryTime) ? request.EstimatedDeliveryTime : defaultEstimatedTime;

                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "订单已开始配送" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 确认收货
        /// </summary>
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteOrder(int id, [FromBody] CompleteOrderRequest request)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }

                // 检查是否是订单所有者
                if (order.UserId != request.UserId)
                {
                    return BadRequest(new { code = 400, message = "只有订单所有者才能确认收货" });
                }

                // 检查订单状态
                if (order.Status != "待收货" && order.Status != "货到付款配送中")
                {
                    return BadRequest(new { code = 400, message = $"订单状态为 {order.Status}，不能确认收货" });
                }

                // 确认收货
                order.Status = "已完成";
                order.CompleteTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "确认收货成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 取消订单
        /// </summary>
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }

                // 检查是否是订单所有者或农户（产品所有者）
                bool isOwner = order.UserId == request.UserId;
                bool isFarmer = order.Product != null && order.Product.FarmerId == request.UserId;
                
                if (!isOwner && !isFarmer)
                {
                    return BadRequest(new { code = 400, message = "只有订单所有者或农户才能取消订单" });
                }

                // 检查订单状态
                if (order.Status != "待支付" && order.Status != "待发货" && order.Status != "货到付款待处理" && order.Status != "货到付款配送中")
                {
                    return BadRequest(new { code = 400, message = $"订单状态为 {order.Status}，不能取消" });
                }

                // 恢复库存
                if (order.Product != null)
                {
                    order.Product.Stock += order.Quantity;
                    order.Product.UpdateTime = DateTime.Now;
                }

                // 取消订单
                order.Status = "已取消";
                order.CancelTime = DateTime.Now;
                order.CancelBy = request.UserId;
                order.CancelByType = isOwner ? "user" : "farmer";
                order.CancelReason = request.CancelReason;

                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "取消订单成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 从购物车创建订单
        /// </summary>
        [HttpPost("from-cart")]
        public async Task<IActionResult> CreateFromCart([FromBody] CreateFromCartRequest request)
        {
            try
            {
                Console.WriteLine($"CreateFromCart接口被调用：userId={request.UserId}");
                
                // 检查用户是否存在
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    Console.WriteLine($"用户ID {request.UserId} 不存在");
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                Console.WriteLine($"找到用户：{user.Username}");

                // 获取用户购物车商品
                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == request.UserId)
                    .ToListAsync();

                if (cartItems.Count == 0)
                {
                    Console.WriteLine("购物车为空");
                    return BadRequest(new { code = 400, message = "购物车为空" });
                }

                Console.WriteLine($"购物车商品数量：{cartItems.Count}");
                
                // 如果选中了特定商品，则只处理选中的商品
                if (request.SelectedItems != null && request.SelectedItems.Count > 0)
                {
                    Console.WriteLine($"选中了{request.SelectedItems.Count}个商品");
                    cartItems = cartItems.Where(item => request.SelectedItems.Contains(item.CartItemId)).ToList();
                    
                    if (cartItems.Count == 0)
                    {
                        Console.WriteLine("没有找到选中的商品");
                        return BadRequest(new { code = 400, message = "未找到选中的商品" });
                    }
                }

                // 检查库存是否充足
                var outOfStockItems = cartItems.Where(c => c.Quantity > c.Product.Stock).ToList();
                if (outOfStockItems.Any())
                {
                    var products = string.Join(", ", outOfStockItems.Select(c => c.Product.ProductName));
                    Console.WriteLine($"库存不足的商品: {products}");
                    return BadRequest(new { 
                        code = 400, 
                        message = "部分商品库存不足", 
                        data = outOfStockItems.Select(c => new { 
                            c.Product.ProductId, 
                            c.Product.ProductName, 
                            c.Product.Stock, 
                            c.Quantity
                        })
                    });
                }

                // 1. 创建订单组
                string groupNumber = GenerateOrderGroupNumber();
                decimal productTotalAmount = cartItems.Sum(item => item.Product.Price * item.Quantity);
                
                // 2. 计算运费 - 根据配送区域和运费规则
                decimal shippingFee = 0;
                int? shippingFeeId = null;
                
                if (request.ShippingFeeId.HasValue)
                {
                    // 获取运费规则
                    var shippingFeeRule = await _context.ShippingFees
                        .FirstOrDefaultAsync(s => s.ShippingFeeId == request.ShippingFeeId && s.IsEnabled);
                    
                    if (shippingFeeRule != null)
                    {
                        // 检查是否满足免运费条件
                        if (shippingFeeRule.FreeShippingThreshold > 0 && productTotalAmount >= shippingFeeRule.FreeShippingThreshold)
                        {
                            shippingFee = 0;
                        }
                        else
                        {
                            // 基本运费 + 额外每公斤费用（后续可以考虑根据商品重量计算）
                            shippingFee = shippingFeeRule.BaseFee;
                        }
                        
                        shippingFeeId = shippingFeeRule.ShippingFeeId;
                    }
                }
                
                // 3. 创建OrderGroup实体
                var orderGroup = new OrderGroup
                {
                    UserId = request.UserId,
                    GroupNumber = groupNumber,
                    CreateTime = DateTime.Now,
                    OrderCount = cartItems.Count,
                    TotalProductAmount = productTotalAmount,
                    ShippingFeeAmount = shippingFee,
                    TotalAmount = productTotalAmount + shippingFee,
                    ShippingAddress = request.ShippingAddress,
                    ContactPhone = request.ContactPhone,
                    ReceiverName = request.ReceiverName ?? "收货人", // 确保有收货人姓名
                    ShippingFeeId = shippingFeeId,
                    DeliveryAreaId = request.DeliveryAreaId
                };
                
                _context.OrderGroups.Add(orderGroup);
                await _context.SaveChangesAsync(); // 保存订单组以获取ID
                
                // 4. 创建多个订单并关联到订单组
                var createdOrders = new List<Order>();

                foreach (var item in cartItems)
                {
                    var product = item.Product;
                    decimal itemTotal = product.Price * item.Quantity;
                    
                    // 创建订单
                    var order = new Order
                    {
                        UserId = request.UserId,
                        ProductId = product.ProductId,
                        OrderGroupId = orderGroup.OrderGroupId, // 关联到订单组
                        Quantity = item.Quantity,
                        TotalPrice = itemTotal,
                        Status = "货到付款待处理",
                        CreateTime = DateTime.Now,
                        PayTime = null,
                        ShipTime = null,
                        CompleteTime = null,
                        ShippingAddress = request.ShippingAddress,
                        ContactPhone = request.ContactPhone,
                        ShippingFeeAmount = 0 // 单个订单不再分配运费
                    };

                    // 减少库存
                    product.Stock -= item.Quantity;
                    product.UpdateTime = DateTime.Now;

                    _context.Orders.Add(order);
                    createdOrders.Add(order);
                }

                // 只从购物车中移除已下单的商品
                var cartItemsToRemove = cartItems.ToList();
                _context.CartItems.RemoveRange(cartItemsToRemove);
                
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"创建了订单组 {groupNumber}，包含 {createdOrders.Count} 个订单，总金额：{orderGroup.TotalAmount}");

                return Ok(new
                {
                    code = 200,
                    message = "创建订单成功",
                    data = new
                    {
                        orderGroup = new {
                            orderGroup.OrderGroupId,
                            orderGroup.GroupNumber,
                            orderGroup.TotalProductAmount,
                            orderGroup.ShippingFeeAmount,
                            orderGroup.TotalAmount,
                            orderGroup.OrderCount,
                            orderGroup.CreateTime
                        },
                        orders = createdOrders.Select(o => new {
                            o.OrderId,
                            o.ProductId,
                            o.Quantity,
                            o.TotalPrice,
                            o.Status,
                            o.CreateTime
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建订单异常: {ex.Message}");
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 生成订单组编号
        /// </summary>
        private string GenerateOrderGroupNumber()
        {
            // 生成格式: OG + 年月日 + 6位随机数
            string datePrefix = DateTime.Now.ToString("yyyyMMdd");
            string randomPart = new Random().Next(100000, 999999).ToString();
            return $"OG{datePrefix}{randomPart}";
        }

        /// <summary>
        /// 直接购买创建订单
        /// </summary>
        [HttpPost("direct-buy")]
        public async Task<IActionResult> CreateDirectOrder([FromBody] DirectBuyOrderRequest request)
        {
            try
            {
                Console.WriteLine($"CreateDirectOrder接口被调用：userId={request.UserId}, productId={request.ProductId}, quantity={request.Quantity}");
                
                // 检查用户是否存在
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    Console.WriteLine($"用户ID {request.UserId} 不存在");
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                Console.WriteLine($"找到用户：{user.Username}");

                // 检查产品是否存在且激活
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == request.ProductId && p.IsActive);
                if (product == null)
                {
                    Console.WriteLine($"产品ID {request.ProductId} 不存在或未激活");
                    return NotFound(new { code = 404, message = "产品不存在或已下架" });
                }

                Console.WriteLine($"找到产品：{product.ProductName}，库存：{product.Stock}，请求数量：{request.Quantity}");

                // 检查库存是否充足
                if (product.Stock < request.Quantity)
                {
                    Console.WriteLine("库存不足");
                    return BadRequest(new { code = 400, message = "库存不足", data = new { product.ProductId, product.ProductName, product.Stock, RequestQuantity = request.Quantity } });
                }

                // 计算订单总价
                decimal productTotal = product.Price * request.Quantity;
                
                // 1. 创建订单组
                string groupNumber = GenerateOrderGroupNumber();
                decimal shippingFee = request.ShippingFee; // 使用请求中的运费
                
                var orderGroup = new OrderGroup
                {
                    UserId = request.UserId,
                    GroupNumber = groupNumber,
                    CreateTime = DateTime.Now,
                    OrderCount = 1, // 直接购买只有一个订单
                    TotalProductAmount = productTotal,
                    ShippingFeeAmount = shippingFee,
                    TotalAmount = productTotal + shippingFee,
                    ShippingAddress = request.ShippingAddress,
                    ContactPhone = request.ContactPhone,
                    ReceiverName = request.ReceiverName,
                    ShippingFeeId = request.ShippingFeeId,
                    DeliveryAreaId = null // 直接购买目前没有传递区域ID
                };
                
                _context.OrderGroups.Add(orderGroup);
                await _context.SaveChangesAsync(); // 保存订单组以获取ID
                
                // 2. 创建订单并关联到订单组
                var order = new Order
                {
                    UserId = request.UserId,
                    ProductId = request.ProductId,
                    OrderGroupId = orderGroup.OrderGroupId, // 关联到订单组
                    Quantity = request.Quantity,
                    TotalPrice = productTotal,
                    Status = "货到付款待处理",
                    CreateTime = DateTime.Now,
                    PayTime = null,
                    ShipTime = null,
                    CompleteTime = null,
                    ShippingAddress = request.ShippingAddress,
                    ContactPhone = request.ContactPhone,
                    ShippingFeeAmount = 0 // 单个订单不再分配运费，运费在订单组中
                };

                // 减少库存
                product.Stock -= request.Quantity;
                product.UpdateTime = DateTime.Now;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"创建了订单组 {groupNumber}，一个直接购买订单，总金额：{orderGroup.TotalAmount}");

                return Ok(new
                {
                    code = 200,
                    message = "创建订单成功",
                    data = new
                    {
                        orderGroup = new {
                            orderGroup.OrderGroupId,
                            orderGroup.GroupNumber,
                            orderGroup.TotalProductAmount,
                            orderGroup.ShippingFeeAmount,
                            orderGroup.TotalAmount,
                            orderGroup.CreateTime
                        },
                        order = new
                        {
                            order.OrderId,
                            order.ProductId,
                            order.Quantity,
                            order.TotalPrice,
                            order.Status,
                            order.CreateTime
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建订单异常: {ex.Message}");
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除订单（支持软删除和硬删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id, [FromQuery] int userId)
        {
            try
            {
                Console.WriteLine($"收到删除订单请求: 订单ID={id}, 用户ID={userId}");
                
                // 查找订单
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }

                // 检查权限（只有订单所有者才能删除）
                if (order.UserId != userId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权删除此订单" });
                }

                // 检查是否满足硬删除条件：
                // 1. 订单已取消
                // 2. 取消人是当前用户
                bool canHardDelete = order.Status == "已取消" && 
                                    order.CancelByType == "user" && 
                                    order.CancelBy == userId;

                if (canHardDelete)
                {
                    // 硬删除 - 从数据库中完全删除
                    _context.Orders.Remove(order);
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { code = 200, message = "订单已彻底删除" });
                }
                else
                {
                    // 软删除 - 仅标记为已删除
                    order.IsDeleted = true;
                    order.DeleteTime = DateTime.Now;
                    
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { code = 200, message = "订单已删除（软删除）" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 申请退款
        /// </summary>
        [HttpPost("{id}/refund-request")]
        public async Task<IActionResult> RequestRefund(int id, [FromBody] RequestRefundDto request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }

                // 验证是用户发起的申请
                if (order.UserId != request.UserId)
                {
                    return BadRequest(new { code = 400, message = "只有订单用户才能申请退款" });
                }

                // 检查订单状态，只有已完成或配送中的订单可以申请退款
                if (order.Status != "已完成" && order.Status != "货到付款配送中")
                {
                    return BadRequest(new { code = 400, message = $"订单状态为 {order.Status}，不能申请退款" });
                }

                // 更新订单状态为申请退款中
                order.Status = "申请退款中";
                
                // 记录申请信息
                order.CancelReason = request.RefundReason;
                order.CancelBy = request.UserId;
                order.CancelByType = "user";
                order.RefundRequestTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "退款申请提交成功，等待农户处理" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 处理退款（农户或管理员操作）
        /// </summary>
        [HttpPost("{id}/process-refund")]
        public async Task<IActionResult> ProcessRefund(int id, [FromBody] ProcessRefundDto request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }

                // 验证是农户或管理员操作
                bool isFarmer = order.Product.FarmerId == request.ProcessorId;
                var processor = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.ProcessorId);
                bool isAdmin = processor?.Role == "admin";

                if (!isFarmer && !isAdmin)
                {
                    return BadRequest(new { code = 400, message = "只有农户或管理员才能处理退款" });
                }

                // 检查订单状态，只有已完成、配送中或申请退款中的订单可以退款
                if (order.Status != "已完成" && order.Status != "货到付款配送中" && order.Status != "申请退款中")
                {
                    return BadRequest(new { code = 400, message = $"订单状态为 {order.Status}，不能退款" });
                }

                // 处理退款
                if (request.IsApproved)
                {
                    // 恢复库存
                    if (order.Product != null)
                    {
                        order.Product.Stock += order.Quantity;
                        order.Product.UpdateTime = DateTime.Now;
                    }

                    // 更新订单状态为已取消（而不是已退款）
                    order.Status = "已取消";
                }
                else
                {
                    // 拒绝退款，恢复原状态
                    order.Status = order.Status == "申请退款中" ? (order.CompleteTime.HasValue ? "已完成" : "货到付款配送中") : order.Status;
                }
                
                // 记录处理信息
                order.CancelReason = request.ProcessDescription + (request.IsApproved ? " (已批准)" : " (已拒绝)");
                order.CancelBy = request.ProcessorId;
                order.CancelByType = isFarmer ? "farmer" : "admin";
                order.CancelTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { 
                    code = 200, 
                    message = request.IsApproved ? "退款处理完成，订单已取消" : "已拒绝退款申请"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 批量发货
        /// </summary>
        [HttpPost("batch-ship")]
        public async Task<IActionResult> BatchShipOrders([FromBody] BatchShipOrdersRequest request)
        {
            // 使用数据库事务确保数据一致性
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                Console.WriteLine($"BatchShipOrders接口被调用：farmerId={request.FarmerId}, orderIds={string.Join(",", request.OrderIds)}");
                
                // 参数验证
                if (request.OrderIds == null || request.OrderIds.Count == 0)
                {
                    return BadRequest(new { code = 400, message = "请选择要发货的订单" });
                }

                // 批量限制：单次最多处理100个订单
                const int maxBatchSize = 100;
                if (request.OrderIds.Count > maxBatchSize)
                {
                    return BadRequest(new { 
                        code = 400, 
                        message = $"单次批量发货最多支持 {maxBatchSize} 个订单，当前选择了 {request.OrderIds.Count} 个。请分批处理。" 
                    });
                }

                // 验证农户是否存在
                var farmer = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == request.FarmerId && u.Role == "farmer");
                
                if (farmer == null)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 批量获取订单并验证（使用一次查询替代循环查询）
                var orders = await _context.Orders
                    .Include(o => o.Product)
                    .Where(o => request.OrderIds.Contains(o.OrderId))
                    .ToListAsync();

                // 验证订单存在性
                if (orders.Count == 0)
                {
                    return BadRequest(new { code = 400, message = "未找到任何有效订单" });
                }

                // 检查是否有缺失的订单
                var foundOrderIds = orders.Select(o => o.OrderId).ToHashSet();
                var missingOrderIds = request.OrderIds.Where(id => !foundOrderIds.Contains(id)).ToList();
                if (missingOrderIds.Any())
                {
                    Console.WriteLine($"缺失的订单ID: {string.Join(", ", missingOrderIds)}");
                }

                // 验证所有订单都属于该农户
                var invalidOwnershipOrders = orders
                    .Where(o => o.Product?.FarmerId != request.FarmerId)
                    .ToList();
                
                if (invalidOwnershipOrders.Any())
                {
                    var invalidIds = string.Join(", ", invalidOwnershipOrders.Select(o => o.OrderId));
                    return BadRequest(new { 
                        code = 400, 
                        message = $"订单 {invalidIds} 不属于该农户，无权限操作" 
                    });
                }

                // 验证订单状态（只有特定状态的订单才能发货）
                var allowedStatuses = new[] { "货到付款待处理", "待付款", "已付款", "待发货" };
                var invalidStatusOrders = orders
                    .Where(o => !allowedStatuses.Contains(o.Status))
                    .ToList();

                if (invalidStatusOrders.Any())
                {
                    var invalidDetails = invalidStatusOrders
                        .Select(o => $"#{o.OrderId}({o.Status})")
                        .ToList();
                    return BadRequest(new { 
                        code = 400, 
                        message = $"以下订单状态不允许发货: {string.Join(", ", invalidDetails)}" 
                    });
                }

                // 准备批量更新数据
                var shipTime = DateTime.Now;
                var successCount = 0;
                var failedOrders = new List<string>();
                
                // 设置默认配送信息
                string defaultDeliveryInfo = "货到付款，请保持电话畅通，配送员会提前联系您确认收货时间。";
                string defaultContact = "配送员";
                string defaultPhone = "将在配送前联系";
                string defaultEstimatedTime = "1-3个工作日内送达";

                // 批量更新订单状态和配送信息
                foreach (var order in orders)
                {
                    try
                    {
                        // 更新订单状态和时间
                        order.Status = "货到付款配送中";
                        order.ShipTime = shipTime;
                        
                        // 保存配送信息，优先使用用户输入，否则使用默认值
                        order.DeliveryInfo = !string.IsNullOrWhiteSpace(request.DeliveryInfo) 
                            ? request.DeliveryInfo 
                            : defaultDeliveryInfo;
                        order.DeliveryContact = !string.IsNullOrWhiteSpace(request.DeliveryContact) 
                            ? request.DeliveryContact 
                            : defaultContact;
                        order.DeliveryPhone = !string.IsNullOrWhiteSpace(request.DeliveryPhone) 
                            ? request.DeliveryPhone 
                            : defaultPhone;
                        order.EstimatedDeliveryTime = !string.IsNullOrWhiteSpace(request.EstimatedDeliveryTime) 
                            ? request.EstimatedDeliveryTime 
                            : defaultEstimatedTime;
                        
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"订单 {order.OrderId} 更新失败：{ex.Message}");
                        failedOrders.Add($"订单 #{order.OrderId}: {ex.Message}");
                    }
                }

                // 一次性保存所有更改（大大提高性能）
                var savedChanges = await _context.SaveChangesAsync();
                Console.WriteLine($"批量发货：保存了 {savedChanges} 个更改");

                // 提交事务
                await transaction.CommitAsync();

                // 记录操作日志
                Console.WriteLine($"批量发货完成：农户 {request.FarmerId}，成功 {successCount}/{orders.Count} 个订单");

                return Ok(new
                {
                    code = 200,
                    message = failedOrders.Any() 
                        ? $"批量发货部分完成，成功发货 {successCount} 个订单，{failedOrders.Count} 个失败"
                        : $"批量发货完成，成功发货 {successCount} 个订单",
                    data = new
                    {
                        successCount = successCount,
                        totalCount = orders.Count,
                        failedOrders = failedOrders,
                        processedAt = DateTime.Now,
                        batchSize = orders.Count
                    }
                });
            }
            catch (Exception ex)
            {
                // 回滚事务
                await transaction.RollbackAsync();
                
                Console.WriteLine($"批量发货异常：{ex.Message}");
                Console.WriteLine($"异常堆栈：{ex.StackTrace}");
                
                return StatusCode(500, new { 
                    code = 500, 
                    message = "批量发货失败: " + ex.Message,
                    details = "系统已回滚所有更改，请稍后重试"
                });
            }
        }

        /// <summary>
        /// 更新配送信息
        /// </summary>
        [HttpPut("{id}/update-delivery")]
        public async Task<IActionResult> UpdateDeliveryInfo(int id, [FromBody] UpdateDeliveryInfoRequest request)
        {
            try
            {
                // 查找订单
                var order = await _context.Orders
                    .Include(o => o.Product)
                    .ThenInclude(p => p.Farmer)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }

                // 验证农户权限
                if (order.Product.FarmerId != request.FarmerId)
                {
                    return StatusCode(403, new { code = 403, message = "无权限操作此订单" });
                }

                // 验证订单状态：只有配送中的订单才能更新配送信息
                if (order.Status != "货到付款配送中")
                {
                    return BadRequest(new { code = 400, message = "只有配送中的订单才能更新配送信息" });
                }

                // 更新配送信息
                order.DeliveryInfo = request.DeliveryInfo;
                order.DeliveryContact = request.DeliveryContact;
                order.DeliveryPhone = request.DeliveryPhone;
                order.EstimatedDeliveryTime = request.EstimatedDeliveryTime;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "配送信息更新成功",
                    data = new
                    {
                        order.OrderId,
                        order.Status,
                        order.DeliveryInfo,
                        order.DeliveryContact,
                        order.DeliveryPhone,
                        order.EstimatedDeliveryTime,
                        UpdateTime = DateTime.Now
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = $"更新配送信息失败: {ex.Message}" });
            }
        }
    }

    /// <summary>
    /// 创建订单请求模型
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }

        /// <summary>
        /// 产品ID
        /// </summary>
        [Required(ErrorMessage = "产品ID不能为空")]
        public int ProductId { get; set; }

        /// <summary>
        /// 购买数量
        /// </summary>
        [Required(ErrorMessage = "购买数量不能为空")]
        [Range(1, int.MaxValue, ErrorMessage = "购买数量必须大于0")]
        public int Quantity { get; set; }

        /// <summary>
        /// 收货地址
        /// </summary>
        [Required(ErrorMessage = "收货地址不能为空")]
        public string ShippingAddress { get; set; } = string.Empty;

        /// <summary>
        /// 联系电话
        /// </summary>
        [Required(ErrorMessage = "联系电话不能为空")]
        public string ContactPhone { get; set; } = string.Empty;
    }

    /// <summary>
    /// 支付订单请求模型
    /// </summary>
    public class PayOrderRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }
    }

    /// <summary>
    /// 开始配送请求模型（已移除物流信息字段）
    /// </summary>
    public class ShipOrderRequest
    {
        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }
        
        /// <summary>
        /// 配送信息备注
        /// </summary>
        public string? DeliveryInfo { get; set; }

        /// <summary>
        /// 配送联系人
        /// </summary>
        public string? DeliveryContact { get; set; }

        /// <summary>
        /// 配送联系电话
        /// </summary>
        public string? DeliveryPhone { get; set; }

        /// <summary>
        /// 预计送达时间
        /// </summary>
        public string? EstimatedDeliveryTime { get; set; }
    }

    /// <summary>
    /// 确认收货请求模型
    /// </summary>
    public class CompleteOrderRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }
    }

    /// <summary>
    /// 取消订单请求模型
    /// </summary>
    public class CancelOrderRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }
        
        /// <summary>
        /// 取消原因
        /// </summary>
        public string? CancelReason { get; set; }
    }

    public class CreateFromCartRequest
    {
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "收货地址不能为空")]
        [StringLength(500, ErrorMessage = "收货地址最多500个字符")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "联系电话不能为空")]
        [StringLength(20, ErrorMessage = "联系电话最多20个字符")]
        [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确")]
        public string ContactPhone { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? ReceiverName { get; set; }
        
        public int? ShippingFeeId { get; set; }
        
        public int? DeliveryAreaId { get; set; }

        public List<int> SelectedItems { get; set; } = new List<int>();
    }

    /// <summary>
    /// 申请退款请求模型
    /// </summary>
    public class RequestRefundDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }
        
        /// <summary>
        /// 退款原因
        /// </summary>
        [Required(ErrorMessage = "退款原因不能为空")]
        public string RefundReason { get; set; } = string.Empty;
        
        /// <summary>
        /// 退款类型（全额/部分）
        /// </summary>
        public string RefundType { get; set; } = "full";
        
        /// <summary>
        /// 退款金额（部分退款时使用）
        /// </summary>
        public decimal? RefundAmount { get; set; }
    }
    
    /// <summary>
    /// 处理退款请求模型
    /// </summary>
    public class ProcessRefundDto
    {
        /// <summary>
        /// 处理人ID（农户或管理员）
        /// </summary>
        [Required(ErrorMessage = "处理人ID不能为空")]
        public int ProcessorId { get; set; }
        
        /// <summary>
        /// 是否同意退款
        /// </summary>
        [Required(ErrorMessage = "请指明是否同意退款")]
        public bool IsApproved { get; set; }
        
        /// <summary>
        /// 处理说明
        /// </summary>
        [Required(ErrorMessage = "处理说明不能为空")]
        [StringLength(500, ErrorMessage = "处理说明最多500字")]
        public string ProcessDescription { get; set; }
        
        /// <summary>
        /// 退款原因
        /// </summary>
        public string RefundReason { get; set; } = string.Empty;
        
        /// <summary>
        /// 退款金额
        /// </summary>
        public decimal? RefundAmount { get; set; }
    }
    
    public class DirectBuyOrderRequest
    {
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "产品ID不能为空")]
        public int ProductId { get; set; }
        
        [Required(ErrorMessage = "购买数量不能为空")]
        [Range(1, int.MaxValue, ErrorMessage = "购买数量必须大于0")]
        public int Quantity { get; set; }
        
        [Required(ErrorMessage = "收货地址不能为空")]
        [StringLength(200, ErrorMessage = "收货地址最多200字")]
        public string ShippingAddress { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "联系电话不能为空")]
        [StringLength(20, ErrorMessage = "联系电话最多20字")]
        public string ContactPhone { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "联系人姓名不能为空")]
        [StringLength(50, ErrorMessage = "联系人姓名最多50字")]
        public string ReceiverName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "配送方式不能为空")]
        [StringLength(20, ErrorMessage = "配送方式最多20字")]
        public string DeliveryMethod { get; set; } = string.Empty;
        
        public decimal ShippingFee { get; set; }
        
        public int? ShippingFeeId { get; set; }
    }
        
    /// <summary>
    /// 批量发货请求模型
    /// </summary>
    public class BatchShipOrdersRequest
    {
        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }

        /// <summary>
        /// 订单ID列表
        /// </summary>
        [Required(ErrorMessage = "订单ID列表不能为空")]
        public List<int> OrderIds { get; set; } = new List<int>();
        
        /// <summary>
        /// 配送信息备注
        /// </summary>
        public string? DeliveryInfo { get; set; }

        /// <summary>
        /// 配送联系人
        /// </summary>
        public string? DeliveryContact { get; set; }

        /// <summary>
        /// 配送联系电话
        /// </summary>
        public string? DeliveryPhone { get; set; }

        /// <summary>
        /// 预计送达时间
        /// </summary>
        public string? EstimatedDeliveryTime { get; set; }
    }

    /// <summary>
    /// 更新配送信息请求模型
    /// </summary>
    public class UpdateDeliveryInfoRequest
    {
        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }
        
        /// <summary>
        /// 配送信息备注
        /// </summary>
        public string? DeliveryInfo { get; set; }

        /// <summary>
        /// 配送联系人
        /// </summary>
        public string? DeliveryContact { get; set; }

        /// <summary>
        /// 配送联系电话
        /// </summary>
        public string? DeliveryPhone { get; set; }

        /// <summary>
        /// 预计送达时间
        /// </summary>
        public string? EstimatedDeliveryTime { get; set; }
    }
} 
 
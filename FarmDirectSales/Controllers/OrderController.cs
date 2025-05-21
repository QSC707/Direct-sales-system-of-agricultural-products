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
                        o.ContactPhone
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
        public async Task<IActionResult> GetFarmerOrders(int farmerId, [FromQuery] string status = "", [FromQuery] string keyword = "")
        {
            try
            {
                Console.WriteLine($"GetFarmerOrders接口被调用：farmerId={farmerId}, status={status}, keyword={keyword}");
                
                // 检查农户是否存在
                var farmer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == farmerId && u.Role == "farmer");
                if (farmer == null)
                {
                    Console.WriteLine($"农户ID {farmerId} 不存在或不是农户角色");
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                Console.WriteLine($"找到农户：{farmer.Username}");

                // 构建查询
                var query = _context.Orders
                    .Include(o => o.Product)
                    .Include(o => o.User)
                    .Where(o => o.Product.FarmerId == farmerId);

                Console.WriteLine($"初始查询构建完成，开始应用筛选条件");
                
                // 按状态筛选
                if (!string.IsNullOrEmpty(status))
                {
                    Console.WriteLine($"应用状态筛选: status='{status}'");
                    // 使用常规的 == 比较而不是 string.Equals
                    query = query.Where(o => o.Status == status);
                    Console.WriteLine($"状态筛选应用完成");
                }

                // 按关键词搜索（订单ID或客户用户名）
                if (!string.IsNullOrEmpty(keyword))
                {
                    Console.WriteLine($"应用关键词筛选: keyword='{keyword}'");
                    query = query.Where(o => o.OrderId.ToString().Contains(keyword) || 
                                          o.User.Username.Contains(keyword));
                    Console.WriteLine($"关键词筛选应用完成");
                }

                Console.WriteLine($"执行查询前的最终查询语句: {query.ToQueryString()}");
                
                var orders = await query
                    .OrderByDescending(o => o.CreateTime)
                    .ToListAsync();
                    
                Console.WriteLine($"查询结果：找到 {orders.Count} 个订单");

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
                        o.ContactPhone
                    })
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

                // 直接更新为货到付款配送中状态
                order.Status = "货到付款配送中";
                order.ShipTime = DateTime.Now;

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

                decimal totalAmount = 0;
                var createdOrders = new List<Order>();

                // 创建订单
                foreach (var item in cartItems)
                {
                    var product = item.Product;
                    decimal itemTotal = product.Price * item.Quantity;
                    
                    // 创建订单
                    var order = new Order
                    {
                        UserId = request.UserId,
                        ProductId = product.ProductId,
                        Quantity = item.Quantity,
                        TotalPrice = itemTotal,
                        Status = "货到付款待处理",
                        CreateTime = DateTime.Now,
                        PayTime = null,
                        ShipTime = null,
                        CompleteTime = null,
                        ShippingAddress = request.ShippingAddress,
                        ContactPhone = request.ContactPhone
                    };

                    // 减少库存
                    product.Stock -= item.Quantity;
                    product.UpdateTime = DateTime.Now;

                    _context.Orders.Add(order);
                    createdOrders.Add(order);
                    totalAmount += itemTotal;
                }

                // 只从购物车中移除已下单的商品
                var cartItemsToRemove = cartItems.ToList();
                _context.CartItems.RemoveRange(cartItemsToRemove);
                
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"创建了 {createdOrders.Count} 个订单，总金额：{totalAmount}");

                return Ok(new
                {
                    code = 200,
                    message = "创建订单成功",
                    data = new
                    {
                        orders = createdOrders.Select(o => new {
                            o.OrderId,
                            o.ProductId,
                            o.Quantity,
                            o.TotalPrice,
                            o.Status,
                            o.CreateTime
                        }),
                        totalAmount,
                        orderCount = createdOrders.Count
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
                order.CancelReason = request.RefundReason + (request.IsApproved ? " (已批准)" : " (已拒绝)");
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
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "联系电话不能为空")]
        [StringLength(20, ErrorMessage = "联系电话最多20个字符")]
        [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确")]
        public string ContactPhone { get; set; }
        
        [StringLength(20)]
        public string DeliveryMethod { get; set; } = "express";
        
        // 选中的购物车项ID列表
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
        
        /// <summary>
        /// 备注说明
        /// </summary>
        public string? Notes { get; set; }
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
        /// 退款原因/处理说明
        /// </summary>
        [Required(ErrorMessage = "处理说明不能为空")]
        public string RefundReason { get; set; } = string.Empty;
        
        /// <summary>
        /// 退款金额
        /// </summary>
        public decimal? RefundAmount { get; set; }
        
        /// <summary>
        /// 备注说明
        /// </summary>
        public string? Notes { get; set; }
    }
} 
 
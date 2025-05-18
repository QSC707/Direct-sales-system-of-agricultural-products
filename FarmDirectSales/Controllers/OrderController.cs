using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FarmDirectSales.Data;
using FarmDirectSales.Models;
using Microsoft.AspNetCore.Authorization;

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
                    Status = "待支付",
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
                    .Include(o => o.Review)
                    .Where(o => o.UserId == userId)
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
                                o.Product.Farmer.Username
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
                        isReviewed = o.Review != null,
                        review = o.Review != null ? new
                        {
                            o.Review.Rating,
                            o.Review.Content,
                            o.Review.CreateTime,
                            o.Review.IsAnonymous
                        } : null
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
        /// 发货
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
                    return BadRequest(new { code = 400, message = "只有产品所有者才能发货" });
                }

                // 检查订单状态
                if (order.Status != "待发货")
                {
                    return BadRequest(new { code = 400, message = $"订单状态为 {order.Status}，不能发货" });
                }

                // 发货
                order.Status = "待收货";
                order.ShipTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "发货成功" });
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
                if (order.Status != "待收货")
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
                if (order.Status != "待支付" && order.Status != "待发货")
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
    /// 发货请求模型
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
} 
 
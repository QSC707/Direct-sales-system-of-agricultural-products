using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmDirectSales.Data;
using FarmDirectSales.Models;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 订单组控制器，用于处理订单组相关的API请求
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrderGroupController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public OrderGroupController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取用户的订单组列表（支持筛选、排序和分页）
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserOrderGroups(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string keyword = null,
            [FromQuery] string sortBy = "CreateTime",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                // 参数验证
                if (pageSize <= 0 || pageSize > 100)
                {
                    pageSize = 20; // 限制每页最大100条记录
                }
                if (page <= 0)
                {
                    page = 1;
                }

                // 检查用户是否存在
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                // 构建查询
                var query = _context.OrderGroups
                    .Where(g => g.UserId == userId)
                    .Include(g => g.Orders)
                    .AsQueryable();

                // 日期范围筛选
                if (startDate.HasValue)
                {
                    query = query.Where(g => g.CreateTime >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    query = query.Where(g => g.CreateTime <= endDate.Value);
                }

                // 关键词搜索（扩展搜索范围）
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(g => 
                        g.GroupNumber.Contains(keyword) ||                    // 搜索订单组编号
                        g.Orders.Any(o => 
                            o.Product.ProductName.Contains(keyword) ||        // 搜索商品名称
                            o.Product.Farmer.Username.Contains(keyword) ||    // 搜索农户名称
                            o.OrderId.ToString().Contains(keyword)            // 搜索订单ID
                        )
                    );
                }

                // 状态筛选（基于订单组内订单的状态）
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    query = query.Where(g => g.Orders.Any(o => o.Status == status));
                }

                // 排序
                IOrderedQueryable<OrderGroup> orderedQuery;
                switch (sortBy?.ToLower())
                {
                    case "totalamount":
                        orderedQuery = sortOrder?.ToLower() == "asc" 
                            ? query.OrderBy(g => g.TotalAmount)
                            : query.OrderByDescending(g => g.TotalAmount);
                        break;
                    case "createtime":
                    default:
                        orderedQuery = sortOrder?.ToLower() == "asc"
                            ? query.OrderBy(g => g.CreateTime)
                            : query.OrderByDescending(g => g.CreateTime);
                        break;
                }

                // 获取总数量（用于分页）
                var totalCount = await query.CountAsync();

                // 应用分页
                var orderGroups = await orderedQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取订单组列表成功",
                    data = orderGroups.Select(g => new
                    {
                        g.OrderGroupId,
                        g.GroupNumber,
                        g.CreateTime,
                        g.OrderCount,
                        g.TotalProductAmount,
                        g.ShippingFeeAmount,
                        g.TotalAmount,
                        g.ShippingAddress,
                        g.ContactPhone,
                        g.ReceiverName,
                        OrderIds = g.Orders.Select(o => o.OrderId).ToList()
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
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取订单组详情
        /// </summary>
        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetOrderGroupById(int groupId)
        {
            try
            {
                // 获取订单组详情
                var orderGroup = await _context.OrderGroups
                    .Include(g => g.Orders)
                    .ThenInclude(o => o.Product)
                    .ThenInclude(p => p.Farmer)
                    .FirstOrDefaultAsync(g => g.OrderGroupId == groupId);

                if (orderGroup == null)
                {
                    return NotFound(new { code = 404, message = "订单组不存在" });
                }

                return Ok(new
                {
                    code = 200,
                    message = "获取订单组详情成功",
                    data = new
                    {
                        orderGroup.OrderGroupId,
                        orderGroup.GroupNumber,
                        orderGroup.CreateTime,
                        orderGroup.OrderCount,
                        orderGroup.TotalProductAmount,
                        orderGroup.ShippingFeeAmount,
                        orderGroup.TotalAmount,
                        orderGroup.ShippingAddress,
                        orderGroup.ContactPhone,
                        orderGroup.ReceiverName,
                        Orders = orderGroup.Orders.Select(o => new
                        {
                            o.OrderId,
                            o.ProductId,
                            o.Quantity,
                            o.TotalPrice,
                            o.Status,
                            o.CreateTime,
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
                            }
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取订单组内的所有订单
        /// </summary>
        [HttpGet("{groupId}/orders")]
        public async Task<IActionResult> GetOrdersInGroup(int groupId)
        {
            try
            {
                // 检查订单组是否存在
                var orderGroup = await _context.OrderGroups.FirstOrDefaultAsync(g => g.OrderGroupId == groupId);
                if (orderGroup == null)
                {
                    return NotFound(new { code = 404, message = "订单组不存在" });
                }

                // 获取订单组内的所有订单
                var orders = await _context.Orders
                    .Include(o => o.Product)
                    .ThenInclude(p => p.Farmer)
                    .Where(o => o.OrderGroupId == groupId)
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
                        o.ProductId,
                        o.OrderGroupId,
                        o.Quantity,
                        o.TotalPrice,
                        o.Status,
                        o.CreateTime,
                        o.PayTime,
                        o.ShipTime,
                        o.CompleteTime,
                        o.CancelTime,
                        o.CancelReason,
                        o.ShippingAddress,
                        o.ContactPhone,
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
                        }
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }
} 
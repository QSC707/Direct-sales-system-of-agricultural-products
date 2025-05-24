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
        /// 获取用户的订单组列表
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserOrderGroups(int userId)
        {
            try
            {
                // 检查用户是否存在
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                // 获取用户的订单组，按创建时间倒序排列
                var orderGroups = await _context.OrderGroups
                    .Where(g => g.UserId == userId)
                    .Include(g => g.Orders)
                    .OrderByDescending(g => g.CreateTime)
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
                    })
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
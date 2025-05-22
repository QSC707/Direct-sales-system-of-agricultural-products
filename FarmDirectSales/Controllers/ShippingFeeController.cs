using Microsoft.AspNetCore.Mvc;
using FarmDirectSales.Models;
using FarmDirectSales.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 运费管理控制器
    /// </summary>
    [ApiController]
    [Route("api/shipping-fee")]
    public class ShippingFeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ShippingFeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取所有运费规则
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllShippingFees()
        {
            try
            {
                var fees = await _context.ShippingFees
                    .Include(s => s.DeliveryArea)
                    .OrderByDescending(s => s.Priority)
                    .ThenBy(s => s.CreateTime)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取运费规则列表成功",
                    data = fees
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取单个运费规则
        /// </summary>
        [HttpGet("{feeId}")]
        public async Task<IActionResult> GetShippingFee(int feeId)
        {
            try
            {
                var fee = await _context.ShippingFees
                    .Include(s => s.DeliveryArea)
                    .FirstOrDefaultAsync(s => s.ShippingFeeId == feeId);

                if (fee == null)
                {
                    return NotFound(new { code = 404, message = "运费规则不存在" });
                }

                return Ok(new
                {
                    code = 200,
                    message = "获取运费规则成功",
                    data = fee
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 添加运费规则（仅管理员）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddShippingFee([FromBody] AddShippingFeeRequest request)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 如果关联了配送区域，检查是否存在
                if (request.DeliveryAreaId.HasValue)
                {
                    var area = await _context.DeliveryAreas.FindAsync(request.DeliveryAreaId);
                    if (area == null)
                    {
                        return BadRequest(new { code = 400, message = "指定的配送区域不存在" });
                    }

                    // 检查该配送区域是否已有运费规则
                    var existingRule = await _context.ShippingFees
                        .FirstOrDefaultAsync(s => s.DeliveryAreaId == request.DeliveryAreaId);
                        
                    if (existingRule != null)
                    {
                        return BadRequest(new { code = 400, message = "该配送区域已有运费规则，请编辑现有规则或删除后重新添加" });
                    }
                }

                // 创建新运费规则
                var newFee = new ShippingFee
                {
                    DeliveryAreaId = request.DeliveryAreaId,
                    BaseFee = request.BaseFee,
                    FreeShippingThreshold = request.FreeShippingThreshold,
                    ExtraFeePerKg = request.ExtraFeePerKg,
                    IsEnabled = request.IsEnabled,
                    Priority = request.Priority,
                    Name = request.Name,
                    Description = request.Description,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now,
                    UpdateBy = currentUser.UserId
                };

                _context.ShippingFees.Add(newFee);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "添加运费规则成功",
                    data = newFee
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新运费规则（仅管理员）
        /// </summary>
        [HttpPut("{feeId}")]
        public async Task<IActionResult> UpdateShippingFee(int feeId, [FromBody] UpdateShippingFeeRequest request)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 获取要更新的运费规则
                var fee = await _context.ShippingFees.FindAsync(feeId);
                if (fee == null)
                {
                    return NotFound(new { code = 404, message = "运费规则不存在" });
                }

                // 如果更改了关联的配送区域，检查新区域是否存在
                if (request.DeliveryAreaId.HasValue && request.DeliveryAreaId != fee.DeliveryAreaId)
                {
                    var area = await _context.DeliveryAreas.FindAsync(request.DeliveryAreaId);
                    if (area == null)
                    {
                        return BadRequest(new { code = 400, message = "指定的配送区域不存在" });
                    }

                    // 检查该配送区域是否已有运费规则
                    var existingRule = await _context.ShippingFees
                        .FirstOrDefaultAsync(s => s.DeliveryAreaId == request.DeliveryAreaId && s.ShippingFeeId != feeId);
                        
                    if (existingRule != null)
                    {
                        return BadRequest(new { code = 400, message = "该配送区域已有运费规则，请编辑现有规则或删除后重新添加" });
                    }
                }

                // 更新运费规则
                fee.DeliveryAreaId = request.DeliveryAreaId;
                fee.BaseFee = request.BaseFee;
                fee.FreeShippingThreshold = request.FreeShippingThreshold;
                fee.ExtraFeePerKg = request.ExtraFeePerKg;
                fee.IsEnabled = request.IsEnabled;
                fee.Priority = request.Priority;
                fee.Name = request.Name;
                fee.Description = request.Description;
                fee.UpdateTime = DateTime.Now;
                fee.UpdateBy = currentUser.UserId;

                _context.ShippingFees.Update(fee);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "更新运费规则成功",
                    data = fee
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除运费规则（仅管理员）
        /// </summary>
        [HttpDelete("{feeId}")]
        public async Task<IActionResult> DeleteShippingFee(int feeId)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 获取要删除的运费规则
                var fee = await _context.ShippingFees.FindAsync(feeId);
                if (fee == null)
                {
                    return NotFound(new { code = 404, message = "运费规则不存在" });
                }

                // 检查是否有订单正在使用此运费规则
                var hasOrders = await _context.Orders.AnyAsync(o => o.ShippingFeeId == feeId);
                if (hasOrders)
                {
                    return BadRequest(new { code = 400, message = "该运费规则已被订单使用，无法删除" });
                }

                _context.ShippingFees.Remove(fee);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "删除运费规则成功"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 计算运费
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateShippingFee([FromBody] CalculateShippingFeeRequest request)
        {
            try
            {
                // 初始化变量
                decimal shippingFee = 0;
                ShippingFee? appliedRule = null;
                string message = "免运费";

                // 如果订单金额为0，则无需计算运费
                if (request.OrderAmount <= 0)
                {
                    return Ok(new
                    {
                        code = 200,
                        message = "订单金额为0，无需计算运费",
                        data = new
                        {
                            shippingFee = 0,
                            appliedRule = (object)null,
                            message = "免运费"
                        }
                    });
                }

                // 查找适用的运费规则
                IQueryable<ShippingFee> rulesQuery = _context.ShippingFees.Where(r => r.IsEnabled);

                // 如果提供了配送区域，优先匹配该区域的规则
                if (request.DeliveryAreaId.HasValue)
                {
                    // 尝试查找指定配送区域的规则
                    appliedRule = await rulesQuery
                        .Where(r => r.DeliveryAreaId == request.DeliveryAreaId)
                        .OrderByDescending(r => r.Priority)
                        .FirstOrDefaultAsync();
                }
                
                // 如果没有找到区域特定的规则，查找默认规则（DeliveryAreaId为null的规则）
                if (appliedRule == null)
                {
                    appliedRule = await rulesQuery
                        .Where(r => r.DeliveryAreaId == null)
                        .OrderByDescending(r => r.Priority)
                        .FirstOrDefaultAsync();
                }

                // 如果找到了适用的规则，计算运费
                if (appliedRule != null)
                {
                    // 判断是否超过免运费阈值
                    if (request.OrderAmount >= appliedRule.FreeShippingThreshold)
                    {
                        shippingFee = 0;
                        message = "订单金额已达到免运费标准";
                    }
                    else
                    {
                        // 基础运费
                        shippingFee = appliedRule.BaseFee;
                        
                        // 如果有计重费用且提供了商品重量，则添加重量费用
                        if (appliedRule.ExtraFeePerKg.HasValue && request.Weight.HasValue && request.Weight.Value > 0)
                        {
                            decimal weightFee = appliedRule.ExtraFeePerKg.Value * request.Weight.Value;
                            shippingFee += weightFee;
                            message = $"基础运费¥{appliedRule.BaseFee} + 重量费¥{weightFee:F2}";
                        }
                        else
                        {
                            message = $"基础运费¥{appliedRule.BaseFee}";
                        }
                    }
                }
                else
                {
                    // 没有找到规则，使用默认运费10元
                    shippingFee = 10.0m;
                    message = "使用默认运费";
                }

                return Ok(new
                {
                    code = 200,
                    message = "计算运费成功",
                    data = new
                    {
                        shippingFee,
                        appliedRuleId = appliedRule?.ShippingFeeId,
                        appliedRuleName = appliedRule?.Name,
                        message
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取运费相关的订单统计
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetShippingFeeStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 设置默认时间范围为过去30天
                if (!startDate.HasValue)
                {
                    startDate = DateTime.Now.AddDays(-30);
                }
                
                if (!endDate.HasValue)
                {
                    endDate = DateTime.Now;
                }
                
                // 确保结束日期是当天的最后一刻
                endDate = endDate.Value.Date.AddDays(1).AddSeconds(-1);

                // 查询符合条件的订单
                var orders = await _context.Orders
                    .Where(o => o.CreateTime >= startDate && o.CreateTime <= endDate)
                    .Where(o => o.Status != "canceled" && o.Status != "refunded") // 排除已取消和已退款的订单
                    .ToListAsync();

                // 计算统计数据
                decimal totalShippingFee = orders.Sum(o => o.ShippingFeeAmount);
                decimal totalOrderAmount = orders.Sum(o => o.TotalPrice);
                int totalOrders = orders.Count;
                int freeShippingOrders = orders.Count(o => o.ShippingFeeAmount == 0);
                
                // 计算每个运费规则的使用情况
                var ruleUsage = await _context.Orders
                    .Where(o => o.CreateTime >= startDate && o.CreateTime <= endDate)
                    .Where(o => o.Status != "canceled" && o.Status != "refunded")
                    .Where(o => o.ShippingFeeId.HasValue)
                    .GroupBy(o => o.ShippingFeeId)
                    .Select(g => new
                    {
                        ShippingFeeId = g.Key,
                        OrderCount = g.Count(),
                        TotalShippingFee = g.Sum(o => o.ShippingFeeAmount)
                    })
                    .ToListAsync();

                // 获取规则名称
                var feeIds = ruleUsage.Select(r => r.ShippingFeeId).ToList();
                var rules = await _context.ShippingFees
                    .Where(s => feeIds.Contains(s.ShippingFeeId))
                    .ToDictionaryAsync(s => s.ShippingFeeId, s => s.Name);

                // 构建最终结果
                var ruleStatistics = ruleUsage.Select(r => new
                {
                    shippingFeeId = r.ShippingFeeId,
                    ruleName = rules.TryGetValue(r.ShippingFeeId ?? 0, out var name) ? name : "未知规则",
                    orderCount = r.OrderCount,
                    totalShippingFee = r.TotalShippingFee,
                    averageFee = r.OrderCount > 0 ? r.TotalShippingFee / r.OrderCount : 0
                }).ToList();

                return Ok(new
                {
                    code = 200,
                    message = "获取运费统计数据成功",
                    data = new
                    {
                        timeRange = new
                        {
                            startDate = startDate,
                            endDate = endDate
                        },
                        summary = new
                        {
                            totalOrders,
                            totalShippingFee,
                            totalOrderAmount,
                            shippingFeePercentage = totalOrderAmount > 0 ? (totalShippingFee / totalOrderAmount) * 100 : 0,
                            freeShippingOrders,
                            freeShippingPercentage = totalOrders > 0 ? ((decimal)freeShippingOrders / totalOrders) * 100 : 0,
                            averageShippingFee = totalOrders > 0 ? totalShippingFee / totalOrders : 0
                        },
                        ruleStatistics
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 添加运费规则请求
    /// </summary>
    public class AddShippingFeeRequest
    {
        /// <summary>
        /// 配送区域ID（可空，为空表示默认运费）
        /// </summary>
        public int? DeliveryAreaId { get; set; }

        /// <summary>
        /// 基础运费
        /// </summary>
        [Required]
        [Range(0, 1000, ErrorMessage = "基础运费必须在0-1000之间")]
        public decimal BaseFee { get; set; } = 10.0m;

        /// <summary>
        /// 免运费订单金额（订单金额超过此值免运费）
        /// </summary>
        [Required]
        [Range(0, 10000, ErrorMessage = "免运费金额必须在0-10000之间")]
        public decimal FreeShippingThreshold { get; set; } = 100.0m;

        /// <summary>
        /// 每公斤额外费用（按重量计费时使用）
        /// </summary>
        [Range(0, 100, ErrorMessage = "每公斤额外费用必须在0-100之间")]
        public decimal? ExtraFeePerKg { get; set; }

        /// <summary>
        /// 是否启用（禁用则不使用此运费规则）
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 规则优先级（数字越大优先级越高）
        /// </summary>
        [Range(0, 100, ErrorMessage = "优先级必须在0-100之间")]
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 规则名称
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "规则名称最多50个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 规则描述
        /// </summary>
        [StringLength(200, ErrorMessage = "规则描述最多200个字符")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 更新运费规则请求
    /// </summary>
    public class UpdateShippingFeeRequest
    {
        /// <summary>
        /// 配送区域ID（可空，为空表示默认运费）
        /// </summary>
        public int? DeliveryAreaId { get; set; }

        /// <summary>
        /// 基础运费
        /// </summary>
        [Required]
        [Range(0, 1000, ErrorMessage = "基础运费必须在0-1000之间")]
        public decimal BaseFee { get; set; }

        /// <summary>
        /// 免运费订单金额（订单金额超过此值免运费）
        /// </summary>
        [Required]
        [Range(0, 10000, ErrorMessage = "免运费金额必须在0-10000之间")]
        public decimal FreeShippingThreshold { get; set; }

        /// <summary>
        /// 每公斤额外费用（按重量计费时使用）
        /// </summary>
        [Range(0, 100, ErrorMessage = "每公斤额外费用必须在0-100之间")]
        public decimal? ExtraFeePerKg { get; set; }

        /// <summary>
        /// 是否启用（禁用则不使用此运费规则）
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 规则优先级（数字越大优先级越高）
        /// </summary>
        [Range(0, 100, ErrorMessage = "优先级必须在0-100之间")]
        public int Priority { get; set; }

        /// <summary>
        /// 规则名称
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "规则名称最多50个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 规则描述
        /// </summary>
        [StringLength(200, ErrorMessage = "规则描述最多200个字符")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 计算运费请求
    /// </summary>
    public class CalculateShippingFeeRequest
    {
        /// <summary>
        /// 订单金额
        /// </summary>
        [Required]
        [Range(0, 100000, ErrorMessage = "订单金额必须在0-100000之间")]
        public decimal OrderAmount { get; set; }

        /// <summary>
        /// 配送区域ID
        /// </summary>
        public int? DeliveryAreaId { get; set; }

        /// <summary>
        /// 商品总重量（公斤）
        /// </summary>
        [Range(0, 1000, ErrorMessage = "商品重量必须在0-1000之间")]
        public decimal? Weight { get; set; }
    }
} 
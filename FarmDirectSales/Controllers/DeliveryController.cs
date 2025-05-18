using Microsoft.AspNetCore.Mvc;
using FarmDirectSales.Models;
using FarmDirectSales.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 配送服务控制器
    /// </summary>
    [ApiController]
    [Route("api/delivery")]
    public class DeliveryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DeliveryController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取支持当天配送的区域
        /// </summary>
        [HttpGet("sameday-delivery-areas")]
        public async Task<IActionResult> GetSameDayDeliveryAreas()
        {
            try
            {
                var areas = await _context.DeliveryAreas
                    .Where(a => a.SupportSameDayDelivery)
                    .OrderBy(a => a.Province)
                    .ThenBy(a => a.City)
                    .ThenBy(a => a.District)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取当天配送区域成功",
                    data = areas.Select(a => new {
                        a.Province,
                        a.City,
                        a.District,
                        a.SupportSameDayDelivery,
                        a.DeliveryFee
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取支持当天配送的区域 (用于兼容前端代码的API调用)
        /// </summary>
        [HttpGet("sameday-areas")]
        public async Task<IActionResult> GetSameDayAreas()
        {
            try
            {
                var areas = await _context.DeliveryAreas
                    .Where(a => a.SupportSameDayDelivery)
                    .OrderBy(a => a.Province)
                    .ThenBy(a => a.City)
                    .ThenBy(a => a.District)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取当天配送区域成功",
                    data = areas.Select(a => new {
                        a.Province,
                        a.City,
                        a.District,
                        a.SupportSameDayDelivery,
                        a.DeliveryFee
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 检查地址是否支持当天配送
        /// </summary>
        [HttpPost("check-sameday")]
        public async Task<IActionResult> CheckSameDayDelivery([FromBody] CheckSameDayDeliveryRequest request)
        {
            try
            {
                // 查找匹配的配送区域
                var area = await _context.DeliveryAreas
                    .Where(a => a.Province == request.Province && 
                           a.City == request.City)
                    .FirstOrDefaultAsync();

                bool isSameDayAvailable = false;
                decimal deliveryFee = 0;

                if (area != null && area.SupportSameDayDelivery)
                {
                    isSameDayAvailable = true;
                    deliveryFee = area.DeliveryFee;
                }

                return Ok(new
                {
                    code = 200,
                    message = isSameDayAvailable ? "该地区支持当天配送" : "该地区不支持当天配送",
                    data = new 
                    {
                        isSameDayAvailable,
                        deliveryFee,
                        estimatedTime = isSameDayAvailable ? "今天18:00前" : "2-3个工作日"
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取配送费用
        /// </summary>
        [HttpPost("fee")]
        public async Task<IActionResult> GetDeliveryFee([FromBody] GetDeliveryFeeRequest request)
        {
            try
            {
                // 查找匹配的配送区域
                var area = await _context.DeliveryAreas
                    .Where(a => a.Province == request.Province && 
                           a.City == request.City)
                    .FirstOrDefaultAsync();

                decimal fee = 0;
                string message = "免配送费";

                if (area != null)
                {
                    fee = area.DeliveryFee;
                    if (fee > 0)
                    {
                        message = $"配送费: ¥{fee}";
                    }
                }
                else
                {
                    // 如果没有找到配送区域记录，设置默认配送费
                    fee = request.DeliveryMethod == "express" ? 10 : 15;
                    message = $"配送费: ¥{fee}";
                }

                return Ok(new
                {
                    code = 200,
                    message = message,
                    data = new 
                    {
                        fee,
                        freeThreshold = 99 // 满99元免配送费
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取预计送达时间
        /// </summary>
        [HttpPost("estimated-time")]
        public async Task<IActionResult> GetEstimatedDeliveryTime([FromBody] GetEstimatedTimeRequest request)
        {
            try
            {
                // 查找匹配的配送区域
                var area = await _context.DeliveryAreas
                    .Where(a => a.Province == request.Province && 
                           a.City == request.City)
                    .FirstOrDefaultAsync();

                string estimatedTime = "2-3个工作日";
                
                if (area != null && area.SupportSameDayDelivery && request.DeliveryMethod == "express")
                {
                    // 判断当前时间，如果在15点前下单，当天送达
                    var now = DateTime.Now;
                    if (now.Hour < 15)
                    {
                        estimatedTime = "今天18:00前";
                    }
                    else
                    {
                        estimatedTime = "明天12:00前";
                    }
                }
                else if (request.DeliveryMethod == "express")
                {
                    estimatedTime = "1-2个工作日";
                }

                return Ok(new
                {
                    code = 200,
                    message = "获取预计送达时间成功",
                    data = new 
                    {
                        estimatedTime,
                        method = request.DeliveryMethod == "express" ? "快递配送" : "普通配送"
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }

    public class CheckSameDayDeliveryRequest
    {
        [Required(ErrorMessage = "省份不能为空")]
        public string Province { get; set; }

        [Required(ErrorMessage = "城市不能为空")]
        public string City { get; set; }
    }

    public class GetDeliveryFeeRequest
    {
        [Required(ErrorMessage = "省份不能为空")]
        public string Province { get; set; }

        [Required(ErrorMessage = "城市不能为空")]
        public string City { get; set; }

        [Required(ErrorMessage = "配送方式不能为空")]
        public string DeliveryMethod { get; set; } = "express"; // express or standard
    }

    public class GetEstimatedTimeRequest
    {
        [Required(ErrorMessage = "省份不能为空")]
        public string Province { get; set; }

        [Required(ErrorMessage = "城市不能为空")]
        public string City { get; set; }

        [Required(ErrorMessage = "配送方式不能为空")]
        public string DeliveryMethod { get; set; } = "express"; // express or standard
    }
} 
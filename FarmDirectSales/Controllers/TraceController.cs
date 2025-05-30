using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Data;
using FarmDirectSales.Models;
using System.ComponentModel.DataAnnotations;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 溯源信息控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TraceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TraceController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取产品的溯源信息
        /// </summary>
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductTraces(int productId)
        {
            try
            {
                // 检查产品是否存在
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                var traces = await _context.Traces
                    .Where(t => t.ProductId == productId)
                    .OrderByDescending(t => t.CreateTime)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取产品溯源信息成功",
                    data = traces.Select(t => new
                    {
                        t.TraceId,
                        t.ProductId,
                        t.SourcePlace,
                        t.PlantingMethod,
                        t.PlantingTime,
                        t.HarvestTime,
                        t.QualityLevel,
                        t.IsOrganic,
                        t.AdditionalInfo,
                        t.CreateTime,
                        t.UpdateTime
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取溯源信息详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrace(int id)
        {
            try
            {
                var trace = await _context.Traces
                    .Include(t => t.Product)
                    .FirstOrDefaultAsync(t => t.TraceId == id);

                if (trace == null)
                {
                    return NotFound(new { code = 404, message = "溯源信息不存在" });
                }

                return Ok(new
                {
                    code = 200,
                    message = "获取溯源信息成功",
                    data = new
                    {
                        trace.TraceId,
                        trace.ProductId,
                        Product = new
                        {
                            trace.Product.ProductId,
                            trace.Product.ProductName,
                            trace.Product.Description
                        },
                        trace.SourcePlace,
                        trace.PlantingMethod,
                        trace.PlantingTime,
                        trace.HarvestTime,
                        trace.QualityLevel,
                        trace.IsOrganic,
                        trace.AdditionalInfo,
                        trace.CreateTime,
                        trace.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 添加溯源信息
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddTrace([FromBody] AddTraceRequest request)
        {
            try
            {
                // 检查产品是否存在
                var product = await _context.Products
                    .Include(p => p.Farmer)
                    .FirstOrDefaultAsync(p => p.ProductId == request.ProductId && p.IsActive);

                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 检查是否是产品所有者
                if (product.FarmerId != request.FarmerId)
                {
                    return BadRequest(new { code = 400, message = "只有产品所有者才能添加溯源信息" });
                }

                var trace = new Trace
                {
                    ProductId = request.ProductId,
                    SourcePlace = request.SourcePlace,
                    PlantingMethod = request.PlantingMethod,
                    PlantingTime = request.PlantingTime,
                    HarvestTime = request.HarvestTime,
                    QualityLevel = request.QualityLevel,
                    IsOrganic = request.IsOrganic,
                    AdditionalInfo = request.AdditionalInfo ?? string.Empty,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                _context.Traces.Add(trace);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "添加溯源信息成功",
                    data = new
                    {
                        trace.TraceId,
                        trace.ProductId,
                        trace.SourcePlace,
                        trace.PlantingMethod,
                        trace.PlantingTime,
                        trace.HarvestTime,
                        trace.QualityLevel,
                        trace.IsOrganic,
                        trace.AdditionalInfo,
                        trace.CreateTime,
                        trace.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新溯源信息
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTrace(int id, [FromBody] UpdateTraceRequest request)
        {
            try
            {
                var trace = await _context.Traces
                    .Include(t => t.Product)
                    .FirstOrDefaultAsync(t => t.TraceId == id);

                if (trace == null)
                {
                    return NotFound(new { code = 404, message = "溯源信息不存在" });
                }

                // 检查是否是产品所有者
                if (trace.Product?.FarmerId != request.FarmerId)
                {
                    return BadRequest(new { code = 400, message = "只有产品所有者才能更新溯源信息" });
                }

                // 更新溯源信息
                trace.SourcePlace = request.SourcePlace ?? trace.SourcePlace;
                trace.PlantingMethod = request.PlantingMethod ?? trace.PlantingMethod;
                trace.PlantingTime = request.PlantingTime ?? trace.PlantingTime;
                trace.HarvestTime = request.HarvestTime ?? trace.HarvestTime;
                trace.QualityLevel = request.QualityLevel ?? trace.QualityLevel;
                trace.IsOrganic = request.IsOrganic ?? trace.IsOrganic;
                trace.AdditionalInfo = request.AdditionalInfo ?? trace.AdditionalInfo;
                trace.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "更新溯源信息成功",
                    data = new
                    {
                        trace.TraceId,
                        trace.ProductId,
                        trace.SourcePlace,
                        trace.PlantingMethod,
                        trace.PlantingTime,
                        trace.HarvestTime,
                        trace.QualityLevel,
                        trace.IsOrganic,
                        trace.AdditionalInfo,
                        trace.CreateTime,
                        trace.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除溯源信息
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrace(int id, [FromBody] DeleteTraceRequest request)
        {
            try
            {
                var trace = await _context.Traces
                    .Include(t => t.Product)
                    .FirstOrDefaultAsync(t => t.TraceId == id);

                if (trace == null)
                {
                    return NotFound(new { code = 404, message = "溯源信息不存在" });
                }

                // 检查是否是产品所有者
                if (trace.Product?.FarmerId != request.FarmerId)
                {
                    return BadRequest(new { code = 400, message = "只有产品所有者才能删除溯源信息" });
                }

                _context.Traces.Remove(trace);
                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "删除溯源信息成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 添加溯源信息请求模型
    /// </summary>
    public class AddTraceRequest
    {
        /// <summary>
        /// 产品ID
        /// </summary>
        [Required(ErrorMessage = "产品ID不能为空")]
        public int ProductId { get; set; }

        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }

        /// <summary>
        /// 产地信息
        /// </summary>
        [Required(ErrorMessage = "产地信息不能为空")]
        public string SourcePlace { get; set; } = string.Empty;

        /// <summary>
        /// 种植/养殖方式
        /// </summary>
        public string PlantingMethod { get; set; } = string.Empty;

        /// <summary>
        /// 种植/养殖时间
        /// </summary>
        public DateTime PlantingTime { get; set; }

        /// <summary>
        /// 收获时间
        /// </summary>
        public DateTime HarvestTime { get; set; }

        /// <summary>
        /// 质量等级
        /// </summary>
        public string QualityLevel { get; set; } = string.Empty;

        /// <summary>
        /// 是否有机认证
        /// </summary>
        public bool IsOrganic { get; set; }

        /// <summary>
        /// 附加信息
        /// </summary>
        public string? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// 更新溯源信息请求模型
    /// </summary>
    public class UpdateTraceRequest
    {
        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }

        /// <summary>
        /// 产地信息
        /// </summary>
        public string? SourcePlace { get; set; }

        /// <summary>
        /// 种植/养殖方式
        /// </summary>
        public string? PlantingMethod { get; set; }

        /// <summary>
        /// 种植/养殖时间
        /// </summary>
        public DateTime? PlantingTime { get; set; }

        /// <summary>
        /// 收获时间
        /// </summary>
        public DateTime? HarvestTime { get; set; }

        /// <summary>
        /// 质量等级
        /// </summary>
        public string? QualityLevel { get; set; }

        /// <summary>
        /// 是否有机认证
        /// </summary>
        public bool? IsOrganic { get; set; }

        /// <summary>
        /// 附加信息
        /// </summary>
        public string? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// 删除溯源信息请求模型
    /// </summary>
    public class DeleteTraceRequest
    {
        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }
    }
} 
 
 
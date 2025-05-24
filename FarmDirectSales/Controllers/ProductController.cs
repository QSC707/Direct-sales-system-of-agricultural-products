using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Data;
using FarmDirectSales.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Linq;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 产品管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取所有产品
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int? farmerId = null,
            [FromQuery] string? category = null,
            [FromQuery] string? status = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = "desc",
            [FromQuery] string? keyword = null)
        {
            try
            {
                // 检查用户身份和权限
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                
                // 添加详细调试日志
                Console.WriteLine($"=== GetProducts 开始调试 ===");
                Console.WriteLine($"用户角色: '{userRole}', 用户ID: '{userId}', 农户ID参数: '{farmerId}'");
                Console.WriteLine($"筛选参数 - 分类: '{category}', 状态: '{status}', 排序字段: '{sortBy}', 排序顺序: '{sortOrder}', 关键词: '{keyword}'");
                
                // 构建基础查询，不包括IsActive过滤
                IQueryable<Product> query = _context.Products.Include(p => p.Farmer);
                
                // 判断查询条件
                if (farmerId.HasValue)
                {
                    // 如果指定了farmerId，查询该农户的所有产品（包括下架的）
                    query = query.Where(p => p.FarmerId == farmerId.Value);
                    Console.WriteLine($"应用农户筛选: farmerId = {farmerId.Value}");
                }
                else if (userRole == "farmer" && int.TryParse(userId, out int currentFarmerId))
                {
                    // 如果是农户查看自己的产品，不过滤IsActive
                    query = query.Where(p => p.FarmerId == currentFarmerId);
                    Console.WriteLine($"应用当前农户筛选: currentFarmerId = {currentFarmerId}");
                }
                else
                {
                    // 其他情况（管理员或普通用户）只返回上架(IsActive)的产品
                    query = query.Where(p => p.IsActive);
                    Console.WriteLine("应用默认筛选: 只返回上架产品");
                }
                
                // 分类筛选
                if (!string.IsNullOrEmpty(category) && category != "all")
                {
                    query = query.Where(p => p.Category == category);
                    Console.WriteLine($"应用分类筛选: category = '{category}'");
                }
                else
                {
                    Console.WriteLine("跳过分类筛选");
                }
                
                // 状态筛选
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    if (status == "active")
                    {
                        query = query.Where(p => p.IsActive);
                        Console.WriteLine("应用状态筛选: 只返回上架产品");
                    }
                    else if (status == "inactive")
                    {
                        query = query.Where(p => !p.IsActive);
                        Console.WriteLine("应用状态筛选: 只返回下架产品");
                    }
                }
                else
                {
                    Console.WriteLine("跳过状态筛选");
                }
                
                // 关键词搜索
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(p => p.ProductName.Contains(keyword) || 
                                           p.Description.Contains(keyword));
                    Console.WriteLine($"应用关键词搜索: keyword = '{keyword}'");
                }
                else
                {
                    Console.WriteLine("跳过关键词搜索");
                }
                
                // 排序
                if (!string.IsNullOrEmpty(sortBy))
                {
                    bool isDescending = sortOrder?.ToLower() == "desc";
                    
                    query = sortBy.ToLower() switch
                    {
                        "name" => isDescending ? query.OrderByDescending(p => p.ProductName) : query.OrderBy(p => p.ProductName),
                        "price" => isDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                        "stock" => isDescending ? query.OrderByDescending(p => p.Stock) : query.OrderBy(p => p.Stock),
                        "createtime" => isDescending ? query.OrderByDescending(p => p.CreateTime) : query.OrderBy(p => p.CreateTime),
                        "updatetime" => isDescending ? query.OrderByDescending(p => p.UpdateTime) : query.OrderBy(p => p.UpdateTime),
                        "category" => isDescending ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
                        _ => query.OrderByDescending(p => p.CreateTime) // 默认按创建时间降序
                    };
                    Console.WriteLine($"应用排序: {sortBy} {sortOrder}");
                }
                else
                {
                    // 默认排序：按创建时间降序
                    query = query.OrderByDescending(p => p.CreateTime);
                    Console.WriteLine("应用默认排序: 按创建时间降序");
                }
                
                var products = await query.ToListAsync();
                Console.WriteLine($"查询结果: 找到 {products.Count} 个产品");
                
                // 打印每个产品的基本信息用于调试
                foreach (var p in products)
                {
                    Console.WriteLine($"  产品: {p.ProductName}, 分类: {p.Category}, 状态: {(p.IsActive ? "上架" : "下架")}");
                }
                Console.WriteLine($"=== GetProducts 调试结束 ===");

                return Ok(new
                {
                    code = 200,
                    message = "获取产品列表成功",
                    data = products.Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Description,
                        p.Price,
                        p.Stock,
                        p.ImageUrl,
                        p.Category,
                        p.Specification,
                        p.IsOrganic,
                        p.HarvestDate,
                        p.ShelfLife,
                        Farmer = new { p.Farmer.UserId, p.Farmer.Username },
                        p.CreateTime,
                        p.UpdateTime,
                        p.IsActive,
                        p.ActiveTime,
                        p.InactiveTime
                    })
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetProducts错误: {ex.Message}");
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取产品详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Farmer)
                    .Include(p => p.Farmer.FarmerProfile)
                    .Include(p => p.Traces) // 包含溯源信息
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 获取最新的溯源信息
                var traceInfo = product.Traces?.OrderByDescending(t => t.CreateTime).FirstOrDefault();

                return Ok(new
                {
                    code = 200,
                    message = "获取产品详情成功",
                    data = new
                    {
                        product.ProductId,
                        product.ProductName,
                        product.Description,
                        product.Price,
                        product.Stock,
                        product.ImageUrl,
                        product.Category,
                        product.IsActive,
                        product.ActiveTime,
                        product.InactiveTime,
                        // 添加规格信息
                        product.Specification,
                        // 添加其他农户填写的信息
                        product.IsOrganic,
                        product.HarvestDate,
                        product.ShelfLife,
                        Farmer = new { 
                            product.Farmer.UserId, 
                            product.Farmer.Username,
                            product.Farmer.Phone,
                            Location = product.Farmer.FarmerProfile != null ? product.Farmer.FarmerProfile.Location : "",
                            Description = product.Farmer.FarmerProfile != null ? product.Farmer.FarmerProfile.Description : "",
                            product.Farmer.CreateTime
                        },
                        product.CreateTime,
                        product.UpdateTime,
                        // 添加溯源信息
                        TraceInfo = traceInfo == null ? null : new
                        {
                            traceInfo.TraceId,
                            traceInfo.SourcePlace,
                            traceInfo.PlantingMethod,
                            traceInfo.PlantingTime,
                            traceInfo.HarvestTime,
                            traceInfo.QualityLevel,
                            traceInfo.IsOrganic,
                            traceInfo.AdditionalInfo,
                            traceInfo.CreateTime,
                            traceInfo.UpdateTime
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 添加产品
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] AddProductRequest request)
        {
            try
            {
                // 检查用户是否存在且是农户
                var farmer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.FarmerId);
                if (farmer == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                if (farmer.Role != "farmer")
                {
                    return BadRequest(new { code = 400, message = "只有农户才能添加产品" });
                }

                var product = new Product
                {
                    ProductName = request.ProductName,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    ImageUrl = request.ImageUrl,
                    FarmerId = request.FarmerId,
                    Category = request.Category ?? "其他",
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now,
                    IsActive = true,
                    ActiveTime = DateTime.Now,
                    // 添加新字段信息
                    Specification = request.Specification ?? "",
                    IsOrganic = request.IsOrganic ?? false,
                    HarvestDate = request.HarvestDate,
                    ShelfLife = request.ShelfLife
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // 如果提供了溯源信息，则添加溯源记录
                if (!string.IsNullOrEmpty(request.SourcePlace))
                {
                    var trace = new Trace
                    {
                        ProductId = product.ProductId,
                        SourcePlace = request.SourcePlace,
                        PlantingMethod = request.PlantingMethod ?? "",
                        PlantingTime = request.PlantingTime ?? DateTime.Now,
                        HarvestTime = request.HarvestDate ?? DateTime.Now,
                        QualityLevel = request.QualityLevel ?? "标准",
                        IsOrganic = request.IsOrganic ?? false,
                        AdditionalInfo = request.TraceInfo ?? "",
                        CreateTime = DateTime.Now
                    };

                    _context.Traces.Add(trace);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    code = 200,
                    message = "添加产品成功",
                    data = new
                    {
                        product.ProductId,
                        product.ProductName,
                        product.Description,
                        product.Price,
                        product.Stock,
                        product.ImageUrl,
                        product.Category,
                        product.Specification,
                        product.IsOrganic,
                        product.HarvestDate,
                        product.ShelfLife,
                        product.FarmerId,
                        product.CreateTime,
                        product.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新产品
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                // 检查产品是否存在
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 检查是否有权限修改（只有该产品的农户和管理员可以修改）
                int farmerId = request.FarmerId;
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userRole != "admin" && (product.FarmerId != farmerId || userId != farmerId.ToString()))
                {
                    return Unauthorized(new { code = 401, message = "无权修改该产品" });
                }

                // 更新产品信息
                if (request.ProductName != null)
                    product.ProductName = request.ProductName;
                if (request.Description != null)
                    product.Description = request.Description;
                if (request.Price.HasValue)
                    product.Price = request.Price.Value;
                if (request.Stock.HasValue)
                    product.Stock = request.Stock.Value;
                if (request.ImageUrl != null)
                    product.ImageUrl = request.ImageUrl;
                if (request.Category != null)
                    product.Category = request.Category;
                
                // 更新新增字段
                if (request.Specification != null)
                    product.Specification = request.Specification;
                if (request.IsOrganic.HasValue)
                    product.IsOrganic = request.IsOrganic.Value;
                if (request.HarvestDate.HasValue)
                    product.HarvestDate = request.HarvestDate.Value;
                if (request.ShelfLife.HasValue)
                    product.ShelfLife = request.ShelfLife.Value;
                
                // 处理上下架状态
                if (request.IsActive.HasValue && product.IsActive != request.IsActive.Value)
                {
                    product.IsActive = request.IsActive.Value;
                    if (request.IsActive.Value)
                    {
                            product.ActiveTime = DateTime.Now;
                        }
                        else
                        {
                            product.InactiveTime = DateTime.Now;
                    }
                }

                product.UpdateTime = DateTime.Now;

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                // 如果提供了溯源信息，则更新溯源记录
                if (!string.IsNullOrEmpty(request.SourcePlace))
                {
                    var trace = new Trace
                    {
                        ProductId = product.ProductId,
                        SourcePlace = request.SourcePlace,
                        PlantingMethod = request.PlantingMethod ?? "",
                        PlantingTime = request.PlantingTime ?? DateTime.Now,
                        HarvestTime = request.HarvestDate ?? DateTime.Now,
                        QualityLevel = request.QualityLevel ?? "标准",
                        IsOrganic = request.IsOrganic ?? false,
                        AdditionalInfo = request.TraceInfo ?? "",
                        CreateTime = DateTime.Now
                    };

                    _context.Traces.Add(trace);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    code = 200,
                    message = "更新产品成功",
                    data = new
                    {
                        product.ProductId,
                        product.ProductName,
                        product.Description,
                        product.Price,
                        product.Stock,
                        product.ImageUrl,
                        product.Category,
                        product.Specification,
                        product.IsOrganic,
                        product.HarvestDate,
                        product.ShelfLife,
                        product.FarmerId,
                        product.CreateTime,
                        product.UpdateTime,
                        product.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取产品销量
        /// </summary>
        [HttpGet("{id}/sales")]
        public async Task<IActionResult> GetProductSales(int id)
        {
            try
            {
                // 验证产品是否存在
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 记录日志
                Console.WriteLine($"正在获取产品ID={id}的销量数据");

                // 查询已完成的订单中该产品的总销量
                var totalSales = await _context.Orders
                    .Where(o => o.ProductId == id && o.Status == "已完成")
                    .SumAsync(o => o.Quantity);

                Console.WriteLine($"产品ID={id}的销量数据: {totalSales}");

                // 即使销量为0也返回有效响应
                return Ok(new
                {
                    code = 200,
                    message = "获取产品销量成功",
                    data = totalSales
                });
            }
            catch (Exception ex)
            {
                // 记录详细错误信息
                Console.WriteLine($"获取产品销量时发生错误: {ex.Message}");
                Console.WriteLine($"错误详情: {ex}");
                
                // 返回错误响应
                return BadRequest(new { 
                    code = 400, 
                    message = $"获取产品销量失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 删除产品（智能判断是否可以硬删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id, [FromBody] DeleteProductRequest request)
        {
            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 检查是否是产品所有者
                if (product.FarmerId != request.FarmerId)
                {
                    return BadRequest(new { code = 400, message = "只有产品所有者才能删除产品" });
                }

                // 检查产品是否被订单引用
                bool hasOrderReferences = await _context.Orders.AnyAsync(o => o.ProductId == id);

                if (hasOrderReferences)
                {
                    // 如果产品已被订单引用，只能软删除
                    product.IsActive = false;
                    product.UpdateTime = DateTime.Now;
                    product.InactiveTime = DateTime.Now;

                    await _context.SaveChangesAsync();
                    return Ok(new { 
                        code = 200, 
                        message = "产品已下架但无法永久删除，因为该产品已被订单引用",
                        isHardDeleted = false
                    });
                }
                else if (request.HardDelete)
                {
                    // 硬删除 - 从数据库中完全删除
                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                    return Ok(new { 
                        code = 200, 
                        message = "产品已永久删除",
                        isHardDeleted = true
                    });
                }
                else
                {
                    // 软删除
                    product.IsActive = false;
                    product.UpdateTime = DateTime.Now;
                    product.InactiveTime = DateTime.Now;

                    await _context.SaveChangesAsync();
                    return Ok(new { 
                        code = 200, 
                        message = "产品已下架",
                        isHardDeleted = false
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除产品时发生错误: {ex}");
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 批量导出产品
        /// </summary>
        [HttpGet("export")]
        [Authorize(Roles = "farmer,admin")]
        public async Task<IActionResult> ExportProducts([FromQuery] int? farmerId)
        {
            try
            {
                IQueryable<Product> query = _context.Products.Include(p => p.Farmer);

                // 如果是农户用户，只能导出自己的产品
                if (farmerId.HasValue)
                {
                    query = query.Where(p => p.FarmerId == farmerId.Value);
                }

                var products = await query.ToListAsync();

                if (products.Count == 0)
                {
                    return NotFound(new { code = 404, message = "没有找到产品数据" });
                }

                // 创建CSV内容
                var csv = new System.Text.StringBuilder();
                
                // 添加CSV标题行
                csv.AppendLine("产品ID,产品名称,类别,描述,价格,库存,农户ID,农户名称,创建时间,上次更新时间,是否上架,图片URL");
                
                // 添加产品数据行
                foreach (var product in products)
                {
                    csv.AppendLine($"{product.ProductId},{EscapeCsvField(product.ProductName)},{EscapeCsvField(product.Category)},{EscapeCsvField(product.Description)},{product.Price},{product.Stock},{product.FarmerId},{EscapeCsvField(product.Farmer?.Username ?? "未知")},{product.CreateTime},{product.UpdateTime},{product.IsActive},{EscapeCsvField(product.ImageUrl)}");
                }

                // 设置文件名
                string fileName = farmerId.HasValue ? $"products_farmer_{farmerId}.csv" : "all_products.csv";
                
                // 返回CSV文件
                return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 批量导入产品
        /// </summary>
        [HttpPost("import")]
        [Authorize(Roles = "farmer")]
        public async Task<IActionResult> ImportProducts(IFormFile file, [FromQuery] int farmerId)
        {
            try
            {
                // 检查文件
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { code = 400, message = "请上传CSV文件" });
                }

                if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { code = 400, message = "请上传CSV格式的文件" });
                }

                // 检查农户是否存在
                var farmer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == farmerId && u.Role == "farmer");
                if (farmer == null)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 读取CSV文件
                List<Product> productsToAdd = new List<Product>();
                List<string> errors = new List<string>();
                int lineNumber = 0;

                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    string line;
                    // 跳过标题行
                    reader.ReadLine();
                    lineNumber++;

                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNumber++;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var fields = ParseCsvLine(line);
                            
                            // 确保有足够的字段
                            if (fields.Length < 5)
                            {
                                errors.Add($"第{lineNumber}行: 字段不足");
                                continue;
                            }

                            // 解析产品数据
                            var product = new Product
                            {
                                ProductName = fields[0],
                                Category = fields[1],
                                Description = fields[2],
                                Price = decimal.Parse(fields[3]),
                                Stock = int.Parse(fields[4]),
                                FarmerId = farmerId,
                                CreateTime = DateTime.Now,
                                UpdateTime = DateTime.Now,
                                IsActive = true,
                                ImageUrl = fields.Length > 5 ? fields[5] : null
                            };

                            // 验证数据
                            if (string.IsNullOrWhiteSpace(product.ProductName))
                            {
                                errors.Add($"第{lineNumber}行: 产品名称不能为空");
                                continue;
                            }

                            if (product.Price <= 0)
                            {
                                errors.Add($"第{lineNumber}行: 价格必须大于0");
                                continue;
                            }

                            if (product.Stock < 0)
                            {
                                errors.Add($"第{lineNumber}行: 库存不能为负数");
                                continue;
                            }

                            productsToAdd.Add(product);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"第{lineNumber}行: {ex.Message}");
                        }
                    }
                }

                // 如果有错误，返回错误信息
                if (errors.Count > 0)
                {
                    return BadRequest(new { code = 400, message = "导入数据有错误", errors });
                }

                // 如果没有有效产品，返回错误
                if (productsToAdd.Count == 0)
                {
                    return BadRequest(new { code = 400, message = "没有有效的产品数据" });
                }

                // 添加产品到数据库
                _context.Products.AddRange(productsToAdd);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = $"成功导入{productsToAdd.Count}个产品",
                    data = productsToAdd.Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Category,
                        p.Price,
                        p.Stock,
                        p.FarmerId,
                        p.CreateTime,
                        p.UpdateTime
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        // 辅助方法：处理CSV字段转义
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            if (field.Contains(',') || field.Contains('\"') || field.Contains('\n'))
            {
                // 将双引号转义为两个双引号
                field = field.Replace("\"", "\"\"");
                // 将整个字段包含在双引号中
                return $"\"{field}\"";
            }

            return field;
        }

        // 辅助方法：解析CSV行
        private string[] ParseCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            int startIndex = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(line.Substring(startIndex, i - startIndex).Trim('"').Replace("\"\"", "\""));
                    startIndex = i + 1;
                }
            }

            // 添加最后一个字段
            result.Add(line.Substring(startIndex).Trim('"').Replace("\"\"", "\""));

            return result.ToArray();
        }
    }

    /// <summary>
    /// 添加产品请求模型
    /// </summary>
    public class AddProductRequest
    {
        /// <summary>
        /// 产品名称
        /// </summary>
        [Required(ErrorMessage = "产品名称不能为空")]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 产品描述
        /// </summary>
        [Required(ErrorMessage = "产品描述不能为空")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 产品价格
        /// </summary>
        [Required(ErrorMessage = "产品价格不能为空")]
        [Range(0.01, double.MaxValue, ErrorMessage = "价格必须大于0")]
        public decimal Price { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        [Required(ErrorMessage = "库存数量不能为空")]
        [Range(0, int.MaxValue, ErrorMessage = "库存不能为负数")]
        public int Stock { get; set; }

        /// <summary>
        /// 产品图片URL
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }

        /// <summary>
        /// 产品类别
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 规格
        /// </summary>
        public string? Specification { get; set; }

        /// <summary>
        /// 是否有机
        /// </summary>
        public bool? IsOrganic { get; set; }

        /// <summary>
        /// 收获日期
        /// </summary>
        public DateTime? HarvestDate { get; set; }

        /// <summary>
        /// 保质期
        /// </summary>
        public int? ShelfLife { get; set; }

        /// <summary>
        /// 溯源来源
        /// </summary>
        public string? SourcePlace { get; set; }

        /// <summary>
        /// 种植方法
        /// </summary>
        public string? PlantingMethod { get; set; }

        /// <summary>
        /// 种植时间
        /// </summary>
        public DateTime? PlantingTime { get; set; }

        /// <summary>
        /// 质量等级
        /// </summary>
        public string? QualityLevel { get; set; }

        /// <summary>
        /// 附加信息
        /// </summary>
        public string? TraceInfo { get; set; }
    }

    /// <summary>
    /// 更新产品请求模型
    /// </summary>
    public class UpdateProductRequest
    {
        /// <summary>
        /// 产品名称
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 价格
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "价格必须大于0")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 库存
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "库存不能为负数")]
        public int? Stock { get; set; }

        /// <summary>
        /// 图片URL
        /// </summary>
        public string? ImageUrl { get; set; }
        
        /// <summary>
        /// 分类
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }
        
        /// <summary>
        /// 是否上架
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// 规格
        /// </summary>
        public string? Specification { get; set; }

        /// <summary>
        /// 是否有机
        /// </summary>
        public bool? IsOrganic { get; set; }

        /// <summary>
        /// 收获日期
        /// </summary>
        public DateTime? HarvestDate { get; set; }

        /// <summary>
        /// 保质期
        /// </summary>
        public int? ShelfLife { get; set; }

        /// <summary>
        /// 溯源来源
        /// </summary>
        public string? SourcePlace { get; set; }

        /// <summary>
        /// 种植方法
        /// </summary>
        public string? PlantingMethod { get; set; }

        /// <summary>
        /// 种植时间
        /// </summary>
        public DateTime? PlantingTime { get; set; }

        /// <summary>
        /// 质量等级
        /// </summary>
        public string? QualityLevel { get; set; }

        /// <summary>
        /// 附加信息
        /// </summary>
        public string? TraceInfo { get; set; }
    }

    /// <summary>
    /// 删除产品请求模型
    /// </summary>
    public class DeleteProductRequest
    {
        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }

        /// <summary>
        /// 是否硬删除
        /// </summary>
        public bool HardDelete { get; set; } = false;
    }
} 
 
 
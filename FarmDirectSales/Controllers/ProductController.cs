using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Data;
using FarmDirectSales.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

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
        public async Task<IActionResult> GetProducts([FromQuery] int? farmerId = null)
        {
            try
            {
                // 检查用户身份和权限
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                
                // 构建查询
                var query = _context.Products
                    .Include(p => p.Farmer)
                    .Where(p => p.IsActive);
                
                // 如果请求中指定了farmerId参数，只返回该农户的产品
                if (farmerId.HasValue)
                {
                    query = query.Where(p => p.FarmerId == farmerId.Value);
                }
                // 如果是农户角色且没有指定farmerId，则只返回自己的产品
                else if (userRole == "farmer" && int.TryParse(userId, out int currentFarmerId))
                {
                    query = query.Where(p => p.FarmerId == currentFarmerId);
                }
                // 其他情况（管理员或普通用户）返回所有产品
                
                var products = await query.ToListAsync();

                // 添加日志
                Console.WriteLine($"用户角色: {userRole}, 用户ID: {userId}, 农户ID参数: {farmerId}");
                Console.WriteLine($"查询到 {products.Count} 个产品");
                
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
                        Farmer = new { p.Farmer.UserId, p.Farmer.Username },
                        p.CreateTime,
                        p.UpdateTime,
                        p.IsActive
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
                    .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

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
                        Farmer = new { product.Farmer.UserId, product.Farmer.Username },
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
                    IsActive = true
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

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
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 检查是否是产品所有者
                if (product.FarmerId != request.FarmerId)
                {
                    return BadRequest(new { code = 400, message = "只有产品所有者才能更新产品" });
                }

                // 更新产品信息
                product.ProductName = request.ProductName ?? product.ProductName;
                product.Description = request.Description ?? product.Description;
                product.Price = request.Price ?? product.Price;
                product.Stock = request.Stock ?? product.Stock;
                product.ImageUrl = request.ImageUrl ?? product.ImageUrl;
                product.Category = request.Category ?? product.Category;
                product.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

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
        /// 删除产品（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id, [FromBody] DeleteProductRequest request)
        {
            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 检查是否是产品所有者
                if (product.FarmerId != request.FarmerId)
                {
                    return BadRequest(new { code = 400, message = "只有产品所有者才能删除产品" });
                }

                // 软删除
                product.IsActive = false;
                product.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "删除产品成功" });
            }
            catch (Exception ex)
            {
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
                csv.AppendLine("产品ID,产品名称,类别,描述,价格,库存,农户ID,农户名称,创建时间,上次更新时间,是否上架,平均评分,评价数量,图片URL");
                
                // 添加产品数据行
                foreach (var product in products)
                {
                    csv.AppendLine($"{product.ProductId},{EscapeCsvField(product.ProductName)},{EscapeCsvField(product.Category)},{EscapeCsvField(product.Description)},{product.Price},{product.Stock},{product.FarmerId},{EscapeCsvField(product.Farmer?.Username ?? "未知")},{product.CreateTime},{product.UpdateTime},{product.IsActive},{product.AverageRating},{product.ReviewCount},{EscapeCsvField(product.ImageUrl)}");
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
                                AverageRating = 0,
                                ReviewCount = 0,
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

        /// <summary>
        /// 批量更新产品状态
        /// </summary>
        [HttpPut("batch/status")]
        [Authorize(Roles = "farmer,admin")]
        public async Task<IActionResult> UpdateProductsStatus([FromBody] BatchUpdateStatusRequest request)
        {
            try
            {
                // 验证请求
                if (request == null || request.ProductIds == null || !request.ProductIds.Any())
                {
                    return BadRequest(new { code = 400, message = "请提供产品ID列表" });
                }

                if (string.IsNullOrWhiteSpace(request.Status))
                {
                    return BadRequest(new { code = 400, message = "请提供状态" });
                }

                // 验证状态值
                if (request.Status != "上架" && request.Status != "下架")
                {
                    return BadRequest(new { code = 400, message = "状态值无效，只能为'上架'或'下架'" });
                }

                // 查找要更新的产品
                var productsToUpdate = await _context.Products
                    .Where(p => request.ProductIds.Contains(p.ProductId))
                    .ToListAsync();

                if (productsToUpdate.Count == 0)
                {
                    return NotFound(new { code = 404, message = "未找到指定的产品" });
                }

                // 如果是农户用户，只能更新自己的产品
                if (request.FarmerId.HasValue)
                {
                    productsToUpdate = productsToUpdate.Where(p => p.FarmerId == request.FarmerId.Value).ToList();
                    
                    if (productsToUpdate.Count == 0)
                    {
                        return BadRequest(new { code = 400, message = "您没有权限更新这些产品" });
                    }
                }

                // 更新产品状态
                bool isActive = request.Status == "上架";
                foreach (var product in productsToUpdate)
                {
                    product.IsActive = isActive;
                    product.UpdateTime = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = $"成功更新{productsToUpdate.Count}个产品状态",
                    data = productsToUpdate.Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        Status = p.IsActive ? "上架" : "下架",
                        p.UpdateTime
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 批量删除产品
        /// </summary>
        [HttpDelete("batch")]
        [Authorize(Roles = "farmer,admin")]
        public async Task<IActionResult> DeleteProducts([FromBody] BatchDeleteRequest request)
        {
            try
            {
                // 验证请求
                if (request == null || request.ProductIds == null || !request.ProductIds.Any())
                {
                    return BadRequest(new { code = 400, message = "请提供产品ID列表" });
                }

                // 查找要删除的产品
                var productsToDelete = await _context.Products
                    .Where(p => request.ProductIds.Contains(p.ProductId))
                    .ToListAsync();

                if (productsToDelete.Count == 0)
                {
                    return NotFound(new { code = 404, message = "未找到指定的产品" });
                }

                // 如果是农户用户，只能删除自己的产品
                if (request.FarmerId.HasValue)
                {
                    productsToDelete = productsToDelete.Where(p => p.FarmerId == request.FarmerId.Value).ToList();
                    
                    if (productsToDelete.Count == 0)
                    {
                        return BadRequest(new { code = 400, message = "您没有权限删除这些产品" });
                    }
                }

                // 执行软删除
                foreach (var product in productsToDelete)
                {
                    product.IsActive = false;
                    product.UpdateTime = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = $"成功删除{productsToDelete.Count}个产品"
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
        /// 产品价格
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "价格必须大于0")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 库存数量
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
    }

    /// <summary>
    /// 批量更新状态的请求模型
    /// </summary>
    public class BatchUpdateStatusRequest
    {
        /// <summary>
        /// 产品ID列表
        /// </summary>
        [Required]
        public List<int> ProductIds { get; set; }

        /// <summary>
        /// 要更新的状态（上架/下架）
        /// </summary>
        [Required]
        public string Status { get; set; }

        /// <summary>
        /// 农户ID（如果是农户用户）
        /// </summary>
        public int? FarmerId { get; set; }
    }

    /// <summary>
    /// 批量删除的请求模型
    /// </summary>
    public class BatchDeleteRequest
    {
        /// <summary>
        /// 产品ID列表
        /// </summary>
        [Required]
        public List<int> ProductIds { get; set; }

        /// <summary>
        /// 农户ID（如果是农户用户）
        /// </summary>
        public int? FarmerId { get; set; }
    }
} 
 
 
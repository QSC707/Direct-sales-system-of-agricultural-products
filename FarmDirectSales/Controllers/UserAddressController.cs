using Microsoft.AspNetCore.Mvc;
using FarmDirectSales.Models;
using FarmDirectSales.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Controllers.RequestModels;

namespace FarmDirectSales.Controllers.RequestModels
{
    /// <summary>
    /// 用户添加地址请求模型
    /// </summary>
    public class UserAddressAddRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }

        /// <summary>
        /// 收货人姓名
        /// </summary>
        [Required(ErrorMessage = "收货人姓名不能为空")]
        [StringLength(50, ErrorMessage = "收货人姓名长度不能超过50个字符")]
        public string ReceiverName { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [Required(ErrorMessage = "联系电话不能为空")]
        [StringLength(20, ErrorMessage = "联系电话长度不能超过20个字符")]
        [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "联系电话格式不正确")]
        public string ContactPhone { get; set; }

        /// <summary>
        /// 省份
        /// </summary>
        [Required(ErrorMessage = "省份不能为空")]
        [StringLength(20, ErrorMessage = "省份长度不能超过20个字符")]
        public string Province { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        [Required(ErrorMessage = "城市不能为空")]
        [StringLength(20, ErrorMessage = "城市长度不能超过20个字符")]
        public string City { get; set; }

        /// <summary>
        /// 区/县
        /// </summary>
        [Required(ErrorMessage = "区/县不能为空")]
        [StringLength(20, ErrorMessage = "区/县长度不能超过20个字符")]
        public string District { get; set; }

        /// <summary>
        /// 详细地址
        /// </summary>
        [Required(ErrorMessage = "详细地址不能为空")]
        [StringLength(100, ErrorMessage = "详细地址长度不能超过100个字符")]
        public string DetailAddress { get; set; }

        /// <summary>
        /// 是否默认地址
        /// </summary>
        public bool IsDefault { get; set; } = false;
    }

    /// <summary>
    /// 用户更新地址请求模型
    /// </summary>
    public class UserAddressUpdateRequest
    {
        /// <summary>
        /// 收货人姓名
        /// </summary>
        [Required(ErrorMessage = "收货人姓名不能为空")]
        [StringLength(50, ErrorMessage = "收货人姓名长度不能超过50个字符")]
        public string ReceiverName { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [Required(ErrorMessage = "联系电话不能为空")]
        [StringLength(20, ErrorMessage = "联系电话长度不能超过20个字符")]
        [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "联系电话格式不正确")]
        public string ContactPhone { get; set; }

        /// <summary>
        /// 省份
        /// </summary>
        [Required(ErrorMessage = "省份不能为空")]
        [StringLength(20, ErrorMessage = "省份长度不能超过20个字符")]
        public string Province { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        [Required(ErrorMessage = "城市不能为空")]
        [StringLength(20, ErrorMessage = "城市长度不能超过20个字符")]
        public string City { get; set; }

        /// <summary>
        /// 区/县
        /// </summary>
        [Required(ErrorMessage = "区/县不能为空")]
        [StringLength(20, ErrorMessage = "区/县长度不能超过20个字符")]
        public string District { get; set; }

        /// <summary>
        /// 详细地址
        /// </summary>
        [Required(ErrorMessage = "详细地址不能为空")]
        [StringLength(100, ErrorMessage = "详细地址长度不能超过100个字符")]
        public string DetailAddress { get; set; }

        /// <summary>
        /// 是否默认地址
        /// </summary>
        public bool IsDefault { get; set; } = false;
    }
}

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 用户地址控制器
    /// </summary>
    [ApiController]
    [Route("api/user-address")]
    public class UserAddressController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UserAddressController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取用户的所有地址
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserAddresses(int userId)
        {
            try
            {
                // 验证当前用户身份
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || (currentUser.UserId != userId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此地址信息" });
                }

                // 获取用户的所有地址
                var addresses = await _context.UserAddresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.UpdateTime)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取地址列表成功",
                    data = addresses
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取用户的默认地址
        /// </summary>
        [HttpGet("user/{userId}/default")]
        public async Task<IActionResult> GetDefaultAddress(int userId)
        {
            try
            {
                // 验证当前用户身份
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || (currentUser.UserId != userId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此地址信息" });
                }

                // 获取用户的默认地址
                var defaultAddress = await _context.UserAddresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .FirstOrDefaultAsync();

                if (defaultAddress == null)
                {
                    // 如果没有默认地址，返回最新的地址
                    defaultAddress = await _context.UserAddresses
                        .Where(a => a.UserId == userId)
                        .OrderByDescending(a => a.UpdateTime)
                        .FirstOrDefaultAsync();
                }

                if (defaultAddress == null)
                {
                    return NotFound(new { code = 404, message = "未找到地址信息" });
                }

                return Ok(new
                {
                    code = 200,
                    message = "获取默认地址成功",
                    data = defaultAddress
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 添加新地址
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddUserAddress([FromBody] UserAddressAddRequest request)
        {
            try
            {
                // 验证当前用户身份
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || (currentUser.UserId != request.UserId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限添加此地址" });
                }

                // 如果设置为默认地址，则先将其他地址设为非默认
                if (request.IsDefault)
                {
                    var existingDefaultAddresses = await _context.UserAddresses
                        .Where(a => a.UserId == request.UserId && a.IsDefault)
                        .ToListAsync();

                    foreach (var address in existingDefaultAddresses)
                    {
                        address.IsDefault = false;
                        _context.UserAddresses.Update(address);
                    }
                }

                // 创建新地址
                var newAddress = new UserAddress
                {
                    UserId = request.UserId,
                    ReceiverName = request.ReceiverName,
                    ContactPhone = request.ContactPhone,
                    Province = request.Province,
                    City = request.City,
                    District = request.District,
                    DetailAddress = request.DetailAddress,
                    IsDefault = request.IsDefault,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                _context.UserAddresses.Add(newAddress);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "添加地址成功",
                    data = newAddress
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新地址
        /// </summary>
        [HttpPut("{addressId}")]
        public async Task<IActionResult> UpdateUserAddress(int addressId, [FromBody] UserAddressUpdateRequest request)
        {
            try
            {
                // 查找地址
                var address = await _context.UserAddresses.FindAsync(addressId);
                if (address == null)
                {
                    return NotFound(new { code = 404, message = "地址不存在" });
                }

                // 验证当前用户身份
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || (currentUser.UserId != address.UserId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限修改此地址" });
                }

                // 如果设置为默认地址，则先将其他地址设为非默认
                if (request.IsDefault && !address.IsDefault)
                {
                    var existingDefaultAddresses = await _context.UserAddresses
                        .Where(a => a.UserId == address.UserId && a.IsDefault)
                        .ToListAsync();

                    foreach (var defaultAddress in existingDefaultAddresses)
                    {
                        defaultAddress.IsDefault = false;
                        _context.UserAddresses.Update(defaultAddress);
                    }
                }

                // 更新地址信息
                address.ReceiverName = request.ReceiverName;
                address.ContactPhone = request.ContactPhone;
                address.Province = request.Province;
                address.City = request.City;
                address.District = request.District;
                address.DetailAddress = request.DetailAddress;
                address.IsDefault = request.IsDefault;
                address.UpdateTime = DateTime.Now;

                _context.UserAddresses.Update(address);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "更新地址成功",
                    data = address
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除地址
        /// </summary>
        [HttpDelete("{addressId}")]
        public async Task<IActionResult> DeleteUserAddress(int addressId)
        {
            try
            {
                // 查找地址
                var address = await _context.UserAddresses.FindAsync(addressId);
                if (address == null)
                {
                    return NotFound(new { code = 404, message = "地址不存在" });
                }

                // 验证当前用户身份
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || (currentUser.UserId != address.UserId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限删除此地址" });
                }

                // 删除地址
                _context.UserAddresses.Remove(address);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "删除地址成功"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }
} 
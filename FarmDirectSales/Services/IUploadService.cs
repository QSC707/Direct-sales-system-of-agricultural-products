using FarmDirectSales.Models;
using Microsoft.AspNetCore.Http;

namespace FarmDirectSales.Services
{
    /// <summary>
    /// 文件上传服务接口
    /// </summary>
    public interface IUploadService
    {
        /// <summary>
        /// 上传单个文件
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <param name="productId">关联的产品ID</param>
        /// <returns>上传文件信息</returns>
        Task<Upload> UploadFileAsync(IFormFile file, int? productId);
        
        /// <summary>
        /// 上传多个文件
        /// </summary>
        /// <param name="files">上传的文件集合</param>
        /// <param name="productId">关联的产品ID</param>
        /// <returns>上传文件信息集合</returns>
        Task<IEnumerable<Upload>> UploadFilesAsync(IEnumerable<IFormFile> files, int? productId);
        
        /// <summary>
        /// 获取产品图片列表
        /// </summary>
        /// <param name="productId">产品ID</param>
        /// <returns>图片列表</returns>
        Task<IEnumerable<Upload>> GetProductUploadsAsync(int productId);
        
        /// <summary>
        /// 删除上传的文件
        /// </summary>
        /// <param name="uploadId">上传ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteUploadAsync(int uploadId);
    }
} 
 
 
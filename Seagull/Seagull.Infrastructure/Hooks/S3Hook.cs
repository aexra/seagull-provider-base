using Microsoft.AspNetCore.Http;
using Seagull.Infrastructure.Services;

namespace Seagull.Infrastructure.Hooks;

public class UploadResult(string fileName, bool success, string? errorMessage = null, Exception? exception = null) : S3OperationResult(success, errorMessage, exception)
{
    public string FileName = fileName;
}

public class S3Hook(S3Service s3)
{
    private readonly S3Service _s3 = s3;

    public async Task<UploadResult> UploadAsync(string bucket, string path, IFormFile file)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        using var stream = file.OpenReadStream();
        var result = await _s3.UploadObjectAsync(bucket, $"{path}/{fileName}", stream, file.ContentType);
        return new UploadResult(fileName, result.Success, result.ErrorMessage, result.Exception);
    }
    public async Task<S3ObjectResult<Stream>> LoadAsync(string bucket, string path)
    {
        using var stream = new MemoryStream();
        var result = await _s3.LoadObjectAsync(bucket, path);
        return result;
    }
    public async Task<S3ObjectResult<Stream>> LoadAsync(string bucket, string path, string fileName)
    {
        using var stream = new MemoryStream();
        var result = await _s3.LoadObjectAsync(bucket, $"{path}/{fileName}");
        return result;
    }
    public async Task<S3ObjectResult<Stream>> LoadAsync(string bucket, string path, string fileName, string contentType)
    {
        using var stream = new MemoryStream();
        var result = await _s3.LoadObjectAsync(bucket, $"{path}/{fileName}/{contentType}");
        return result;
    }
    public async Task<S3OperationResult> DeleteAsync(string bucket, string path)
    {
        return await _s3.DeleteObjectAsync(bucket, path);
    }
    public async Task<S3OperationResult> DeleteAsync(string bucket, string path, string fileName)
    {
        return await _s3.DeleteObjectAsync(bucket, $"{path}/{fileName}");
    }
    public async Task<S3OperationResult> DeleteAsync(string bucket, string path, string fileName, string contentType)
    {
        return await _s3.DeleteObjectAsync(bucket, $"{path}/{fileName}/{contentType}");
    }
}

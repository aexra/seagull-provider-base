using Minio.DataModel.Args;
using Minio.Exceptions;
using Minio;

namespace Seagull.Infrastructure.Services;

#region DTO
public class S3OperationResult(bool success, string? errorMessage = null, Exception? exception = null)
{
    public bool Success { get; } = success;
    public string? ErrorMessage { get; } = errorMessage;
    public Exception? Exception { get; } = exception;

    public static S3OperationResult Ok() => new(true);
    public static S3OperationResult Fail(string errorMessage, Exception? ex = null) => new(false, errorMessage, ex);
}
public class S3ObjectResult<T>(T? data, bool success, string? errorMessage = null, Exception? exception = null) : S3OperationResult(success, errorMessage, exception)
{
    public T? Data { get; } = data;

    public static S3ObjectResult<T> Ok(T data) => new(data, true);
    public static new S3ObjectResult<T> Fail(string errorMessage, Exception? ex = null) => new(default, false, errorMessage, ex);
}
public class S3ExistsResult(bool exists, bool success, string? errorMessage = null, Exception? exception = null) : S3OperationResult(success, errorMessage, exception)
{
    public bool Exists { get; } = exists;

    public static S3ExistsResult Ok(bool exists) => new(exists, true);
    public static new S3ExistsResult Fail(string errorMessage, Exception? ex = null) => new(false, false, errorMessage, ex);
}
#endregion

public class S3Service(IMinioClient client)
{
    private readonly IMinioClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<S3ObjectResult<Stream>> LoadObjectAsync(string bucket, string path)
    {
        try
        {
            // Проверяем существование бакета
            var beArgs = new BucketExistsArgs().WithBucket(bucket);
            bool found = await _client.BucketExistsAsync(beArgs);
            if (!found)
            {
                return S3ObjectResult<Stream>.Fail($"Bucket {bucket} does not exist");
            }

            var memoryStream = new MemoryStream();
            var args = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(path)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _client.GetObjectAsync(args);
            memoryStream.Position = 0;

            return S3ObjectResult<Stream>.Ok(memoryStream);
        }
        catch (MinioException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return S3ObjectResult<Stream>.Fail($"Object {path} not found in bucket {bucket}", ex);
        }
        catch (Exception ex)
        {
            return S3ObjectResult<Stream>.Fail($"Error loading object {path} from bucket {bucket}", ex);
        }
    }
    public async Task<S3OperationResult> UploadObjectAsync(string bucket, string path, Stream data, string contentType)
    {
        try
        {
            // Проверяем существование бакета
            var beArgs = new BucketExistsArgs().WithBucket(bucket);
            bool found = await _client.BucketExistsAsync(beArgs);
            if (!found)
            {
                return S3OperationResult.Fail($"Bucket {bucket} does not exist");
            }

            data.Position = 0;
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(path)
                .WithStreamData(data)
                .WithObjectSize(data.Length)
                .WithContentType(contentType);

            await _client.PutObjectAsync(putObjectArgs);
            return S3OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return S3OperationResult.Fail($"Error uploading object {path} to bucket {bucket}", ex);
        }
    }
    public async Task<S3OperationResult> DeleteObjectAsync(string bucket, string path)
    {
        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(path);

            await _client.RemoveObjectAsync(args);
            return S3OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return S3OperationResult.Fail($"Error deleting object {path} from bucket {bucket}", ex);
        }
    }
    public async Task<S3OperationResult> ReplaceObjectAsync(string bucket, string path, Stream data, string contentType)
    {
        try
        {
            // Удаляем старый объект, если существует
            var existsResult = await ExistsAsync(bucket, path);
            if (existsResult.Success && existsResult.Exists)
            {
                var deleteResult = await DeleteObjectAsync(bucket, path);
                if (!deleteResult.Success)
                {
                    return deleteResult;
                }
            }

            return await UploadObjectAsync(bucket, path, data, contentType);
        }
        catch (Exception ex)
        {
            return S3OperationResult.Fail($"Error replacing object {path} in bucket {bucket}", ex);
        }
    }
    public async Task<S3ExistsResult> ExistsAsync(string bucket, string path)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(path);

            await _client.StatObjectAsync(args);
            return S3ExistsResult.Ok(true);
        }
        catch (MinioException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return S3ExistsResult.Ok(false);
        }
        catch (Exception ex)
        {
            return S3ExistsResult.Fail($"Error checking existence of object {path} in bucket {bucket}", ex);
        }
    }
}
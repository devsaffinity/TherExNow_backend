using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aps.Apps.HepNow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly string _bucketName;
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<UploadController> _logger;
        private const double TimeoutDuration = 12;

        public UploadController(IConfiguration configuration, ILogger<UploadController> logger)
        {
            _bucketName = configuration["S3Credentials:BucketName"];
            var accessKey = configuration["S3Credentials:AccessKey"];
            var secretKey = configuration["S3Credentials:SecretKey"];
            _s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.USWest1);
            _logger = logger;
        }

        [HttpPost("GetPresignedUrls")]
        public async Task<IActionResult> GetPresignedUrlsForChunksAsync([FromBody] PresignedUrlChunkInput chunks)
        {
            try
            {
                if (chunks.ChunkCount == 0)
                {
                    return BadRequest("No chunk numbers provided.");
                }

                var initiateRequest = new InitiateMultipartUploadRequest
                {
                    BucketName = _bucketName,
                    Key = chunks.key
                };

                var initiateResponse = _s3Client.InitiateMultipartUploadAsync(initiateRequest).Result;

                var presignedUrls = new List<string>();
                for (var chunkNumber = 1; chunkNumber <= chunks.ChunkCount; chunkNumber++)
                {
                    // Use the same expiration for each chunk
                    var presignedUrl = await GeneratePresignedUrl(chunks.key, chunkNumber, TimeoutDuration, initiateResponse.UploadId);
                    presignedUrls.Add(presignedUrl);
                }

                return Ok(presignedUrls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating presigned upload URLs");
                return BadRequest("Internal server error.");
            }
        }

        private async Task<string> GeneratePresignedUrl(string objectKey, int chunkNumber, double duration, string uploadId)
        {
            try
            {
                AWSConfigsS3.UseSignatureVersion4 = true;
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = objectKey,
                    Expires = DateTime.UtcNow.AddHours(duration),
                    Verb = HttpVerb.PUT,
                    PartNumber = chunkNumber,
                    // Adjust the key to differentiate between chunks, for example: sample_part1.txt, sample_part2.txt, etc.
                    UploadId = uploadId
                };

                return await _s3Client.GetPreSignedURLAsync(request);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Error generating presigned URL for {ObjectKey} part {ChunkNumber}", objectKey, chunkNumber);
                throw;
            }
        }
    }
}

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
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<UploadController> _logger;
        private const double TimeoutDuration = 12;

        // Whether the bucket has S3 Transfer Acceleration switched on, re-read every few minutes.
        private static bool? _accelerationEnabled;
        private static DateTime _accelerationCheckedAt;
        private static readonly TimeSpan AccelerationRecheck = TimeSpan.FromMinutes(10);

        public UploadController(IConfiguration configuration, ILogger<UploadController> logger)
        {
            _bucketName = configuration["S3Credentials:BucketName"];
            _accessKey = configuration["S3Credentials:AccessKey"];
            _secretKey = configuration["S3Credentials:SecretKey"];
            _s3Client = new AmazonS3Client(_accessKey, _secretKey, RegionEndpoint.USWest1);
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

                var presignClient = await UploadUrlClientAsync();
                var presignedUrls = new List<string>();
                for (var chunkNumber = 1; chunkNumber <= chunks.ChunkCount; chunkNumber++)
                {
                    // Use the same expiration for each chunk
                    var presignedUrl = await GeneratePresignedUrl(presignClient, chunks.key, chunkNumber, TimeoutDuration, initiateResponse.UploadId);
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

        [HttpPost("CompleteMultipartUpload")]
        public async Task<IActionResult> CompleteMultipartUploadAsync([FromBody] CompleteMultipartUploadInput input)
        {
            try
            {
                var parts = new List<PartETag>();
                var listRequest = new ListPartsRequest
                {
                    BucketName = _bucketName,
                    Key = input.key,
                    UploadId = input.uploadId
                };

                ListPartsResponse listResponse;
                do
                {
                    listResponse = await _s3Client.ListPartsAsync(listRequest);
                    parts.AddRange(listResponse.Parts.Select(p => new PartETag(p.PartNumber, p.ETag)));
                    listRequest.PartNumberMarker = listResponse.NextPartNumberMarker.ToString();
                } while (listResponse.IsTruncated);

                if (input.expectedPartCount > 0 && parts.Count != input.expectedPartCount)
                {
                    return BadRequest($"Expected {input.expectedPartCount} parts but S3 has {parts.Count}.");
                }

                await _s3Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
                {
                    BucketName = _bucketName,
                    Key = input.key,
                    UploadId = input.uploadId,
                    PartETags = parts
                });

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing multipart upload for {ObjectKey}", input.key);
                return BadRequest($"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("AbortMultipartUpload")]
        public async Task<IActionResult> AbortMultipartUploadAsync([FromBody] AbortMultipartUploadInput input)
        {
            try
            {
                await _s3Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                {
                    BucketName = _bucketName,
                    Key = input.key,
                    UploadId = input.uploadId
                });

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aborting multipart upload for {ObjectKey}", input.key);
                return BadRequest($"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// The client that signs the part-upload URLs browsers and the mobile apps PUT to. From some networks
        /// the regional S3 addresses reset TLS connections at random (measured 2026-09-17: 6 of 20 attempts got
        /// through, shown to users as net::ERR_CONNECTION_RESET), while the Transfer Acceleration endpoint,
        /// served from CloudFront edge addresses, got through 20 of 20. It is only used once the bucket has
        /// acceleration enabled, so this code is safe to deploy before or after that switch. The server's own
        /// S3 calls stay on the regional endpoint.
        /// </summary>
        private async Task<IAmazonS3> UploadUrlClientAsync()
        {
            if (_accelerationEnabled == null || DateTime.UtcNow - _accelerationCheckedAt > AccelerationRecheck)
            {
                try
                {
                    var config = await _s3Client.GetBucketAccelerateConfigurationAsync(new GetBucketAccelerateConfigurationRequest { BucketName = _bucketName });
                    _accelerationEnabled = config.Status == BucketAccelerateStatus.Enabled;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read Transfer Acceleration status for {Bucket}; signing regional upload URLs", _bucketName);
                    _accelerationEnabled = false;
                }
                _accelerationCheckedAt = DateTime.UtcNow;
            }

            return _accelerationEnabled == true
                ? new AmazonS3Client(_accessKey, _secretKey, new AmazonS3Config { RegionEndpoint = RegionEndpoint.USWest1, UseAccelerateEndpoint = true })
                : _s3Client;
        }

        private async Task<string> GeneratePresignedUrl(IAmazonS3 client, string objectKey, int chunkNumber, double duration, string uploadId)
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

                return await client.GetPreSignedURLAsync(request);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Error generating presigned URL for {ObjectKey} part {ChunkNumber}", objectKey, chunkNumber);
                throw;
            }
        }
    }
}

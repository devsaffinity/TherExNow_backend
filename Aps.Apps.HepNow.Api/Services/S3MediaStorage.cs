using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Aps.Apps.HepNow.Services;

public class S3MediaStorage : IMediaStorage
{
    private readonly IConfiguration configuration;
    private IAmazonS3 client;

    public S3MediaStorage(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public bool CanDelete => configuration["env"] == "prd";

    public async Task<int> DeleteAllVersionsAsync(string key)
    {
        if (!CanDelete)
            throw new InvalidOperationException("Media is only deleted from production; dev shares the bucket.");

        var bucket = configuration["S3Credentials:BucketName"];
        var s3 = Client();
        var removed = 0;

        // The bucket is versioned, so a plain DeleteObject would only add a delete marker and leave the
        // file recoverable. Every version and marker of this exact key goes; the prefix match alone would
        // also catch longer keys that merely start with it.
        var request = new ListVersionsRequest { BucketName = bucket, Prefix = key };
        ListVersionsResponse page;
        do
        {
            page = await s3.ListVersionsAsync(request);

            var versions = page.Versions
                .Where(v => v.Key == key)
                .Select(v => new KeyVersion { Key = v.Key, VersionId = v.VersionId })
                .ToList();

            if (versions.Count > 0)
            {
                var result = await s3.DeleteObjectsAsync(new DeleteObjectsRequest { BucketName = bucket, Objects = versions });
                removed += result.DeletedObjects.Count;
            }

            request.KeyMarker = page.NextKeyMarker;
            request.VersionIdMarker = page.NextVersionIdMarker;
        }
        while (page.IsTruncated);

        return removed;
    }

    private IAmazonS3 Client() => client ??= new AmazonS3Client(
        configuration["S3Credentials:AccessKey"],
        configuration["S3Credentials:SecretKey"],
        RegionEndpoint.GetBySystemName(configuration["S3Credentials:Region"] ?? "us-west-1"));
}

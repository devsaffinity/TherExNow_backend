namespace Aps.Apps.HepNow.Services.Contracts;

/// <summary>The bucket that holds uploaded media (keys under <c>files/</c>).</summary>
public interface IMediaStorage
{
    /// <summary>
    /// False outside production. dev and prd share one bucket, so a deletion made from dev would
    /// remove production media.
    /// </summary>
    bool CanDelete { get; }

    /// <summary>Permanently removes every version and delete marker of exactly this key; returns how many.</summary>
    Task<int> DeleteAllVersionsAsync(string key);
}

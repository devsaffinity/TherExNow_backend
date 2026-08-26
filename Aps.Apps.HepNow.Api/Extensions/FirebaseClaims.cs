using Newtonsoft.Json.Linq;

namespace Aps.Apps.HepNow.Extensions;

/// <summary>
/// Reads identity facts out of an already-verified Firebase ID token.
/// </summary>
/// <remarks>
/// The important rule lives in <see cref="TryGetVerifiedEmail"/>: an address on the token is not
/// proof the caller controls it. Firebase issues a token as soon as an email/password account is
/// created, before the confirmation link is followed, and some federated providers never verify at
/// all. Matching an existing row on such an address and attaching the caller's UID to it hands
/// them that account, so linking and auto-provisioning must both go through the verified path.
/// </remarks>
public static class FirebaseClaims
{
    /// <summary>Providers that establish control of the address before issuing an identity.</summary>
    private static readonly HashSet<string> VerifiedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "google.com",
        "apple.com",
    };

    /// <summary>
    /// The address on the token, verified or not. Safe as a seed value on a record the caller is
    /// already known to own — never as the key that decides which account a caller gets.
    /// </summary>
    public static string GetEmail(ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return null;
        }

        var email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrWhiteSpace(email))
        {
            return email;
        }

        // Federated sign-ins carry it under firebase.identities.email instead of a top-level claim.
        return ReadFirebaseClaim(principal)?.SelectToken("identities.email[0]")?.Value<string>();
    }

    /// <summary>Whether the identity provider has confirmed the caller owns the address.</summary>
    public static bool IsEmailVerified(ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return false;
        }

        // JwtBearer surfaces the token's boolean as the string "true"/"false".
        if (bool.TryParse(principal.FindFirstValue("email_verified"), out var verified) && verified)
        {
            return true;
        }

        var provider = GetSignInProvider(principal);
        return provider != null && VerifiedProviders.Contains(provider);
    }

    /// <summary>e.g. "password", "google.com", "apple.com".</summary>
    public static string GetSignInProvider(ClaimsPrincipal principal) =>
        ReadFirebaseClaim(principal)?.SelectToken("sign_in_provider")?.Value<string>();

    /// <summary>
    /// True only when the token carries an address the provider actually verified. This is the
    /// only safe basis for linking a UID to an existing account or creating one on the fly.
    /// </summary>
    public static bool TryGetVerifiedEmail(ClaimsPrincipal principal, out string email)
    {
        email = GetEmail(principal);
        return !string.IsNullOrWhiteSpace(email) && IsEmailVerified(principal);
    }

    private static JObject ReadFirebaseClaim(ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return null;
        }

        var raw = principal.FindFirstValue("firebase");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        // Parsed defensively rather than through `dynamic`: a malformed claim should degrade to
        // "no email" and leave the caller unidentified, not throw out of the auth pipeline.
        try
        {
            return JObject.Parse(raw);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

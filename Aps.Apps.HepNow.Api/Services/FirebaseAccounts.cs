using FirebaseAdmin.Auth;

namespace Aps.Apps.HepNow.Services;

public class FirebaseAccounts : IFirebaseAccounts
{
    private readonly FirebaseAppCreator firebaseAppCreator;

    public FirebaseAccounts(FirebaseAppCreator firebaseAppCreator)
    {
        this.firebaseAppCreator = firebaseAppCreator;
    }

    public async Task<bool> ExistsAsync(string uid)
    {
        try
        {
            await Auth().GetUserAsync(uid);
            return true;
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            return false;
        }
    }

    public async Task DeleteAsync(string uid)
    {
        try
        {
            await Auth().DeleteUserAsync(uid);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            // Already gone: a retried deletion, or a login removed by hand.
        }
    }

    private FirebaseAuth Auth() => FirebaseAuth.GetAuth(firebaseAppCreator.GetFirebaseApp());
}

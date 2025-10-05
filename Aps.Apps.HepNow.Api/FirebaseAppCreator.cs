using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace Aps.Apps.HepNow
{
    public class FirebaseAppCreator
    {
        private FirebaseApp _app;
        private string firebaseJsonUri;

        public FirebaseAppCreator(IConfiguration configuration)
        {
            firebaseJsonUri = configuration["ProjectJsonUri"];
        }

        public FirebaseApp GetFirebaseApp()
        {
            if (_app == null)
            {
                string[] path = new string[] { "Data", firebaseJsonUri };
                if (!File.Exists(System.IO.Path.Combine(path)))
                {
                    throw new FileNotFoundException("config file not found");
                }
                if (FirebaseApp.DefaultInstance == null)
                {
                    var app = FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(System.IO.Path.Combine(path))
                    });

                    _app = app;
                }
                else
                {
                    _app = FirebaseApp.DefaultInstance;
                }
            }
            return _app;
        }

        public async Task DeleteUser(string externalId)
        {
            if (_app == null)
            {
                GetFirebaseApp();
            }

            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            await auth.DeleteUserAsync(externalId);
        }
    }
}

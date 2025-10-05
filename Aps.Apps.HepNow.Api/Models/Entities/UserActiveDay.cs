using Firebase.Auth;

namespace Aps.Apps.HepNow.Models.Entities
{
    public class UserActiveDay: EntityDef
    {
        public int UserId { get; set; }

        public Users User { get; set; }

        public DateTime Day { get; set; }
    }
}

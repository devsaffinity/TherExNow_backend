namespace Aps.Apps.HepNow.Models.Dtos
{
    public class EmailVerifiedDto
    {
        public bool VerifiedInFirebase { get; set; }
        public bool VerifiedInDatabase { get; set; }
    }
}

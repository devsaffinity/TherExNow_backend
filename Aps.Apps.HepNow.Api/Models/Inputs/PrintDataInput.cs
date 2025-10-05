namespace Aps.Apps.HepNow.Models.Inputs
{
    public class PrintDataInput: InputDef
    {
        [GraphQLIgnore]
        public int? UserId { get; set; }

        public string Data { get; set; }
    }
}

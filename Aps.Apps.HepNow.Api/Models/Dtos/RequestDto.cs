using Aps.CommonBack.Base.Models.Dtos;

namespace Aps.Apps.HepNow.Models.Dtos
{
    public class RequestDto: DtoDef
    {
        public Users User { get; set; }
        public Requests Request { get; set; }
        public int ProductCount { get; set; }
    }
}

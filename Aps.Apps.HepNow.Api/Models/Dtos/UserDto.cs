using Aps.CommonBack.Base.Models.Dtos;

namespace Aps.Apps.HepNow.Models.Dtos
{
    public class UserDto: DtoDef
    {
        public Plans Plan { get; set; }
        public Users User { get; set; }
    }
}

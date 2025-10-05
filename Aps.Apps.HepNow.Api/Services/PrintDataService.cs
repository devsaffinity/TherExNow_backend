
namespace Aps.Apps.HepNow.Services;

public class PrintDataService : ServiceBase<PrintData, PrintDataInput>, IPrintDataService
{
    public PrintDataService(IPrintDataRepository repository) : base(repository)
    {
    }
}

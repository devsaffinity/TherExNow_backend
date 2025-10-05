namespace Aps.Apps.HepNow.Data;

public class DataInitializer
{
    internal static void Initialize(DataContext context, bool v)
    {
        context.Database.Migrate();
    }
}


namespace Aps.Apps.HepNow.Services.Contracts;

public interface IShoppingCartService : IServiceBase<ShoppingCart, ShoppingCartInput>
{
    ResponseBase RemoveAll(int userId);
}

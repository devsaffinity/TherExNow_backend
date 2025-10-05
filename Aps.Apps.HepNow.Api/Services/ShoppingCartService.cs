namespace Aps.Apps.HepNow.Services;

public class ShoppingCartService : ServiceBase<ShoppingCart, ShoppingCartInput>, IShoppingCartService
{
    public ShoppingCartService(IShoppingCartRepository repository) : base(repository)
    {
        this.repository = repository;
    }

    #region props
    private IShoppingCartRepository repository;
    #endregion

    #region functions
    public override ResponseBase<ShoppingCart> Remove(int entityId, int userId)
    {
        if (repository.Any(r => r.Id == entityId && r.UserId != userId))
            return ResponseStatus.NotAllowd;

        return base.Remove(entityId);
    }

    public override ResponseBase<ShoppingCart> Update(ShoppingCartInput input)
    {
        if (repository.Any(r => r.Id == input.Id.GetValueOrDefault() && r.UserId != input.UserId))
            return ResponseStatus.NotAllowd;

        return base.Update(input);
    }

    public ResponseBase RemoveAll(int userId)
    {
        repository.RemoveRange(repository.Where(a => a.UserId == userId).ToList());

        return ResponseStatus.Success;
    }
    #endregion
}

namespace Aps.Apps.HepNow.Services;

public class ProductService
 : ServiceBase<Products, ProductInput>, IProductService
{
    #region props
    private IProductRepository repository;
    private readonly IExerciseProductRepository exerciseProductRepository;
    #endregion

    public ProductService(IProductRepository repository, IExerciseProductRepository exerciseProductRepository) : base(repository)
    {
        this.repository = repository;
        this.exerciseProductRepository = exerciseProductRepository;
    }

    #region functions
    public override ResponseBase<Products> Update(ProductInput input)
    {
        var product = repository.Where(w => w.Id.Equals(input.Id)).FirstOrDefault();

        if (product is null)
            return ResponseBase<Products>.Failure(ResponseStatus.NotFound);

        var isEqual = product.Name.Equals(input.Name);

        if (!isEqual)
        {
            var isExist = repository.Any(a => a.Name.Equals(input.Name));

            if (isExist)
                return ResponseBase<Products>.Failure(ResponseStatus.AlreadyExists);
        }

        return base.Update(input);
    }

    public override ResponseBase<Products> Remove(int entityId)
    {
        var product = repository.GetById(entityId);
        if (product is null)
            return ResponseBase<Products>.Failure(ResponseStatus.NotFound);

        if (repository.Any<ExerciseProducts>(a => a.ProductId == entityId) || repository.Any<RequestProducts>(a => a.ProductId == entityId))
        {
            product.IsActive = false;
            repository.Update(product);
            return new(product);
        }

        return base.Remove(entityId);
    }
    
    public ListResponseBase<Products> GetSimilarProducts(int productId)
    {
        if (!repository.Any(p => p.Id == productId))
            return ResponseStatus.NotFound;

        var categoryIds = exerciseProductRepository.Where(a => a.ProductId == productId).Select(a => a.Exercise.SubCategory.CategoryId).ToList();

        var query = exerciseProductRepository.Where(a => categoryIds.Contains(a.Exercise.SubCategory.CategoryId) && a.ProductId != productId)
            .Select(a => a.Product)
            .Distinct();

        return ListResponseBase<Products>.Success(query);
    }
    #endregion
}

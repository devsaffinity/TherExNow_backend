

using Aps.Apps.HepNow.Generics.Responses;
using System.Linq.Expressions;

namespace Aps.Apps.HepNow.Services;

public class SubCategoryService : ServiceBase<SubCategories, SubCategoryInput>, ISubCategoryService
{
    #region props
    private ISubCategoryRepository repository;
    private readonly IExerciseRepository exerciseRepository;
    #endregion

    public SubCategoryService(ISubCategoryRepository repository, IExerciseRepository exerciseRepository) : base(repository)
    {
        this.repository = repository;
        this.exerciseRepository = exerciseRepository;
    }

    #region functions
    public ListResponseBase<SubCategories> GetByCategoryId(int categoryId)
    {
        var result = repository.Where(w => w.CategoryId == categoryId);
        return ListResponseBase<SubCategories>.Success(result);
    }

    public override ResponseBase<SubCategories> Remove(int entityId)
    {
        if (exerciseRepository.Any(a => a.SubCategoryId == entityId))
            return CustomResponseStatus.ThereIsAnExerciseInThisSubcategory;

        return base.Remove(entityId);
    }
    #endregion
}

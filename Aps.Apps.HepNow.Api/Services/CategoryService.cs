

namespace Aps.Apps.HepNow.Services;

public class CategoryService : ServiceBase<Categories, CategoryInput>, ICategoryService
{
    #region props
    private ICategoryRepository repository;
    private readonly ISubCategoryRepository subCategoryRepository;
    private readonly IExerciseRepository exerciseRepository;
    #endregion

    public CategoryService(ICategoryRepository repository, ISubCategoryRepository subCategoryRepository, IExerciseRepository exerciseRepository) : base(repository)
    {
        this.repository = repository;
        this.subCategoryRepository = subCategoryRepository;
        this.exerciseRepository = exerciseRepository;
    }

    #region functions
    public override ResponseBase<Categories> Add(CategoryInput input)
    {
        if (repository.Any(a => a.Name.Equals(input.Name)))
            return ResponseBase<Categories>.Failure(ResponseStatus.AlreadyExists);
        
        return base.Add(input);
    }

    public override ResponseBase<Categories> Update(CategoryInput input)
    {
        var category = repository.Where(a => a.Id == input.Id).Include(a => a.SubCategories).FirstOrDefault();
        if (category is null)
            return ResponseBase<Categories>.Failure(ResponseStatus.NotFound);

        var isEqual = input.Name.Equals(category.Name);
        if (!isEqual && repository.Any(a => a.Name.Equals(input.Name)))
            return ResponseBase<Categories>.Failure(ResponseStatus.AlreadyExists);

        category.Name = input.Name;
        repository.Update(category);

        if (input.SubCategories != null)
        {
            //Delete SubCategory
            var deletedSubCategories = new List<SubCategories>();
            foreach (var subCategory in category.SubCategories)
            {
                if (!input.SubCategories.Any(l => l.Id == subCategory.Id))
                {
                    if (exerciseRepository.Any(a => a.SubCategoryId == subCategory.Id))
                        return ResponseBase<Categories>.Failure(ResponseStatus.NotAllowd);
                    else
                        deletedSubCategories.Add(subCategory);
                }
            }
            subCategoryRepository.RemoveRange(deletedSubCategories);

            //Update SubCategory
            foreach (var subcategory in input.SubCategories.Where(l => l.Id > 0))
            {
                var entity = subCategoryRepository.GetById(subcategory.Id.Value);
                subcategory.Adapt(entity);
                subCategoryRepository.Update(entity);
            }

            //Create SubCategory
            foreach (var subcategory in input.SubCategories.Where(l => l.Id.GetValueOrDefault() <= 0))
            {
                var entity = subcategory.Adapt<SubCategories>();
                entity.CategoryId = input.Id.Value;
                subCategoryRepository.Add(entity);
            }
        }

        return ResponseBase<Categories>.Success();
    }

    public override ResponseBase<Categories> Remove(int entityId)
    {
        var category = repository.Where(a => a.Id == entityId).Include(a => a.SubCategories).FirstOrDefault();

        if (category is null)
            return ResponseBase<Categories>.Failure(ResponseStatus.NotFound);

        var subcategoryIds = category.SubCategories.Select(c => c.Id);

        if (exerciseRepository.Any(a => subcategoryIds.Contains(a.SubCategoryId)))
            return ResponseBase<Categories>.Failure(ResponseStatus.NotAllowd);

        category.IsDeleted = true;
        foreach (var subcategory in category.SubCategories)
            subcategory.IsDeleted = true;

        repository.Update(category);
        return ResponseBase<Categories>.Success();
    }
    #endregion
}

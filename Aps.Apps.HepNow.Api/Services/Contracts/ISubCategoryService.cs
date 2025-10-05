namespace Aps.Apps.HepNow.Services.Contracts;

public interface ISubCategoryService : IServiceBase<SubCategories, SubCategoryInput>
{
    #region functions
    ListResponseBase<SubCategories> GetByCategoryId(int categoryId);
    #endregion
}

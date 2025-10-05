namespace Aps.Apps.HepNow.Services;

public class ExerciseProductService: ServiceBase<ExerciseProducts, ExerciseProductInput>, IExerciseProductService
{
    public ExerciseProductService(IExerciseProductRepository repository): base(repository)
    {
        this.repository = repository;
    }

    #region props
    private IExerciseProductRepository repository;
    #endregion

    #region functions
    public override ResponseBase<ExerciseProducts> Add(ExerciseProductInput input)
    {
        if (repository.Any(a => a.ProductId == input.ProductId && a.ExerciseId == input.ExerciseId))
            return ResponseStatus.AlreadyExists;

        return base.Add(input);
    }
    #endregion
}

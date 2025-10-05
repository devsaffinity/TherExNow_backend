namespace Aps.Apps.HepNow.Services.Contracts;

public interface ICustomizedExerciseService: IServiceBase<CustomizedExercise, CustomizedExerciseInput>
{
    ResponseBase<CustomizedExercise> Get(int userId, int exerciseId);
    ResponseBase<CustomizedExercise> Update(int userId, CustomizedExerciseInput input);
}

using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Services.Contracts;

public interface IExerciseService : IServiceBase<Exercises, ExerciseInput>
{
    ListResponseBase<ExerciseDto> GetExercises(int? userId);
    ListResponseBase<Products> GetProducts(int exerciseId);
    ResponseBase<Exercises> Remove(int entityId, string description);
}

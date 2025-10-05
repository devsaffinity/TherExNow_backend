namespace Aps.Apps.HepNow.Services.Contracts;

public interface IRoutineService: IServiceBase<Routines, RoutineInput>
{
    SingleResponseBase<Routines> GetRoutine(int routineId, int userId);
}

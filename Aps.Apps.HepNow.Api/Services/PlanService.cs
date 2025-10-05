namespace Aps.Apps.HepNow.Services;

public class PlanService : ServiceBase<Plans, PlanInput>, IPlanService
{
    #region props
    private IPlanRepository repository;
    #endregion

    public PlanService(IPlanRepository repository, IMembershipRepository membershipRepository) : base(repository)
    {
        this.repository = repository;
    }

    #region functions
    public override ResponseBase<Plans> Remove(int entityId)
    {
        if (IsExistInMembership(entityId))
            return ResponseBase<Plans>.Failure(ResponseStatus.NotAllowd);

        var result = base.Remove(entityId);

        if (!result.Status.Equals(ResponseStatus.Success))
            return ResponseBase<Plans>.Failure(result.Status);

        return ResponseBase<Plans>.Success();
    }

    private bool IsExistInMembership(int planId) => repository.Any<Memberships>(a => a.PlanId == planId);
    #endregion
}

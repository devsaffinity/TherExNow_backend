namespace Aps.Apps.HepNow.Models.Enums
{
    public enum MembershipProvider
    {
        Apple,
        Google,
        Stripe,

        // A plan an administrator granted directly -- a comp account, a tester, a support fix. It
        // carries the plan's real price so every "is this a paid member" check lights up, but it is
        // NOT money that changed hands, so revenue reporting excludes it. Appended last so the
        // stored integer values of the payment providers above never shift.
        Complimentary,
    }
}

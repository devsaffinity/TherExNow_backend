namespace Aps.Apps.HepNow.Models.Enums
{
    public enum NotificationType
    {
        [GraphQLName(nameof(FreePlanPurchased))]
        FreePlanPurchased,

        [GraphQLName(nameof(RequestCreated))]
        RequestCreated,

        [GraphQLName(nameof(RequestAccepted))]
        RequestAccepted,

        [GraphQLName(nameof(MaxTryFreePlanForEmail))]
        MaxTryFreePlanForEmail,

        [GraphQLName(nameof(MaxTryFreePlanForSms))]
        MaxTryFreePlanForSms,

        [GraphQLName(nameof(MaxTryFreePlanForRoutine))]
        MaxTryFreePlanForRoutine,

        [GraphQLName(nameof(ExerciseDeleted))]
        ExerciseDeleted,

        [GraphQLName(nameof(RequestRejected))]
        RequestRejected,

        [GraphQLName(nameof(PlanUpgraded))]
        PlanUpgraded,

        [GraphQLName(nameof(MaxTryFreePlanForQrCode))]
        MaxTryFreePlanForQrCode,

        [GraphQLName(nameof(MaxTryFreePlanForPrint))]
        MaxTryFreePlanForPrint,
    }
}

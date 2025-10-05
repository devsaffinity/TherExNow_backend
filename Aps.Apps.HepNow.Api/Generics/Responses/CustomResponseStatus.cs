namespace Aps.Apps.HepNow.Generics.Responses
{
    public record CustomResponseStatus : ResponseStatus
    {
        protected CustomResponseStatus(int code, string value) : base(code, value)
        {
        }

        public static CustomResponseStatus ProductNotFound = new(100, nameof(ProductNotFound));
        public static CustomResponseStatus ThereIsAnExerciseInThisSubcategory = new (101, nameof(ThereIsAnExerciseInThisSubcategory));
        public static CustomResponseStatus PlanNotFound = new (102, nameof(PlanNotFound));
        public static CustomResponseStatus MembershipAlreadyExists = new (103, nameof(MembershipAlreadyExists));
        public static CustomResponseStatus CurrentUserIsNotAdmin = new (104, nameof(CurrentUserIsNotAdmin));
        public static CustomResponseStatus MaxTryFreePlanForEmail = new (105, nameof(MaxTryFreePlanForEmail));
        public static CustomResponseStatus MaxTryFreePlanForSms = new (106, nameof(MaxTryFreePlanForSms));
        public static CustomResponseStatus MaxTryFreePlanForRoutine = new (107, nameof(MaxTryFreePlanForRoutine));
        public static CustomResponseStatus ExerciseNotFound = new (108, nameof(ExerciseNotFound));
        public static CustomResponseStatus DuplicateName = new (109, nameof(DuplicateName));
        public static CustomResponseStatus LimitOfMoreThan5000Characters = new (110, nameof(LimitOfMoreThan5000Characters));
    }
}

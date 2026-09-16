using Aps.Apps.HepNow.Extensions;
using Aps.CommonBack.Base;
using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(UserMutations))]
public class UserMutations : UserMutationBase<Users, UserInput, IUserService>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Users> Create(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IUserService service, UserInput input) => null;
    [GraphQLIgnore]
    public override ResponseBase<Users> RemoveUser(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IUserService service) => null;
    // The inherited MutationBase.Delete checks only that the caller is signed in and then hard-deletes
    // whatever user id it is given. user_deleteUser below is an overload (int? userId), not an
    // override, so without this the base method surfaced as the root `delete(entityId: Int!)` field.
    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IUserService service, int entityId) => null;

    /*[GraphQLName("user_createUserInFirebase")]
    public ResponseStatus CreateUserInFirebase([Service] IUserService service, string email, string password)
    {
        return service.CreateUserInFirebase(email, password);
    }*/

    [GraphQLName("user_forgotPassword")]
    public async ValueTask<ResponseStatus> ForgotPassword([Service] IUserService service, string email)
    {
        return await service.ForgotPassword(email);
    }

    [GraphQLName("user_signUp")]
    public async Task<ResponseBase<Users>> SignUp(ClaimsPrincipal claims, [Service] IUserService service, [Service] IMembershipService membershipService, UserInput input
        , [Service] INotificationService notificationService, [Service] IHttpContextAccessor httpContextAccessor, [Service] IConfiguration configuration)
    {
        //var result = base.SignUp(claims, service, input);
        ResponseBase<Users> result = Utils.IsAuthenticated<Users>(claims);
        if (result.Status == ResponseStatus.AuthenticationFailed)
        {
            return result;
        }

        if (result.Status == ResponseStatus.Success)
        {
            return result; // ResponseBase<Users>.Failure(ResponseStatus.AlreadyExists);
        }

        if (result.Status == ResponseStatus.UserNotFound)
        {
            string externalId = claims.FindFirstValue("user_id");

            // Was a `dynamic` walk over the raw firebase claim wrapped in a bare catch, which hid
            // RuntimeBinderException and silently produced a null email on any shape change.
            string tokenEmail = FirebaseClaims.GetEmail(claims);

            if (string.IsNullOrEmpty(input.Email))
                input.Email = tokenEmail;

            // UserServiceBase.SignUp assigns `val.Email = email` last, so whatever is passed here
            // overwrites the address mapped from the input. Passing the token's address blindly
            // therefore threw away a perfectly good sign-up form value whenever the provider did
            // not return one -- a Facebook account with no verified email being the usual case.
            // The row was then stored with Email = null, and because nothing in the app can set an
            // address afterwards (the profile form has no email field), that account could never
            // start a subscription: MembershipService.CreateSubscription rejects a null email.
            string email = !string.IsNullOrEmpty(tokenEmail) ? tokenEmail : input.Email;

            result = service.SignUp(externalId, email, input);
        }
        else
        {
            return ResponseBase<Users>.Failure(ResponseStatus.UnknownError);
        }

        //Add free plan,...
        if (result.Status == ResponseStatus.Success)
        {
            membershipService.AddFreePlan(result.Result.Id);

            NotificationInput notificationInput = new()
            {
                NotificationType = NotificationType.FreePlanPurchased,
                UserId = result.Result.Id,
            };
            await notificationService.Send(notificationInput);

            var user = service.Get(result.Result.Id).Result;

            // Skipping the confirmation email is driven by the token, not by input.IsSocial: that
            // flag is client-supplied, so trusting it let any caller mark itself confirmed and
            // bypass email verification entirely.
            if (FirebaseClaims.IsEmailVerified(claims))
            {
                user.IsConfirmed = true;
                service.Update(user);
                return result;
            }

            user.ConfirmKey = Guid.NewGuid().ToString();
            service.Update(user);

            if (input.Email != null)
            {
                var content = File.ReadAllText(System.IO.Path.Combine("Data", "confirm-email.html"))
                    .Replace("[TermsAndConditionUrl]", configuration["TermsAndConditionUrl"])
                    .Replace("[User]", input.FullName ?? input.Email);
                    
                // Null-safe because this line is newly reachable: a Facebook sign-up used to
                // return above on input.IsSocial and never got here, so a client that omits
                // ConfirmUrl on its social path would now throw after the row and the free plan
                // were already created. No ConfirmUrl falls through to the API confirm link below.
                if (input.ConfirmUrl?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true) //web
                {
                    content = content.Replace("[Link]", input.ConfirmUrl + "?ConfirmKey=" + user.ConfirmKey);

                   await service.SendEmailByAmazonSesAsync(new EmailInput
                    {
                        Content = content,
                        Subject = "TherExNow email verification (do not reply)",
                        To = input.Email,
                    }, null);
                }
                else //mobile
                {
                    var request = httpContextAccessor.HttpContext.Request;
                    var confirmMethod = $"{request.Scheme}://{request.Host}{request.PathBase}/api/account/confirm-user?confirmKey={user.ConfirmKey}&returnUrl={input.ConfirmUrl}";
                    content = content.Replace("[Link]", confirmMethod);

                   await service.SendEmailByAmazonSesAsync(new EmailInput
                    {
                        Content = content,
                        Subject = "TherExNow email verification (do not reply)",
                        To = input.Email,
                    }, null);
                }
            }
        }
        return result;
    }

    [GraphQLName("user_updateUser")]
    public override ResponseBase<Users> Update(ClaimsPrincipal claims, [Service] IUserService service, UserInput input)
    {
        if (!claims.Identity.IsAuthenticated) return ResponseBase<Users>.Failure(ResponseStatus.AuthenticationFailed);
        var user = JsonConvert.DeserializeObject<Users>(claims.FindFirstValue("user"));
        if (user == null) return ResponseBase<Users>.Failure(ResponseStatus.UserNotFound);

        if (input.Id.HasValue && user.UserType != UserType.Admin)
            return ResponseStatus.NotAllowd;

        if (input.Id == null)
            input.Id = user.Id;
        return service.Update(input);
    }

    [GraphQLName("user_incrementQrCodeCount")]
    public async Task<ResponseBase<Users>> IncrementQrCodeCount(ClaimsPrincipal claims, [Service] IUserService service, [Service] INotificationService notificationService, [Service] IMembershipService membershipService)
    {
        if (!claims.Identity.IsAuthenticated) return ResponseBase<Users>.Failure(ResponseStatus.AuthenticationFailed);
        var user = JsonConvert.DeserializeObject<Users>(claims.FindFirstValue("user"));
        if (user == null) return ResponseBase<Users>.Failure(ResponseStatus.UserNotFound);

        if (!await membershipService.CheckMaxTryFreePlan(user.Id, NotificationType.MaxTryFreePlanForQrCode))
            return CustomResponseStatus.MaxTryFreePlanForQrCode;

        user.QrCodeCount++;
        return service.Update(user);
    }

    [GraphQLName("user_incrementPrintCount")]
    public async Task<ResponseBase<Users>> IncrementPrintCount(ClaimsPrincipal claims, [Service] IUserService service, [Service] INotificationService notificationService, [Service] IMembershipService membershipService)
    {
        if (!claims.Identity.IsAuthenticated) return ResponseBase<Users>.Failure(ResponseStatus.AuthenticationFailed);
        var user = JsonConvert.DeserializeObject<Users>(claims.FindFirstValue("user"));
        if (user == null) return ResponseBase<Users>.Failure(ResponseStatus.UserNotFound);

        if (!await membershipService.CheckMaxTryFreePlan(user.Id, NotificationType.MaxTryFreePlanForPrint))
            return CustomResponseStatus.MaxTryFreePlanForPrint;

        user.PrintCount++;
        return service.Update(user);
    }

    [GraphQLName("user_incrementEmailCount")]
    public async Task<ResponseBase<Users>> IncrementEmailCount(ClaimsPrincipal claims, [Service] IUserService service, [Service] INotificationService notificationService, [Service] IMembershipService membershipService)
    {
        if (!claims.Identity.IsAuthenticated) return ResponseBase<Users>.Failure(ResponseStatus.AuthenticationFailed);
        var user = JsonConvert.DeserializeObject<Users>(claims.FindFirstValue("user"));
        if (user == null) return ResponseBase<Users>.Failure(ResponseStatus.UserNotFound);

        if (!await membershipService.CheckMaxTryFreePlan(user.Id, NotificationType.MaxTryFreePlanForEmail))
            return CustomResponseStatus.MaxTryFreePlanForEmail;

        user.SentEmailCount++;
        return service.Update(user);
    }

    [GraphQLName("user_incrementSmsCount")]
    public async Task<ResponseBase<Users>> IncrementSmsCount(ClaimsPrincipal claims, [Service] IUserService service, [Service] INotificationService notificationService, [Service] IMembershipService membershipService)
    {
        if (!claims.Identity.IsAuthenticated) return ResponseBase<Users>.Failure(ResponseStatus.AuthenticationFailed);
        var user = JsonConvert.DeserializeObject<Users>(claims.FindFirstValue("user"));
        if (user == null) return ResponseBase<Users>.Failure(ResponseStatus.UserNotFound);

        if (!await membershipService.CheckMaxTryFreePlan(user.Id, NotificationType.MaxTryFreePlanForSms))
            return CustomResponseStatus.MaxTryFreePlanForSms;

        user.SentSmsCount ++;
        return service.Update(user);
    }

    [GraphQLName("user_deleteUser")]
    public ResponseBase Delete(ClaimsPrincipal claims, [Service] IUserService service, int? userId)
    {
        if (!claims.Identity.IsAuthenticated) return ResponseStatus.AuthenticationFailed;
        var user = JsonConvert.DeserializeObject<Users>(claims.FindFirstValue("user"));
        if (user == null) return ResponseStatus.UserNotFound;

        if (userId.HasValue && user.UserType != UserType.Admin)
            return ResponseStatus.NotAllowd;

        return service.Remove(userId ?? user.Id).Status;
    }

    [GraphQLName("user_sendEmail")]
    public async ValueTask<ResponseBase<ResponseStatus>> SendEmail(ClaimsPrincipal claims, [Service] IUserService service, EmailInput input
        , [Service] INotificationService notificationService
        , [Service] IMembershipService membershipService)
    {
        if (!claims.Identity.IsAuthenticated) return ResponseBase<ResponseStatus>.Failure(ResponseStatus.AuthenticationFailed);
        var user = JsonConvert.DeserializeObject<Users>(claims.FindFirstValue("user"));
        if (user == null) return ResponseBase<ResponseStatus>.Failure(ResponseStatus.UserNotFound);

        if (!await membershipService.CheckMaxTryFreePlan(user.Id, NotificationType.MaxTryFreePlanForEmail))
            return ResponseBase<ResponseStatus>.Failure(CustomResponseStatus.MaxTryFreePlanForEmail);

        return await service.SendEmailByAmazonSesAsync(input, user.Id);
    }

    [GraphQLName("user_sendEmailForSupport")]
    public async ValueTask<ResponseBase<ResponseStatus>> SendEmailForSupport(ClaimsPrincipal claims, [Service] IUserService service, EmailInput input)
    {
        return await service.SendEmailByAmazonSesAsync(input, null);
    }

    [GraphQLName("user_sendSms")]
    public async Task<ResponseBase<ResponseStatus>> SendSms(ClaimsPrincipal claims, [Service] IUserService service, SmsInput input
        , [Service] INotificationService notificationService
        , [Service] IMembershipService membershipService)
    {
        if (!claims.Identity.IsAuthenticated) return ResponseBase<ResponseStatus>.Failure(ResponseStatus.AuthenticationFailed);
        var user = JsonConvert.DeserializeObject<Users>(claims.FindFirstValue("user"));
        if (user == null) return ResponseBase<ResponseStatus>.Failure(ResponseStatus.UserNotFound);

        if (!await membershipService.CheckMaxTryFreePlan(user.Id, NotificationType.MaxTryFreePlanForSms))
            return ResponseBase<ResponseStatus>.Failure(CustomResponseStatus.MaxTryFreePlanForSms);

        return service.SendSms(input, user.Id);
    }

    [GraphQLName("user_confirmUser")]
    public ResponseBase<Users> ConfirmUser([Service] IUserService service, string confirmKey)
    {
        return service.ConfirmUser(confirmKey);
    }
    #endregion
}

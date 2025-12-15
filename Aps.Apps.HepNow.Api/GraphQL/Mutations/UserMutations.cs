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
            string email = null;
            try
            {
                email = ((dynamic)JsonConvert.DeserializeObject<object>(claims.FindFirstValue("firebase"))).identities.email[0];

                if (string.IsNullOrEmpty(input.Email))
                    input.Email = email;
            }
            catch (Exception)
            {
            }

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
            if (input.IsSocial == true)
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
                    
                if (input.ConfirmUrl.ToLower().StartsWith("http")) //web
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

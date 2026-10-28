using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(RequestMutations))]
public class RequestMutations : MutationBase<Requests, RequestInput, IRequestService, Users>
{
    #region functions
    [GraphQLName("request_createRequest")]
    public async Task<ResponseBase<Requests>> Create([Authentication] Authentication authentication
        , [Service] IRequestService service
        , [Service] IUserService userService
        , [Service] INotificationService notificationService
        , RequestInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        input.UserId = authentication.CurrentUser.Id;
        var response = service.Add(input);

        if (response.Status == ResponseStatus.Success)
        {
            var admins = userService.GetAll(a => a.UserType == UserType.Admin).Result.ToList();

            var userList = admins.ToList();
            userList.Add(authentication.CurrentUser);

            foreach(var user in userList)
            {
                NotificationInput notificationInput = new()
                {
                    RequestId = response.Result.Id,
                    NotificationType = NotificationType.RequestCreated,
                    UserId = user.Id,
                };
                await notificationService.Send(notificationInput);
            }

            // Order-confirmation emails. In-app notifications alone are not seen unless someone
            // opens the admin panel, so the business was missing purchases. Best-effort: a failed
            // send must never fail the order, so every send is guarded.
            await SendOrderEmails(service, userService, authentication.CurrentUser, admins, response.Result, input);
        }

        return response;
    }

    private static async Task SendOrderEmails(IRequestService service, IUserService userService
        , Users buyer, List<Users> admins, Requests order, RequestInput input)
    {
        try
        {
            var summaryHtml = service.BuildOrderSummaryHtml(input);

            var shipTo = string.Join(", ", new[] { order.Address, order.City, order.State, order.ZipCode, order.CountryCode }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

            var buyerName = string.IsNullOrWhiteSpace(buyer.FullName) ? buyer.Email : buyer.FullName;

            // Notify the business (every admin) that a purchase was made.
            var adminBody = $@"A new shop order has been placed.<br><br>
                Order #: {order.Id}<br>
                Customer: {buyerName} ({buyer.Email})<br>
                Ship to: {shipTo}<br><br>
                {summaryHtml}<br><br>
                Open the admin panel to accept or reject it: <a href='https://admin.therexnow.com'>admin.therexnow.com</a>";

            foreach (var admin in admins.Where(a => !string.IsNullOrWhiteSpace(a.Email)))
            {
                try
                {
                    await userService.SendEmailByAmazonSesAsync(new EmailInput
                    {
                        To = admin.Email,
                        Subject = $"New shop order #{order.Id} - TherExNow",
                        Content = adminBody
                    }, admin.Id);
                }
                catch { }
            }

            // Send the buyer a receipt so they have a record and know it is being processed.
            if (!string.IsNullOrWhiteSpace(buyer.Email))
            {
                try
                {
                    await userService.SendEmailByAmazonSesAsync(new EmailInput
                    {
                        To = buyer.Email,
                        Subject = $"We received your order #{order.Id} - TherExNow",
                        Content = $@"Hi {buyerName},<br><br>
                            Thanks for your order! We have received it and will let you know once it has been processed.<br><br>
                            Order #: {order.Id}<br>
                            Ship to: {shipTo}<br><br>
                            {summaryHtml}<br><br>
                            - The TherExNow Team"
                    }, buyer.Id);
                }
                catch { }
            }
        }
        catch { }
    }

    [GraphQLName("request_updateRequest")]
    public async Task<ResponseBase<Requests>> Update([Authentication] Authentication authentication
        , [Service] IRequestService service
        , [Service] INotificationService notificationService
        , RequestUpdateInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        var response = service.Update(input);
        if (response.Status == ResponseStatus.Success && (input.RequestStatus == RequestStatus.Accepted || input.RequestStatus == RequestStatus.Rejected))
        {
            NotificationInput notificationInput = new()
            {
                RequestId = response.Result.Id,
                NotificationType =  input.RequestStatus == RequestStatus.Accepted ? NotificationType.RequestAccepted : NotificationType.RequestRejected,
                UserId = response.Result.UserId,
            };
            await notificationService.Send(notificationInput);
        }

        return response;
    }

    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IRequestService service, int entityId) => null;
    #endregion
}

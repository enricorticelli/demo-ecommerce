using Account.Api.Contracts;

namespace Account.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var storeGroup = app.MapGroup(AccountRoutes.StoreBase).WithTags("Account");
        var adminGroup = app.MapGroup(AccountRoutes.AdminBase).WithTags("Account");

        storeGroup.MapStoreUserEndpoints();
        storeGroup.MapStoreProfileEndpoints();

        adminGroup.MapAdminUserEndpoints();
        adminGroup.MapAdminProfileEndpoints();
        adminGroup.MapAdminAdminEndpoints();
        adminGroup.MapAdminCustomerEndpoints();

        return app;
    }
}

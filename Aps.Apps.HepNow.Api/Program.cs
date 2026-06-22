var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServices(builder.Configuration);

var app = builder.Build();

var dbContext = app.Services.GetRequiredService<IDbContextFactory<DataContext>>();
DataInitializer.Initialize(dbContext.CreateDbContext(), true);

var isProd = builder.Configuration["env"] == "prd";
if (!isProd)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Example API V1");
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseGraphQLAltair();
app.UseWebSockets();
app.UseRouting();
app.UseCors("AllowAllOrigins");
app.UseAuthentication();
app.UseMiddleware<AuthMiddleware>();
app.UseAuthorization();

app.MapGraphQL();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}/{id?}");

app.Run();

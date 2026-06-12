var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServices(builder.Configuration);

var app = builder.Build();

var dbContext = app.Services.GetRequiredService<IDbContextFactory<DataContext>>();
DataInitializer.Initialize(dbContext.CreateDbContext(), true);


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Example API V1");
});

app.UseGraphQLAltair();
app.UseCors("AllowAllOrigins");
app.UseWebSockets();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<AuthMiddleware>();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL();
    //endpoints.MapControllers();
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller}/{action}/{id?}");
});

app.Run();

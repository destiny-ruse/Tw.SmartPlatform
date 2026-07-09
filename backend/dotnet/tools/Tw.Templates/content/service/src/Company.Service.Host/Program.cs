using Company.Service.HttpApi;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapOrders();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.Run();

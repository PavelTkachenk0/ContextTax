using ContextTax.Core;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => ContextTaxInfo.Tagline);
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();

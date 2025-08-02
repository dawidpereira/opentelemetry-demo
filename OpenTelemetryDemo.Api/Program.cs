using OpenTelemetryDemo.ServiceDefaults;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<OpenTelemetryDemo.Api.Services.TelemetryService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "OpenTelemetry Demo API",
            Version = "v1",
            Description = "A demo API showcasing OpenTelemetry integration with .NET 9, Serilog, and Aspire",
            Contact = new()
            {
                Name = "OpenTelemetry Demo",
                Url = new Uri("https://github.com/opentelemetry")
            }
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("OpenTelemetry Demo API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();

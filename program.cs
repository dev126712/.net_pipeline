using System.Diagnostics;
using System.Net;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/status", () =>
{
    var assembly = Assembly.GetExecutingAssembly();
    
    var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0-dev";

    var process = Process.GetCurrentProcess();

    var status = new
    {
        ApplicationName = "DevOpsDemoApi",
        Version = informationalVersion,
        Environment = app.Environment.EnvironmentName,
        HostName = Dns.GetHostName(),
        ServerTime = DateTime.UtcNow.ToString("O"),
        ProcessId = process.Id,
        Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        ConfigurationMarker = app.Configuration["Deployment:Marker"] ?? "Marker_Not_Set" 
    };

    app.Logger.LogInformation("Status endpoint accessed. Version: {Version}, Environment: {Environment}", 
        status.Version, status.Environment);

    return Results.Ok(status);
})
.WithName("GetStatus")
.WithOpenApi()
.Produces<object>(StatusCodes.Status200OK);

app.Run();

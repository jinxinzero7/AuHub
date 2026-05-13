using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using AuHub.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot()
    .AddDelegatingHandler<ForwardAuthHeaderHandler>(true);

var app = builder.Build();

await app.UseOcelot();

app.Run();

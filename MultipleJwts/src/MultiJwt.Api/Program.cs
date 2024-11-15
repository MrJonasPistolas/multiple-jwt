using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MultiJwt.Api.Extensions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Security.Claims;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithAuth(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.Audience = builder.Configuration["Authentication:Keycloak:Audience"];
        options.MetadataAddress = builder.Configuration["Authentication:Keycloak:MetadataAddress"]!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Authentication:Keycloak:ValidIssuer"]
        };
    });

builder.Services.AddAuthorization();

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MultiJwt.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        tracing.AddOtlpExporter();
    });

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("users/me", (ClaimsPrincipal claimsPrincipal) =>
{
    return claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
}).RequireAuthorization();

app.UseAuthentication();

app.UseAuthorization();

app.Run();
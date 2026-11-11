using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TowerBridge.API.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddTowerBridgeService(builder.Configuration.GetSection("TowerBridge").Bind);
builder.Services.AddOpenApi(options =>
{
    // Direction is serialised as a string (JsonStringEnumConverter), but the OpenAPI generator
    // emits enums without a type, which makes generated clients treat them as untyped values.
    options.AddSchemaTransformer((schema, context, _) =>
    {
        if (context.JsonTypeInfo.Type.IsEnum)
            schema.Type = JsonSchemaType.String;
        return Task.CompletedTask;
    });
});

var app = builder.Build();
app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/api/bridgelifts", async (ITowerBridgeService service) =>
{
    return await service.GetAllAsync();
}).WithTags("BridgeLifts");

app.MapGet("/api/bridgelifts/next", async (ITowerBridgeService service) =>
{
    return await service.GetNextAsync();
}).WithTags("BridgeLifts");
app.MapGet("/api/bridgelifts/today", async (ITowerBridgeService service) =>
{
    return await service.GetTodayAsync();
}).WithTags("BridgeLifts");

app.Run();

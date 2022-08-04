using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TowerBridge.API.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTowerBridgeService(builder.Configuration.GetSection("TowerBridge").Bind);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGenNewtonsoftSupport();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/bridgelifts", async (ITowerBridgeService service) =>
{
    return await service.GetAllAsync();
}).WithTags("BridgeLifts");

app.MapGet("/bridgelifts/next", async (ITowerBridgeService service) =>
{
    return await service.GetNextAsync();
}).WithTags("BridgeLifts");

app.Run();
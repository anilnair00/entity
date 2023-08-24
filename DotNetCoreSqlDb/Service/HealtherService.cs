﻿
using DotNetCoreSqlDb.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotNetCoreSqlDb.Service;

public sealed class HealtherService : BackgroundService
{
    private readonly CoreDbContext _context;

    public HealtherService(CoreDbContext context)
    {
        _context = context;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var eTrade = new ETrade();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int delay = 1000 * 60 * 5;
                DateTimeOffset now = (DateTimeOffset)Help.GetEstDatetime();
                var heathCheckResponse = eTrade.HealthCheck(_context, "SPY", now);

                Console.WriteLine($"Heathcheck: {heathCheckResponse}");

                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " :: EST Time:" + Help.GetEstDatetime());
            }
        }
    }
}
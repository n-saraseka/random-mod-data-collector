using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using random_mod_data_collector;

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appconfig.json");
var configuration = configurationBuilder.Build();
var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
{
    TokenLimit = 1,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    QueueLimit = 60,
    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
    TokensPerPeriod = 1,
    AutoReplenishment = true
});

var dataProcessor = new DataProcessor(configuration, rateLimiter);

var data = await dataProcessor.ProcessData();
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using random_mod_data_collector.Entities;
namespace random_mod_data_collector;

public class DataProcessor
{
    private static OsuApiService _service;
    private static IConfiguration _configuration;
    private static readonly Random Rng = new();
    private static int[] BeatmapIds;

    public DataProcessor(IConfiguration configuration, RateLimiter rateLimiter)
    {
        _service = new OsuApiService(configuration, rateLimiter);
        _configuration = configuration;
        BeatmapIds = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(_configuration["BeatmapIdsPath"]));
    }
    
    public async Task<List<BeatmapDifficultyData>> ProcessData()
    {
        var allData = new List<BeatmapDifficultyData>();
        var beatmapIndex = 0;
        var id = 0;
        
        for (var i = 0; i < 10; i++)
        {
            beatmapIndex = Rng.Next(0, BeatmapIds.Length);
            id = BeatmapIds[beatmapIndex];
            // rerun RNG in case this beatmap has been analyzed already
            while (allData.FirstOrDefault(d => d.Id == id) != null)
            {
                beatmapIndex = Rng.Next(0, BeatmapIds.Length);
                id = BeatmapIds[beatmapIndex];
            }

            try
            {
                var beatmap = await _service.GetScoreBeatmapAsync(id);
                var flatWorkingBeatmap = new FlatWorkingBeatmap(beatmap);
                var baseAttributes = Calculator.GetBaseDifficultyAttributes(flatWorkingBeatmap);
                for (var j = 0; j < 10; j++)
                {
                    var seed = Rng.Next(Int32.MinValue, Int32.MaxValue);
                    var angleSharpness = (float)Rng.NextDouble() * (10 - 1) + 1;
                    var difficultyAttributes = Calculator.GetRandomDifficultyAttributes(flatWorkingBeatmap, seed, angleSharpness);
                    
                    var difficultyData = new BeatmapDifficultyData()
                    {
                        Id = id,
                        BaseDifficulty = baseAttributes.StarRating,
                        Seed = seed,
                        AngleSharpness = angleSharpness,
                        NewDifficulty = difficultyAttributes.StarRating
                    };
                    allData.Add(difficultyData);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Some kind of exception happened while calculating difficulty attributes. Likely wrong ruleset");
                Console.WriteLine($"Exception: {exception.Message}");
            }
        }
        return allData;
    }
}
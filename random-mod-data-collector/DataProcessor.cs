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
        
        for (var i = 0; i < 100; i++)
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
                for (var j = 0; j < 100; j++)
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
        Console.WriteLine($"Loaded all data");
        return allData;
    }

    public void ImportToCsv(List<BeatmapDifficultyData> data)
    {
        var output = File.CreateText(_configuration["OutputPath"]);
        output.WriteLine("id, seed, angle_sharpness, base_difficulty, new_difficulty");
        foreach (var line in data)
            output.WriteLine($"{line.Id}, " +
                             $"{line.Seed}, " +
                             $"{Utils.ToPointDecimalString(line.AngleSharpness)}, " +
                             $"{Utils.ToPointDecimalString(line.BaseDifficulty)}, " +
                             $"{Utils.ToPointDecimalString(line.NewDifficulty)}");
        output.Close();
        Console.WriteLine($"Imported all data to {_configuration["OutputPath"]}");
    }
}
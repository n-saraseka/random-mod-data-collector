using System.Diagnostics;
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
    private static int BeatmapsPerRun;
    private static int SeedsPerBeatmap;
    private static int[] BeatmapIds;

    /// <summary>
    /// Create a DataProcessor object
    /// </summary>
    /// <param name="configuration">An IConfiguration</param>
    /// <param name="rateLimiter">A RateLimiter</param>
    public DataProcessor(IConfiguration configuration, RateLimiter rateLimiter)
    {
        _service = new OsuApiService(configuration, rateLimiter);
        _configuration = configuration;
        BeatmapIds = JsonConvert.DeserializeObject<int[]>(File.ReadAllText(_configuration["BeatmapIdsPath"]));
        BeatmapsPerRun = int.Parse(_configuration["BeatmapsPerRun"]);
        SeedsPerBeatmap = int.Parse(_configuration["SeedsPerBeatmap"]);
    }
    
    /// <summary>
    /// Process beatmap data
    /// </summary>
    /// <returns>A list with populated BeatmapDifficultyData objects, grouped per beatmap ID</returns>
    public async Task<List<BeatmapDifficultyData>> ProcessDataAsync()
    {
        var allData = new List<BeatmapDifficultyData>();
        var beatmapIndex = 0;
        var id = 0;
        var dataTasks = new List<Task<List<BeatmapDifficultyData>>>();
        
        for (var i = 0; i < BeatmapsPerRun; i++)
        {
            beatmapIndex = Rng.Next(0, BeatmapIds.Length);
            id = BeatmapIds[beatmapIndex];
            // rerun RNG in case this beatmap has been analyzed already
            while (allData.FirstOrDefault(d => d.Id == id) != null)
            {
                beatmapIndex = Rng.Next(0, BeatmapIds.Length);
                id = BeatmapIds[beatmapIndex];
            }

            dataTasks.Add(CalculateDifficultyDataAsync(id));
        }
        
        await Task.WhenAll(dataTasks);
        foreach (var data in dataTasks)
            allData.AddRange(data.Result);
        
        Trace.WriteLine($"Loaded all data");
        return allData;
    }
    
    /// <summary>
    /// Calculate difficulty data for beatmap
    /// </summary>
    /// <param name="id">Beatmap ID</param>
    /// <returns>A list with populated BeatmapDifficultyData objects</returns>
    private async Task<List<BeatmapDifficultyData>> CalculateDifficultyDataAsync(int id)
    {
        var data = new List<BeatmapDifficultyData>();

        try
        {
            var beatmap = await _service.GetScoreBeatmapAsync(id);
            if (beatmap.BeatmapInfo.Ruleset.ShortName == "osu")
            {
                Trace.WriteLine($"Calculating data for beatmap {id}");
                
                var flatWorkingBeatmap = new FlatWorkingBeatmap(beatmap);
                var baseAttributes = Calculator.GetBaseDifficultyAttributes(flatWorkingBeatmap);
        
                for (var j = 0; j < SeedsPerBeatmap; j++)
                {
                    var seed = Rng.Next(Int32.MinValue, Int32.MaxValue);
                    var angleSharpness = (float)(1 + (9.0 / (SeedsPerBeatmap - 1)) * j);
                    var difficultyAttributes = Calculator.GetRandomDifficultyAttributes(flatWorkingBeatmap, seed, angleSharpness);
                    
                    var difficultyData = new BeatmapDifficultyData()
                    {
                        Id = id,
                        BaseDifficulty = baseAttributes.StarRating,
                        Seed = seed,
                        AngleSharpness = angleSharpness,
                        NewDifficulty = difficultyAttributes.StarRating
                    };
                    data.Add(difficultyData);
                }
            }
            else
                Trace.WriteLine($"Beatmap {id} doesn't have the osu! ruleset, skipped");
        }
        catch (Exception exception)
        {
            Trace.WriteLine($"An exception occured while calculating difficulty data: {exception.Message}");
        }

        return data;
    }

    /// <summary>
    /// Import a list of BeatmapDifficultyData to a .csv file
    /// </summary>
    /// <param name="data">List with populated BeatmapDifficultyData objects</param>
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
        Trace.WriteLine($"Imported all data to {_configuration["OutputPath"]}");
    }
}
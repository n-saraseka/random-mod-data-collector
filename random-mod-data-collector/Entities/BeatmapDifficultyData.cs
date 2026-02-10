namespace random_mod_data_collector.Entities;

public class BeatmapDifficultyData
{
    public int Id { get; set; }
    public int Seed { get; set; }
    public float AngleSharpness {get; set;}
    public double BaseDifficulty { get; set; }
    public double NewDifficulty { get; set; }
}
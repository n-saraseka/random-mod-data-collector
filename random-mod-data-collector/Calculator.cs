using osu.Game.Online.API;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;

namespace random_mod_data_collector;

public class Calculator
{
    private static readonly OsuRuleset Ruleset = new OsuRuleset();
    /// <summary>
    /// Create a Mod object that contains the Random mod data
    /// </summary>
    /// <param name="seed">Seed</param>
    /// <param name="angleSharpness">Angle sharpness (between 1 and 10)</param>
    /// <returns>Populated mod object</returns>
    private static Mod CreateModRandom(int seed, float angleSharpness)
    {
        angleSharpness = Math.Clamp(angleSharpness, 1, 10);
        var modData = new APIMod();
        modData.Acronym = "RD";
        modData.Settings["seed"] = seed;
        modData.Settings["angle_sharpness"] = angleSharpness;
        var mod = modData.ToMod(Ruleset);
        return mod;
    }
    /// <summary>
    /// Get DifficultyAttributes for beatmap with the Random mod
    /// </summary>
    /// <param name="beatmap">A FlatWorkingBeatmap used for the DifficultyCalculator</param>
    /// <param name="seed">Seed</param>
    /// <param name="angleSharpness">Angle sharpness (between 1 and 10)</param>
    /// <returns></returns>
    public static DifficultyAttributes GetRandomDifficultyAttributes(FlatWorkingBeatmap beatmap, int seed, float angleSharpness)
    {
        
        var mods = new Mod[1];
        mods[0] = CreateModRandom(seed, angleSharpness);
        
        var difficultyCalculator = Ruleset.CreateDifficultyCalculator(beatmap);
        return difficultyCalculator.Calculate(mods);
    }

    /// <summary>
    /// Get DifficultyAttributes for beatmap with no mods
    /// </summary>
    /// <param name="beatmap">A FlatWorkingBeatmap used for the DifficultyCalculator</param>
    /// <returns></returns>
    public static DifficultyAttributes GetBaseDifficultyAttributes(FlatWorkingBeatmap beatmap)
    {
        var difficultyCalculator = Ruleset.CreateDifficultyCalculator(beatmap);
        return difficultyCalculator.Calculate();
    }
}
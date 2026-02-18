For builds of this program to work properly, an appconfig.json needs to be placed in the build's folder. Here's a breakdown of all keys that the appconfig file should contain:
Key | Value
--- | --- 
BeatmapIdsPath | "BeatmapIds/all.json" 
BaseApiUrl | "https://osu.ppy.sh/api/v2" 
ApiTokenUrl | "https://osu.ppy.sh/oauth/token"
ApiVersion | 20220705 or higher
ApiId | [OAuth API client ID](https://osu.ppy.sh/home/account/edit)
ApiSecret | [OAuth API client secret](https://osu.ppy.sh/home/account/edit)
OutputPath | Output file path
BeatmapsPerRun | Amount of beatmaps to process per run
SeedsPerBeatmap | Amount of random different random settings per beatmap

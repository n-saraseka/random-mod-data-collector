using Newtonsoft.Json;
namespace random_mod_data_collector.Entities;

public class TokenInfo
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }
    [JsonProperty("expires_in")]
    public long ExpiresIn { get; set; }
}
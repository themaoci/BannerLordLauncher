using Newtonsoft.Json;

namespace BannerLordLauncher
{
    public class AppConfig
    {
        [JsonProperty("gamePath")]
        public string GamePath { get; set; }
    }
}
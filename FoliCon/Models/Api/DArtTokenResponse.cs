namespace FoliCon.Models.Api;

public class DArtTokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("token_type")]
    public string TokenType { get; set; }

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty("error")]
    public string ErrorType { get; set; }

    [JsonProperty("error_description")]
    public string ErrorDescription { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
}
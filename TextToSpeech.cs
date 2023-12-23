using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace System.Runtime.CompilerServices
{
  internal static class IsExternalInit { }
}

namespace OuterWildsCompanion
{
  public class TextToSpeech
  {
    public record TextToSpeechRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("voice")] string Voice,
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("speed")] double Speed = 1.0,
        [property: JsonPropertyName("response_format")] string ResponseFormat = "mp3");

    public static async Task<byte[]> GetVoiceConversionAsync(TextToSpeechRequest textToSpeechRequest, string token)
    {
      HttpClient client = new HttpClient();
      string textToSpeechEndpoint = "https://api.openai.com/v1/audio/speech";
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
      HttpResponseMessage result = await client.PostAsJsonAsync(textToSpeechEndpoint, textToSpeechRequest);
      var responseBytes = await result.Content.ReadAsByteArrayAsync();
      return responseBytes;
    }
  }
}

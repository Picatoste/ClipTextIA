using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClipTextIA
{
  public class OpenRouterService : IPromptService
  {
    private static readonly HttpClient client = new HttpClient();
    private const string ErrorTag = "[$ERROR$]";

    public async Task<string> ImproveTextAsync(string apiKey, string apiUrl, string prompt, string text)
    {
      var requestBody = new
      {
        model = "deepseek-chat",
        messages = new[]
        {
          new { role = "system", content = prompt },
          new { role = "user", content = text }
        }
      };

      var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
      content.Headers.Add("Authorization", $"Bearer {apiKey}");

      try
      {
        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
          Content = content
        };
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await client.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
          try
          {
            using var errorDoc = JsonDocument.Parse(responseJson);
            var errorMessage = errorDoc.RootElement
                .GetProperty("error")
                .GetProperty("message")
                .GetString();

            return $"{ErrorTag} {errorMessage}";
          }
          catch
          {
            return $"{ErrorTag} Código {response.StatusCode} - {response.ReasonPhrase}";
          }
        }

        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
      }
      catch (Exception ex)
      {
        return $"{ErrorTag} {ex.Message}";
      }
    }
  }
}

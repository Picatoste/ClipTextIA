using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace ClipTextIA
{
    public class GeminiService : IGeminiService
    {
        private static readonly HttpClient client = new HttpClient();
        private const string ApiKey = "AIzaSyD_rubWAJPc7ycv0DjvVYTD5ojGeUg4V_Y"; // <-- Sustituir por tu clave
        private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=" + ApiKey;

        public async Task<string> ImproveTextAsync(string prompt, string text)
        {
            var requestBody = new
            {
                contents = new[]
                {
                new {
                    parts = new[]
                    {
                        new { text = prompt + "\n\n" + text }
                    }
                }
            }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(Endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }
    }

    // HotkeyModifiers.cs
    public enum HotkeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }


}
﻿using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace ClipTextIA
{
  public class GeminiService : IPromptService
  {
    private static readonly HttpClient client = new HttpClient();
    private const string ErrorTag = "[$ERROR$]";
    public async Task<string> ImproveTextAsync(string apiKey, string apiUrl, string prompt, string text)
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

      try
      {
        var response = await client.PostAsync($"{apiUrl}:generateContent?key={apiKey}", content);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
          // Intenta extraer el mensaje de error del JSON si está presente
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
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();
      }
      catch (Exception ex)
      {
        return $"{ErrorTag} {ex.Message}";
      }
    }
  }
}
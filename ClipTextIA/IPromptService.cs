using System.Threading.Tasks;

namespace ClipTextIA
{
  public interface IPromptService
  {
    Task<string> ImproveTextAsync(string apiKey, string apiUrl, string prompt, string text);
  }
}
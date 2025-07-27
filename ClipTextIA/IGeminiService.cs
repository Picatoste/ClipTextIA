using System.Threading.Tasks;

namespace ClipTextIA
{
    public interface IGeminiService
    {
        Task<string> ImproveTextAsync(string prompt, string text);
    }
}
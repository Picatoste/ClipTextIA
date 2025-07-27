using System.Threading.Tasks;

namespace ClipTextIA
{
    public interface IClipboardHelper
    {
        Task<string> GetTextAsync();
        Task SetTextAsync(string text);
    }
}
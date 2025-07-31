using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
namespace ClipTextIA
{
  public class ClipboardHelper : IClipboardHelper
  {
    public async Task<string> GetTextAsync()
    {
      var content = Clipboard.GetContent();
      if (content.Contains(StandardDataFormats.Text))
      {
        return await content.GetTextAsync();
      }
      return string.Empty;
    }

    public async Task SetTextAsync(string text)
    {
      var dataPackage = new DataPackage();
      dataPackage.SetText(text);
      Clipboard.SetContent(dataPackage);
    }
  }
}
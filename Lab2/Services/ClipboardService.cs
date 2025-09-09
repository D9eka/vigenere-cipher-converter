using System;
using Windows.ApplicationModel.DataTransfer;

namespace Lab2.Services
{
    public interface IClipboardService
    {
        string Paste();
        void Copy(string text);
    }

    public class ClipboardService : IClipboardService
    {
        public string Paste()
        {
            var dataPackage = Clipboard.GetContent();
            if (dataPackage.Contains(StandardDataFormats.Text))
            {
                return dataPackage.GetTextAsync().GetAwaiter().GetResult();
            }
            return string.Empty;
        }

        public void Copy(string text)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }
    }
}

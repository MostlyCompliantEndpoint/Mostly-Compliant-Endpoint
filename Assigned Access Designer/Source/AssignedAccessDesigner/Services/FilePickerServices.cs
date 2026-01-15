using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage.Pickers;
using WinRT;
using WinRT.Interop;


namespace AssignedAccessDesigner.Services
{
    public static class FilePickerService
    {
        public static async Task<string?> PickXmlAsync(Window window)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".xml");
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }

        public static async Task<string?> PickExeAsync(Window window)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }

        public static async Task<string?> PickLnkAsync(Window window)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".lnk");
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }

        public static async Task<IReadOnlyList<string>> PickMultipleXmlAsync(Window window)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".xml");
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);
            var files = await picker.PickMultipleFilesAsync();
            return files.Select(f => f.Path).ToList();
        }

        public static async Task<string?> SaveXmlAsync(Window window, string defaultDir, string defaultName)
        {

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), // UTF-8, no BOM (common for configs)
                Indent = true,
                OmitXmlDeclaration = false                                          // <-- do not omit
            };

            var picker = new FileSavePicker();
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker,hwnd);
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("XML", new List<string> { ".xml" });
            picker.SuggestedFileName = defaultName;
            var file = await picker.PickSaveFileAsync();

            return file?.Path;
        }
    }
}
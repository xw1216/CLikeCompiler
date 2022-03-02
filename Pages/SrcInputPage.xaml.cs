using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CLikeCompiler.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SrcInputPage : Page
    {
        public SrcInputPage()
        {
            this.InitializeComponent();
        }

        private void CloseCheckPane(object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = false;
        }

        private void ToggleCheckPane(object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }

        private async void OpenCodeFile(object sender, RoutedEventArgs e)
        {
            Windows.Storage.Pickers.FileOpenPicker picker = new();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            picker.FileTypeFilter.Add(".txt");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.mainPage);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                codeBox.Text = await Windows.Storage.FileIO.ReadTextAsync(file);
            } else
            {
                MainWindow.mainPage.ShowErrorPage("无法打开代码文件");
            }
        }

        private void CheckPassJumpBtnClick(object sender, RoutedEventArgs e)
        {
            infoBar.Visibility = Visibility.Collapsed;
            MainWindow.mainPage.PageTagNavigation("MidCodePage");
        } 
    }

    
}

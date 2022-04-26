using CLikeCompiler;
using CLikeCompiler.Libs;
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

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.GetInstance());
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            
            if (file != null)
            {
                var folder = await file.GetParentAsync();
                MainWindow.GetInstance().server.SetRootPath(folder.Path);
                codeBox.Text = await Windows.Storage.FileIO.ReadTextAsync(file);
            } else
            {
                MainWindow.GetInstance().SetDefaultRootPath();
                MainWindow.GetInstance().ShowErrorPage("未能打开代码文件");
            }
        }

        private void CheckPassJumpBtnClick(object sender, RoutedEventArgs e)
        {
            if (infoBar.Severity == InfoBarSeverity.Error)
            {
                MainWindow.GetInstance().PageTagNavigation("LogPage");
            } else
            {
                MainWindow.GetInstance().PageTagNavigation("MidCodePage");
            }
            infoBar.IsOpen = false;
        }

        private void StartCompileClick(object sender, RoutedEventArgs e)
        {
            string src = codeBox.Text;
            CheckCodeEmpty(src);
            if(MainWindow.GetInstance().server.StartCompile(ref src, codeBox))
            {
                CompileSuccessHandler();
                
            } else
            {
                CompileErrorDetectedHandler();
            }
        }

        private void CompileErrorDetectedHandler()
        {
            infoBar.Severity = InfoBarSeverity.Error;
            infoBar.Message = "编译发生错误，请前往日志页查看相关信息";
            splitView.Visibility = Visibility.Visible;
            splitView.IsPaneOpen = true;
            infoBar.IsOpen = true;
        }

        private void CompileSuccessHandler()
        {
            infoBar.Severity = InfoBarSeverity.Success;
            infoBar.Message = "编译成功，可以查看中间代码了";
            infoBar.IsOpen = true;
        }

        private void CheckCodeEmpty(string code)
        {
            if (code.Trim().Length < 1)
            {
                MainWindow.GetInstance().ShowErrorPage("请勿置空或输入全为空白字符。", "源码出错");
            }
        } 

        public string GetInputSrc()
        {
            return codeBox.Text;
        }

        public void SetInputSrc(string src)
        {
            codeBox.Text = src;
        }

        private void ClearActionRecordClick(object sender, RoutedEventArgs e)
        {
            Logger.ClearActionRecord();
        }

        private void TestActionRecordClick(object sender, RoutedEventArgs e)
        {
            Logger.ActionRecordTest();
        }

        private void SingleStepClick(object sender, RoutedEventArgs e)
        {

        }
    }

    

    
}

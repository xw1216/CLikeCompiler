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
using CLikeCompiler.Libs.Util;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CLikeCompiler.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SrcInputPage : Page
    {
        private Logger logger = Logger.Instance();

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

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance());
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            
            if (file != null)
            {
                var folder = await file.GetParentAsync();
                MainWindow.Instance().server.SetRootPath(folder.Path);
                codeBox.Text = await Windows.Storage.FileIO.ReadTextAsync(file);
            } else
            {
                MainWindow.Instance().SetDefaultRootPath();
                MainWindow.Instance().ShowNotifyPage("未能打开代码文件");
            }
        }

        private void CheckPassJumpBtnClick(object sender, RoutedEventArgs e)
        {
            if (infoBar.Severity == InfoBarSeverity.Error)
            {
                MainWindow.Instance().PageTagNavigation("LogPage");
            } else
            {
                MainWindow.Instance().PageTagNavigation("MidCodePage");
            }
            infoBar.IsOpen = false;
        }

        private void StartCompileClick(object sender, RoutedEventArgs e)
        {
            string src = codeBox.Text;
            CheckCodeEmpty(src);
            logger.ClearActionRecord();
            try
            {
                if (MainWindow.Instance().server.StartCompile(ref src, codeBox))
                {
                    CompileSuccessHandler();

                }
                else
                {
                    CompileErrorDetectedHandler();
                }
            }
            catch (Exception)
            {
                CompileErrorDetectedHandler();
                Compiler.ResetCompiler();
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
                MainWindow.Instance().ShowNotifyPage("请勿置空或输入全为空白字符。", "源码出错");
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
            logger.ClearActionRecord();
        }

        private void TestActionRecordClick(object sender, RoutedEventArgs e)
        {
            logger.ActionRecordTest();
        }
    }

    

    
}

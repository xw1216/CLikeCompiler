using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

using CLikeCompiler.Libs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CLikeCompiler.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LogPage : Page
    {
        public LogPage()
        {
            this.InitializeComponent();
        }

        private void MoreLogClick(object sender, RoutedEventArgs e)
        {
            LogUtility.GetInstance().OpenLogInNotepad();
            MainWindow.GetInstance().ShowErrorPage("若要继续，请先关闭日志文件。", "提示");
        }

        private void ClearLogClick(object sender, RoutedEventArgs e)
        {
            LogUtility.GetInstance().ClearDisplayRecord();
        }

        private void TestLogClick(object sender, RoutedEventArgs e)
        {
            CompilerReportArgs args = new(LogItem.MsgType.WARN, "Test");
            CompilerReportArgs arg = new(LogItem.MsgType.ERROR, "Test");
            MainWindow.GetInstance().GetLogger().NewLogRecord("Test", LogItem.MsgType.INFO);
            MainWindow.GetInstance().server.ReportBackInfo(this, args);
            MainWindow.GetInstance().server.ReportFrontInfo(this, arg);
        }
    }
}

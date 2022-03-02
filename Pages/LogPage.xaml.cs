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
            AddLogItem();
        }

        private void AddLogItem()
        {
            LogUtility logger =  MainWindow.mainPage.GetLogger();
            logger.NewLogRecord("Test1", LogItem.MsgType.INFO);
            logger.NewLogRecord("Test2", LogItem.MsgType.INFO);
            logger.NewLogRecord("Test3", LogItem.MsgType.INFO);
        }

        private void MoreLogClick(object sender, RoutedEventArgs e)
        {
            LogUtility.logger.OpenLogInNotepad();
            MainWindow.mainPage.ShowErrorPage("若要继续，请先关闭日志文件。", "提示");
            
        }

        private void ClearLogClick(object sender, RoutedEventArgs e)
        {
            LogUtility.logger.ClearDisplayRecord();
        }
    }
}

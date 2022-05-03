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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

using CLikeCompiler.Libs;
using CLikeCompiler.Libs.Util;
using CLikeCompiler.Libs.Util.LogItem;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CLikeCompiler.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LogPage : Page
    {
        private readonly Logger logger = Logger.Instance();
        public ObservableCollection<LogMsgItem> LogDisplay => logger.LogDisplayed;
        public int PageCount { get; private set; } = 1;

        private ObservableCollection<LogMsgItem> LogPageDisplay { get; } = new();

        private int pageIndex;
        public int PageIndex
        {
            get => pageIndex;
            set
            {
                PageIndexChangeHandler(value);
                pageIndex = value;
            }
        }

        public LogPage()
        {
            this.InitializeComponent();
            logger.LogChange += NewLogHandler;
            logger.LogClear += ClearLogHandler;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            LogPageDisplay.Clear();
            base.OnNavigatedTo(e);
            UpdatePageCount();
            PageIndexChangeHandler(pageIndex);
        }

        private void NewLogHandler(LogMsgItem item)
        {
            UpdatePageCount();
            if (LogPageDisplay.Count < Logger.LogDisplayPageCnt)
            {
                LogPageDisplay.Add(item);
            }
        }

        private void UpdatePageCount()
        {
            int page = logger.LogPages;
            LogPager.NumberOfPages = (page == 0) ? 1 : page;
        }

        private void ClearLogHandler()
        {
            LogPageDisplay.Clear();
            LogPager.NumberOfPages = 1;
        }

        private void PageIndexChangeHandler(int index)
        {
            int total = logger.LogDisplayed.Count;
            int startIndex = index * Logger.LogDisplayPageCnt;
            int endIndex = startIndex + Logger.LogDisplayPageCnt;
            if (startIndex >= total)
            {
            } else if (endIndex > total)
            {
                LogPageDisplay.Clear();
                for (int i = startIndex; i < total; i++)
                {
                    LogPageDisplay.Add(LogDisplay.ElementAt(i));
                }
            }
            else
            {
                LogPageDisplay.Clear();
                for (int i = startIndex; i < endIndex; i++)
                {
                    LogPageDisplay.Add(LogDisplay.ElementAt(i));
                }
            }
        }

        private void MoreLogClick(object sender, RoutedEventArgs e)
        {
            Logger.Instance().OpenLogInNotepad();
            MainWindow.GetInstance().ShowErrorPage("若要继续，请先关闭日志文件。", "提示");
        }

        private void ClearLogClick(object sender, RoutedEventArgs e)
        {
            Logger.Instance().ClearDisplayLog();
        }

        private void TestLogClick(object sender, RoutedEventArgs e)
        {
            LogReportArgs args = new(LogMsgItem.Type.WARN, "Test");
            LogReportArgs arg = new(LogMsgItem.Type.ERROR, "Test");
            MainWindow.GetInstance().GetLogger().NewLogRecord("Test", LogMsgItem.Type.INFO);
            MainWindow.GetInstance().server.ReportBackInfo(this, args);
            MainWindow.GetInstance().server.ReportFrontInfo(this, arg);
        }

    }
}

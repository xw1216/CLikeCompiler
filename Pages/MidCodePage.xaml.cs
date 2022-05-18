using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CLikeCompiler.Libs;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Unit.Quads;
using CLikeCompiler.Libs.Util;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CLikeCompiler.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MidCodePage : Page
    {
        private readonly QuadTable quadTable = Compiler.quadTable;
        public readonly ObservableCollection<Quad> quadList = new();
        private readonly TargetWriter writer = MainWindow.Instance().writer;

        private bool isPaneOpen = false;

        private const int PageDisplayCnt = 15;
        private int PageCnt { get; set; } = 1;

        private int pageIndex = 0;
        public int PageIndex
        {
            get => pageIndex;
            set
            {
                IndexChangeHandler(value);
                pageIndex = value;
            }
        }

        public MidCodePage()
        {
            this.InitializeComponent();
            quadTable.NewQuadEvent += NewQuadHandler;
            quadTable.ClearQuadEvent += QuadClearHandler;
            Compiler.Instance().CodeTableChange += UpdateMidCodeFile;
            UpdateMidCodeFile();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            quadList.Clear();
            base.OnNavigatedTo(e);
            UpdatePageCnt();
            IndexChangeHandler(pageIndex);
        }

        private void NewQuadHandler(Quad quad)
        {
            UpdatePageCnt();
            if (quadList.Count < PageDisplayCnt)
            {
                quadList.Add(quad);
            }
        }

        private void UpdatePageCnt()
        {
            PageCnt = quadTable.Count / PageDisplayCnt;
            PageCnt = (quadTable.Count % PageDisplayCnt == 0) ? PageCnt : PageCnt + 1;
            QuadPager.NumberOfPages = (PageCnt == 0) ? 1 : PageCnt;
        }

        private void IndexChangeHandler(int index)
        {
            int start = index * PageDisplayCnt;
            int end = start + PageDisplayCnt ;
            List<Quad> list = quadTable.GetQuadBetween(start, end);
            quadList.Clear();
            foreach (Quad quad in list)
            {
                quadList.Add(quad);
            }
        }

        private void QuadClearHandler()
        {
            quadList.Clear();
            QuadPager.NumberOfPages = 1;
        }

        private void TogglePaneOnClick(object sender, RoutedEventArgs e)
        {
            isPaneOpen = SplitView.IsPaneOpen;
            isPaneOpen = !isPaneOpen;
            SplitView.IsPaneOpen = isPaneOpen;
        }

        private void UpdateMidCodeFile()
        {
            writer.ClearCodeFile(false);
            writer.ExportMidCodeToFile(Compiler.quadTable.GetQuadList());
        }

        private void OpenCodeNotePad(object sender, RoutedEventArgs e)
        {
            writer.OpenFileInNotepad(false);
            MainWindow.Instance().ShowNotifyPage("若要继续，请先关闭代码文件。", "提示");
        }
    }
}

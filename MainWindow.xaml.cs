using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CLikeCompiler
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public static MainWindow mainPage;

        public MainWindow()
        {
            this.InitializeComponent();
            SetWindowTitleBar();
            ShowWelcomePage();
            mainPage = this;
        }

        private void SetWindowTitleBar()
        {
            Title = "OSCC - 类 C 编译器";
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon("Assets/favicon.ico");
        }

        private void SideNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem;
            FrameNavigation(selectedItem);
        }

        private void ShowWelcomePage()
        {
            SideNav.SelectedItem = welcomViewItem;
        }

        private void FrameNavigation(NavigationViewItem pageSelected)
        {
            if(pageSelected != null)
            {
                SideNav.SelectedItem = pageSelected;
                string pageTag = ((string)pageSelected.Tag);
                PageTagNavigation(pageTag, true);
            }
        }

        public void PageTagNavigation(string pageTag, bool isChanged = false)
        {
            if(!isChanged)
            {
                foreach (NavigationViewItem item in SideNav.MenuItems)
                {
                    if (item.Tag.ToString() == pageTag)
                    {
                        SideNav.SelectedItem = item;
                        break;
                    }
                }
            }
            
            string pageName = "CLikeCompiler.Pages." + pageTag;
            Type pageType = Type.GetType(pageName);
            if(pageType != null)
            {
                contentFrame.Navigate(pageType, null, new EntranceNavigationTransitionInfo());
            } else
            {
                ShowErrorPage("应用发生了内部错误");
            }
        }

        public async void ShowErrorPage(string message)
        {
            CLikeCompiler.Pages.ErrorDialog dialogPage = new();
            dialogPage.SetErrorMsg(message);

            ContentDialog dialog = new()
            {
                Title = "内部错误",
                PrimaryButtonText = "确定",
                DefaultButton = ContentDialogButton.Primary,
                Content = dialogPage,
            };
            await dialog.ShowAsync();
        }
    }
}

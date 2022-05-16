using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CLikeCompiler.Libs;
using CLikeCompiler.Libs.Util;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CLikeCompiler.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TargetCodePage : Page
    {
        private readonly List<string> codeTable = Compiler.codeTable;
        private readonly TargetWriter writer = MainWindow.Instance().writer;


        public TargetCodePage()
        {
            this.InitializeComponent();
            Compiler.Instance().CodeTableChange += UpdateCodeDisplay;
        }

        private void UpdateCodeDisplay()
        {
            if (codeTable.Count <= 0)
            {
                CodeBlock.Text = string.Empty;
                writer.ClearCodeFile();
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                foreach (string s in codeTable)
                {
                    builder.Append(s);
                }
                CodeBlock.Text = builder.ToString();
                writer.ExportTargetCodeToFile(CodeBlock.Text);
            }
        }

        private void OpenCodeNotePad(object sender, RoutedEventArgs e)
        {
            writer.OpenLogInNotepad();
            MainWindow.Instance().ShowNotifyPage("若要继续，请先关闭代码文件。", "提示");
        }

        private void ClearDisplayClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}

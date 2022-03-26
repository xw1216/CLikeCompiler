using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

using CLikeCompiler.Pages;
using Microsoft.UI.Xaml.Controls;

[assembly: InternalsVisibleTo("CLikeCompilerTests")]
namespace CLikeCompiler.Libs
{
    public class CompilerReportArgs : EventArgs
    {
        public readonly string msg;
        public readonly int lineNo;
        public readonly LogItem.MsgType msgType;

        public CompilerReportArgs(LogItem.MsgType msgType, string msg, int lineNo = 0)
        {
            this.msg = msg;
            this.lineNo = lineNo;
            this.msgType = msgType;
        }
    }

    public class Compiler
    {
        public delegate void CompilerReportHandler(object sender, CompilerReportArgs e);

        private static Compiler compiler = new();
        internal static PreproServer prepro;
        internal static LexServer lex;
        internal static GramParser parser;
        internal static GramServer gram;
        internal static MidGenServer midGen;
        internal static OptiServer opti;
        internal static TargGenServer targGen;

        internal static ResourceLoader resFile;

        internal static MacroTable macroTable;
        internal static RecordTable recordTable;
        internal static QuadTable quadTable;

        private Compiler()
        {
            RegisterComponents();
            Term.Init();
        }

        private void RegisterComponents()
        {
            prepro = new PreproServer();
            lex = new LexServer();
            parser = new GramParser();
            gram = new GramServer();
            midGen = new MidGenServer(gram);
            opti = new OptiServer();
            targGen = new TargGenServer();
            resFile = new ResourceLoader("Res");
            macroTable = new MacroTable();
            recordTable = new RecordTable();
            quadTable = new QuadTable();
            midGen.SetTable(quadTable, recordTable);
        }

        public void ResetCompiler()
        {
            prepro.ResetPrePro();
            lex.ResetLex();
            parser.ResetGramParser();
            gram.ResetGramServer();

            MainWindow.GetInstance().SetDefaultRootPath();
        }

        public static ref Compiler GetInstance()
        {
            return ref compiler;
        }

        public bool StartCompile(ref string src, TextBox box)
        {
            prepro.ResetPrePro();
            lex.ResetLex();
            gram.ResetAnalyStack();
            string srcAfter = "";
            // First make sure base grammar productions is ready.
            if (!StartGramParse()) { return false; }
            if(!StartGramBuild()) { return false; }
            // Second pre-process the src, and feedback to src input page
            if (!StartPrePro(ref src, ref srcAfter)) { return false; }
            box.Text = srcAfter;
            lex.SetSrc(ref srcAfter);
            // Third grammar analysis start, get lexical unit from lex
            if(!StartGramServer()) { return false; }

            return true;
        }

        internal bool StartPrePro(ref string input, ref string output)
        {
            try
            {
                prepro.SetMacroTable(ref macroTable);
                prepro.StartPrePro(ref input);
                output = prepro.GetSrc();
                CompilerReportArgs argsSuc = new(LogItem.MsgType.INFO, "预处理完成");
                ReportBackInfo(this, argsSuc);
            }
            catch (Exception)
            {
                CompilerReportArgs argsFail = new(LogItem.MsgType.ERROR, "停止预处理");
                ReportBackInfo(this, argsFail);
                prepro.ResetPrePro();
                return false;
            }
            return true;
        }

        internal bool StartGramParse()
        {
            try
            {
                parser.StartGramParse();
                CompilerReportArgs args = new(LogItem.MsgType.INFO, "基础文法解析完成");
                ReportBackInfo(this, args);
            } catch (Exception)
            {
                CompilerReportArgs args = new(LogItem.MsgType.ERROR, "停止文法解析");
                ReportBackInfo(this, args);
                parser.ResetGramParser();
                return false;
            }
            return true;
        }

        internal bool StartGramBuild()
        {
            try
            {
                gram.BuildGram();
                CompilerReportArgs args = new(LogItem.MsgType.INFO, "基础文法解析完成");
                ReportBackInfo(this, args);
                return true;
            }
            catch (Exception)
            {
                CompilerReportArgs args = new(LogItem.MsgType.ERROR, "语法模板建立失败，请检查");
                ReportBackInfo(this, args);
                lex.ResetLex();
                gram.ResetGramServer();
                return false;
            }
        }

        public bool StartGramServer()
        {
            bool IsGramCorrect = false;
            try { 
                IsGramCorrect = gram.StartGramAnaly();
                string tips = (IsGramCorrect ? "分析完成" : "语法分析完成，发现错误");
                Compiler.GetInstance().ReportBackInfo(this,
                        new CompilerReportArgs(LogItem.MsgType.INFO, tips));
            }
            catch (Exception)
            {
                CompilerReportArgs args = new(LogItem.MsgType.ERROR, "内部错误，停止语法检查");
                ReportBackInfo(this, args);
                lex.ResetLex();
                gram.ResetAnalyStack();
                return false;
            }
            return IsGramCorrect;
        }

        internal void ReportFrontInfo(object sender, CompilerReportArgs e)
        {
            string msg =
                (e.lineNo > 0 ?  $"在源码第 {e.lineNo} 行：{e.msg}" : 
                    $"在源码未知行：{e.msg}");
            LogUtility.GetInstance().NewLogRecord(msg, e.msgType);
        }

        internal void ReportBackInfo(object sender, CompilerReportArgs e)
        {
            string partName = GetComponentName(sender);
            string tipMsg = (e.msgType == LogItem.MsgType.INFO) ?  "：" : "内部问题：";
            string msg = partName + tipMsg +  e.msg;
            LogUtility.GetInstance().NewLogRecord(msg, e.msgType);
        }

        private string GetComponentName(object sender)
        {
            string partName = GetStrFromResw(sender.GetType().Name);
            if(partName == null)
                partName = "资源文件";
            return partName;
        }

        internal void SetRootPath(string path)
        {
            PreproServer.SetRootPath(ref path);
        }

        internal string GetRootPath()
        {
            return PreproServer.GetPath();
        }

        internal string GetStrFromResw(string key)
        {
            if(resFile == null)
            {
                ReportBackInfo(this, new CompilerReportArgs(LogItem.MsgType.ERROR, "资源文件丢失"));
            }
            string value = resFile.GetString(key);
            if(value == null) 
            {
                ReportBackInfo(this, new CompilerReportArgs(LogItem.MsgType.ERROR, "资源中不存在该字符串："+ key));
            }
            return value;
        }


        
    }
}

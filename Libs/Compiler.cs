using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

using CLikeCompiler.Pages;
using Microsoft.UI.Xaml.Controls;
using CLikeCompiler.Libs.Component;
using CLikeCompiler.Libs.Unit.Symbol;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Util.LogItem;
using CLikeCompiler.Libs.Util;

namespace CLikeCompiler.Libs
{

    public class Compiler
    {

        private Compiler()
        {
            RegisterComponents();
            Term.Init();
        }

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

        public delegate void CompilerReportHandler(object sender, LogReportArgs e);

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

        public static ref Compiler Instance()
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

        private bool StartPrePro(ref string input, ref string output)
        {
            try
            {
                prepro.SetMacroTable(ref macroTable);
                prepro.StartPrePro(ref input);
                output = prepro.GetSrc();
                LogReportArgs argsSuc = new(LogMsgItem.MsgType.INFO, "预处理完成");
                ReportBackInfo(this, argsSuc);
            }
            catch (Exception)
            {
                LogReportArgs argsFail = new(LogMsgItem.MsgType.ERROR, "停止预处理");
                ReportBackInfo(this, argsFail);
                prepro.ResetPrePro();
                return false;
            }
            return true;
        }

        private bool StartGramParse()
        {
            try
            {
                parser.StartGramParse();
                LogReportArgs args = new(LogMsgItem.MsgType.INFO, "基础文法解析完成");
                ReportBackInfo(this, args);
            } catch (Exception)
            {
                LogReportArgs args = new(LogMsgItem.MsgType.ERROR, "停止文法解析");
                ReportBackInfo(this, args);
                parser.ResetGramParser();
                return false;
            }
            return true;
        }

        private bool StartGramBuild()
        {
            try
            {
                gram.BuildGram();
                LogReportArgs args = new(LogMsgItem.MsgType.INFO, "基础文法解析完成");
                ReportBackInfo(this, args);
                return true;
            }
            catch (Exception)
            {
                LogReportArgs args = new(LogMsgItem.MsgType.ERROR, "语法模板建立失败，请检查");
                ReportBackInfo(this, args);
                lex.ResetLex();
                gram.ResetGramServer();
                return false;
            }
        }

        private bool StartGramServer()
        {
            bool IsGramCorrect = false;
            try { 
                IsGramCorrect = gram.StartGramAnaly();
                string tips = (IsGramCorrect ? "分析完成" : "语法分析完成，发现错误");
                Compiler.Instance().ReportBackInfo(this,
                        new LogReportArgs(LogMsgItem.MsgType.INFO, tips));
            }
            catch (Exception)
            {
                LogReportArgs args = new(LogMsgItem.MsgType.ERROR, "内部错误，停止语法检查");
                ReportBackInfo(this, args);
                lex.ResetLex();
                gram.ResetAnalyStack();
                return false;
            }
            return IsGramCorrect;
        }

        internal void ReportFrontInfo(object sender, LogReportArgs e)
        {
            string msg =
                (e.lineNo > 0 ?  $"在源码第 {e.lineNo} 行：{e.msg}" : 
                    $"在源码未知行：{e.msg}");
            Logger.Instance().NewLogRecord(msg, e.msgType);
        }

        internal void ReportBackInfo(object sender, LogReportArgs e)
        {
            string partName = GetComponentName(sender);
            string tipMsg = (e.msgType == LogMsgItem.MsgType.INFO) ?  "：" : "内部问题：";
            string msg = partName + tipMsg +  e.msg;
            Logger.Instance().NewLogRecord(msg, e.msgType);
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
                ReportBackInfo(this, new LogReportArgs(LogMsgItem.MsgType.ERROR, "资源文件丢失"));
            }
            string value = resFile.GetString(key);
            if(value == null) 
            {
                ReportBackInfo(this, new LogReportArgs(LogMsgItem.MsgType.ERROR, "资源中不存在该字符串："+ key));
            }
            return value;
        }
    }
}

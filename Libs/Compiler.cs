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
using CLikeCompiler.Libs.Unit.Reg;

[assembly: InternalsVisibleTo("CLikeCompiler.Libs")]

namespace CLikeCompiler.Libs
{
    public class Compiler
    {
        private static Compiler compiler = new();

        internal static PreproServer prepro;
        internal static LexServer lex;
        internal static GramParser parser;
        internal static GramServer gram;
        internal static MidGenServer midGen;
        internal static OptimizeServer optimize;
        internal static TargetGenServer targetGen;

        internal static RegFiles regFiles;

        internal static MacroTable macroTable;
        internal static RecordTable recordTable;
        internal static QuadTable quadTable;

        internal static ResourceLoader resFile;

        public delegate void CompilerReportHandler(object sender, LogReportArgs e);

        private Compiler()
        {
            RegisterComponents();
            InitRelation();
            Term.Init();
        }

        private void RegisterComponents()
        {
            prepro = new PreproServer();
            lex = new LexServer();
            parser = new GramParser();
            gram = new GramServer();
            midGen = new MidGenServer(gram);
            optimize = new OptimizeServer();
            targetGen = new TargetGenServer();

            regFiles = new RegFiles();

            macroTable = new MacroTable();
            recordTable = new RecordTable();
            quadTable = new QuadTable();

            resFile = new ResourceLoader("Res");
        }

        private void InitRelation()
        {
            midGen.SetTable(quadTable, recordTable);
            optimize.InitExternalComponents(regFiles, quadTable, recordTable.GetFuncList());
            targetGen.InitExternalComponents(regFiles, quadTable, recordTable);
        }

        public static void ResetCompiler()
        {
            prepro.ResetPrePro();
            lex.ResetLex();
            parser.ResetGramParser();
            gram.ResetGramServer();
            midGen.ResetMidGenServer();

            // MainWindow.GetInstance().SetDefaultRootPath();
        }

        public static ref Compiler Instance()
        {
            return ref compiler;
        }

        public bool StartCompile(ref string src, TextBox box)
        {
            ResetCompiler();
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
                LogReportArgs argsSuc = new(LogMsgItem.Type.INFO, "预处理完成");
                ReportBackInfo(this, argsSuc);
            }
            catch (Exception)
            {
                LogReportArgs argsFail = new(LogMsgItem.Type.ERROR, "停止预处理");
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
                LogReportArgs args = new(LogMsgItem.Type.INFO, "基础文法解析完成");
                ReportBackInfo(this, args);
            } catch (Exception)
            {
                LogReportArgs args = new(LogMsgItem.Type.ERROR, "停止文法解析");
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
                LogReportArgs args = new(LogMsgItem.Type.INFO, "基础文法解析完成");
                ReportBackInfo(this, args);
                return true;
            }
            catch (Exception)
            {
                LogReportArgs args = new(LogMsgItem.Type.ERROR, "语法模板建立失败，请检查");
                ReportBackInfo(this, args);
                lex.ResetLex();
                gram.ResetGramServer();
                return false;
            }
        }

        private bool StartGramServer()
        {
            bool isGramCorrect;
            try { 
                isGramCorrect = gram.StartGramAnaly();
                string tips = (isGramCorrect ? "分析完成" : "语法分析完成，发现错误");
                Compiler.Instance().ReportBackInfo(this,
                        new LogReportArgs(LogMsgItem.Type.INFO, tips));
                recordTable.MarkGlobalDataRecord();
            }
            catch (Exception e)
            {
                LogReportArgs innerArgs = new(LogMsgItem.Type.ERROR, e.Message);
                LogReportArgs args = new(LogMsgItem.Type.ERROR, "内部错误，停止语法检查");
                ReportBackInfo(this, innerArgs);
                ReportBackInfo(this, args);
                lex.ResetLex();
                gram.ResetAnalyStack();
                return false;
            }
            return isGramCorrect;
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
            string tipMsg = (e.msgType == LogMsgItem.Type.INFO) ?  "：" : "问题：";
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
                ReportBackInfo(this, new LogReportArgs(LogMsgItem.Type.ERROR, "资源文件丢失"));
                throw new NullReferenceException("");
            }
            string value = resFile.GetString(key);
            if(value == null) 
            {
                ReportBackInfo(this, new LogReportArgs(LogMsgItem.Type.ERROR, "资源中不存在该字符串："+ key));
            }
            return value;
        }
    }
}

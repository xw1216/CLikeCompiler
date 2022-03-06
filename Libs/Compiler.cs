using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

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
        internal static GramServer gram;
        internal static MidGenServer midGen;
        internal static OptiServer opti;
        internal static TargGenServer targGen;
        internal static ResourceLoader resFile;

        public Compiler()
        {
            prepro = new PreproServer();
            lex = new LexServer();
            gram = new GramServer();
            midGen = new MidGenServer();
            opti = new OptiServer();
            targGen = new TargGenServer();
            resFile = new ResourceLoader("Res");
        }

        public static ref Compiler GetInstance()
        {
            return ref compiler;
        }

        internal bool StartPrePro(ref string input, ref string output, ref MacroTable table)
        {
            try
            {
                prepro.StartPrePro(ref input);
                output = prepro.GetSrc();
                prepro.GetMacroTable(ref table);
            } catch (Exception)
            {
                CompilerReportArgs args = new(LogItem.MsgType.ERROR, "停止解析");
                ReportBackInfo(this, args);
                return false;
            }
            return true;
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
            string msg = partName + "组件内部问题：" +  e.msg;
            LogUtility.GetInstance().NewLogRecord(msg, e.msgType);
        }

        private string GetComponentName(object sender)
        {
            if(resFile == null)
                ReportBackInfo(this, new CompilerReportArgs(LogItem.MsgType.ERROR, "资源文件丢失"));
            string partName = resFile.GetString(sender.GetType().Name);
            if(partName == null)
                partName = "资源文件";
            return partName;
        }

        public void SetRootPath(string path)
        {
            int startPos = path.LastIndexOf(@"\");
            path.Remove(startPos);
            PreproServer.SetRootPath(ref path);
        }

        internal string GetRootPath()
        {
            return PreproServer.GetPath();
        }


        
    }
}

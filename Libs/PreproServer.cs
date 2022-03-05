using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class PreproServer
    {
        private delegate void MacroHandler(string strArg);
        private event MacroHandler MacroEvent;
        private static MacroTable macroTable;
        private static string rootPath;

        private StringBuilder src;
        private int basePos;
        private int rearPos;
        private int ifdefCnt;
        private int linePos;

        private static readonly char macroNote = '#';
        private static readonly char newlineNote = '\n';
        private static readonly char stringNote = '"';
        private static readonly char whiteNote = ' ';
        private readonly int invalid = -1;


        public PreproServer()
        {
            ResetServer();
        }

        private void ResetServer()
        {
            src.Clear();
            basePos = 0;
            rearPos = 1;
            ifdefCnt = 0;
            linePos = 1;
        }

        public void ResetMacroTable()
        {
            macroTable.ResetMacroTable();
        }

        public void SetSrc(ref string src)
        {
            src.Replace('\t', ' ');
            src.Replace("\r", "");
            src.Trim();
            this.src = new(src);
        }

        public string GetSrc()
        {
            return src.ToString();
        }

        public static void SetRootPath(ref string path)
        {
            rootPath = path;
        }

        public void StartPrePro(ref string src)
        {
            SetSrc(ref src);
            MacroRecognize();
        }

        public void GetMacroTable(ref MacroTable table)
        {
            table = macroTable;
        }

        private void SendFrontMessage(string msg, LogItem.MsgType type)
        {
            CompilerReportArgs args = new(type, msg, this.linePos);
            Compiler.GetInstance().ReportFrontInfo(this, args);
        }

        private void SendBackMessage(string msg, LogItem.MsgType type)
        {
            CompilerReportArgs args = new(type, msg, this.linePos);
            Compiler.GetInstance().ReportBackInfo(this, args);
        }

        private void MacroRecognize()
        {
            int macroPos = MacroNoteIndex();

            while(macroPos != invalid)
            {
                StringBuilder builder = GetSubString(macroPos);
                switch (builder.ToString())
                {
                    case "ifndef":
                        IfndefHandler(); break;
                    case "define":
                        DefineHandler(); break;
                    case "include":
                        IncludeHandler(); break;
                    case "endif":
                        EndIfHandler(); break;
                    default:
                        SendFrontMessage("无法识别的预处理符号", LogItem.MsgType.ERROR);
                        throw new Exception();
                }
                macroPos = MacroNoteIndex();
            }
        }

        private void DefineHandler()
        {
            StringBuilder from = GetSubString(rearPos);
            if(from.Length == 0) { MacroArgLack(); }
            if (macroTable.IsMacroExist(from.ToString()))
            {
                SendFrontMessage("重复的宏定义", LogItem.MsgType.WARN);
            }

            StringBuilder to = GetSubString(rearPos);
            if (to.Length == 0)
            {
                macroTable.AddDefineInclude(from.ToString());
            } else
            {
                macroTable.AddDefineValue(from.ToString(), to.ToString());
            }
            RemoveMacro();
            // The Define Replacement has been delayed to lexServer process.
        }

        private void IncludeHandler()
        {
            StringBuilder filename = GetSubString(rearPos);
            RemoveMacro();
            if(!(filename.Length > 0 ||
                (filename[0] == '<' && filename[filename.Length-1] == '>') ||
                (filename[0] == '"' && filename[filename.Length - 1] == '"')))
            {
                MacroArgLack();
            }
            filename.Remove(0, 1);
            filename.Remove(filename.Length - 1, 1);
            StringBuilder subSrc = GetIncludeFile(filename.ToString());
            src.Insert(basePos, subSrc.ToString());
        }

        private StringBuilder GetIncludeFile(string filename)
        {
            if(filename.Length == 0) { return null; }
            string filePath = rootPath + @"\" + filename + ".txt";
            SendFrontMessage("处理包含子文件中：" + filename, LogItem.MsgType.INFO);

            string text;
            try
            {
                text = System.IO.File.ReadAllText(filePath);
            } catch (Exception)
            {
                SendFrontMessage("无法读取文件：" + filename, LogItem.MsgType.ERROR);
                throw new Exception();
            }

            ProcFileRecurs(ref text);

            SendFrontMessage("离开子文件：" + filename, LogItem.MsgType.INFO);
            return new StringBuilder("\n" + text + "\n");
        }

        private void ProcFileRecurs(ref string src)
        {
            PreproServer prepro = new();
            prepro.StartPrePro(ref src);
            src = prepro.GetSrc();
        }

        private void EndIfHandler()
        {
            ifdefCnt--;
            CheckIfDismatch();
        }

        private void MacroArgLack()
        {
            SendFrontMessage("宏定义没有参数", LogItem.MsgType.ERROR);
            throw new Exception();
        }

        private void CheckIfDismatch()
        {
            if (ifdefCnt <= 0)
            {
                SendFrontMessage("未能配对的 if 宏定义", LogItem.MsgType.ERROR);
                throw new Exception();
            }
        }

        private void IfndefHandler()
        {
            StringBuilder macro = GetSubString(rearPos);
            if(macro.Length == 0) {
                MacroArgLack();
            }
            RemoveMacro();
            ifdefCnt++;
            if (macroTable.IsMacroExist(macro.ToString()))
            {
                RemoveUntilEndIf();
            }
        }

        private void RemoveUntilEndIf()
        {
            int start = basePos;
            int nextMacroPos;
            StringBuilder macro;
            do
            {
                nextMacroPos = MacroNoteIndex();
                macro = GetSubString(rearPos);
                if(macro.ToString() == "ifndef") { ifdefCnt++; }
                else if(macro.ToString() == "endif") { ifdefCnt--; }
            } while (ifdefCnt <=0 || nextMacroPos != invalid);
            CheckIfDismatch();
            SetPosToNextLine();
            int end = basePos;
            RemoveSrc(start, end);
        }

        private void RemoveMacro()
        {
            int start = basePos;
            SetPosToNextLine();
            int end = basePos;
            RemoveSrc(start, end);
        }



        private void RemoveSrc(int start, int end)
        {
            int length = end - start;
            src.Remove(start, length);
            basePos -= length;
            UpdateRear();
        }

        private void SetPosToNextLine()
        {
            if(basePos < 0 || basePos >= src.Length) { return; }
            while (basePos < src.Length && !IsNewLine(src[basePos]))
            {
                basePos++;
            }
            if (basePos == src.Length)
            {
                basePos = invalid;
                rearPos = invalid;
            } else
            {
                basePos++;
                UpdateRear();
                linePos++;
            }
        }

        private void UpdateRear()
        {
            if (basePos == src.Length)
            {
                rearPos = invalid;
            }
            else
            {
                rearPos = basePos + 1;
            }
        }

        private StringBuilder GetSubString(int pos)
        {
            StringBuilder str = new();
            bool haveValidChar = false;
            while (pos < src.Length && pos >= 0 
                && !(IsWhiteSpace(src[pos]) || haveValidChar))
            {
                if (IsWhiteSpace(src[pos]) && !haveValidChar)
                {
                    if(IsNewLine(src[pos]))
                    {
                        break;
                    }
                    haveValidChar = true;
                    pos++;
                    continue;
                }
                str.Append(src[pos]);
                pos++;
            }
            if (pos >= src.Length || pos < 0) { rearPos = invalid; }
            else { rearPos = pos; }
            return str;
        }
        
        private int MacroNoteIndex()
        {
            if (basePos == invalid) { return invalid; }
            for (int i = basePos; i < src.Length; i++)
            {
                if(i < 0) { break; }
                if (src[i] == macroNote)
                {
                    basePos = i;
                    rearPos = i + 1;
                    return i;
                }
                else if(src[i] == stringNote) { i = SetPosToStringEnd(i + 1); } 
                else if (src[i] == '/') { i = CommentHandler(i); }
                else if (src[i] == newlineNote) { linePos++; }
                else if(src[i] == whiteNote) { MergeWhiteSpace(i); }
            }
            basePos = rearPos = invalid;
            return invalid;
        }

        private void MergeWhiteSpace(int pos)
        {
            if (pos < 0 || pos >= src.Length)
            {
                SendBackMessage("非法的函数参数：CommentHandler", LogItem.MsgType.ERROR);
                throw new Exception();
            }
            while(pos+1 < src.Length && src[pos+1] == whiteNote)
            {
                src.Remove(pos + 1, 1);
            }
            basePos = pos + 1;
            UpdateRear();
        }
        
        private int CommentHandler(int pos)
        {
            if(pos < 0 || pos >= src.Length) {
                SendBackMessage("非法的函数参数：CommentHandler", LogItem.MsgType.ERROR);
                throw new Exception();
            }

            if(src[pos+1] == '/')
            {
                basePos = pos;
                RemoveMacro();
                return pos - 1;
            } else if(src[pos+1] == '*')
            {
                int start = pos;
                pos += 2;
                for (; pos < src.Length - 1; pos++) {
                    if(src[pos] == '*' && src[pos + 1] == '/') {
                        int end = basePos = pos + 2;
                        RemoveSrc(start, end);
                        return pos - 1;
                     } else if(src[pos] == newlineNote)
                    {
                        linePos++;
                    }
                }
                SendFrontMessage("未闭合的注释符号", LogItem.MsgType.ERROR);
                throw new Exception();
            }

            SendFrontMessage("未闭合的注释符号", LogItem.MsgType.ERROR);
            throw new Exception();
        }

        private int SetPosToStringEnd(int pos)
        {
            if(pos < 0 || pos >= src.Length) {
                SendBackMessage("非法的函数参数：SetPosToStringEnd", LogItem.MsgType.ERROR);
                throw new Exception();
            }
            for(int i = pos; i < src.Length; i++)
            {
                if(src[i] == stringNote)
                {
                    basePos = i + 1;
                    UpdateRear();
                    return i;
                }
            }
            SendFrontMessage("未闭合的字符串", LogItem.MsgType.ERROR);
            throw new Exception();
            /*basePos = src.Length;
            rearPos = invalid;
            return invalid;*/
        }

        private static bool IsNewLine(char c)
        {
            if (c == newlineNote) { return true; }
            return false;
        }

        private static bool IsWhiteSpace(char c)
        {
            if (c == ' ' || c == newlineNote) { return true; }
            return false;
         }

        private void ClearEvent()
         {
            if (MacroEvent == null) return;
            Delegate[] delegates = MacroEvent.GetInvocationList();
            foreach (Delegate d in delegates)
            {
                MacroEvent -= d as MacroHandler;
            }
         }
    }
}

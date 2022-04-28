using System;
using System.Collections.Generic;
using System.Text;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Util.LogItem;

namespace CLikeCompiler.Libs.Component
{
    internal class PreproServer
    {
        private delegate void MacroHandler(string strArg);
        private event MacroHandler MacroEvent;
        private MacroTable macroTable;
        private static string rootPath;

        private StringBuilder src = new();
        private int basePos;
        private int rearPos;
        private int ifdefCnt;
        private int linePos;
        private bool isOver;

        private const char MacroNote = '#';
        private const char NewlineNote = '\n';
        private const char StringNote = '"';
        private const char WhiteNote = ' ';
        private const int Invalid = -1;


        public PreproServer()
        {
            ResetPrePro();
        }

        public void ResetPrePro()
        {
            src?.Clear();
            basePos = 0;
            rearPos = 1;
            ifdefCnt = 0;
            linePos = 1;
            isOver = false;
            ResetMacroTable();
        }

        private void ResetMacroTable()
        {
            macroTable?.ResetMacroTable();
        }

        private void SetSrc(ref string srcArg)
        {
            srcArg = srcArg.Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace('\t', ' ')
                .Trim();
            this.src = new StringBuilder(srcArg);
            
        }

        public string GetSrc()
        {
            return src.ToString();
        }

        public static string GetPath()
        {
            return rootPath;
        }

        public static void SetRootPath(ref string path)
        {
            rootPath = path;
        }

        public bool GetIsOver()
        {
            return isOver;
        }

        public void StartPrePro(ref string srcArg)
        {
            SetSrc(ref srcArg);
            MacroRecognize();
            isOver = true;
        }

        public void SetMacroTable(ref MacroTable table)
        {
            macroTable = table;
        }

        private void SendFrontMessage(string msg, LogMsgItem.Type type)
        {
            LogReportArgs args = new(type, msg, linePos);
            Compiler.Instance().ReportFrontInfo(this, args);
        }

        private void SendBackMessage(string msg, LogMsgItem.Type type)
        {
            LogReportArgs args = new(type, msg, linePos);
            Compiler.Instance().ReportBackInfo(this, args);
        }

        private void MacroRecognize()
        {
            int macroPos = MacroNoteIndex();

            while(macroPos != Invalid)
            {
                StringBuilder builder = GetSubString(macroPos + 1);
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
                        SendFrontMessage("无法识别的预处理符号", LogMsgItem.Type.ERROR);
                        throw new Exception();
                }
                macroPos = MacroNoteIndex();
            }
            CheckIfMismatch();
            DefineReplacement();
        }

        private void DefineHandler()
        {
            StringBuilder from = GetSubString(rearPos);
            if(from.Length == 0) { MacroArgLack(); }
            if (macroTable.IsMacroExist(from.ToString()))
            {
                SendFrontMessage("重复的宏定义", LogMsgItem.Type.WARN);
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

        private void DefineReplacement()
        {
            Dictionary<string, string> macro = macroTable.GetLocalMacros();
            foreach(string key in macro.Keys)
            {
                src = src.Replace(key, macro[key]);
            }
            macroTable.ClearLocalMacros();
        }

        private void IncludeHandler()
        {
            StringBuilder filename = GetSubString(rearPos);
            RemoveMacro();
            if(!(filename.Length > 0 ||
                (filename[0] == '<' && filename[^1] == '>') ||
                (filename[0] == '"' && filename[^1] == '"')))
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
            SendFrontMessage("处理子文件：" + filename, LogMsgItem.Type.INFO);

            string text;
            try
            {
                text = System.IO.File.ReadAllText(filePath);
            } catch (Exception)
            {
                SendFrontMessage("无法读取文件：" + filename, LogMsgItem.Type.ERROR);
                throw new Exception();
            }

            ProcFileRecurs(ref text);

            SendFrontMessage("离开子文件：" + filename, LogMsgItem.Type.INFO);
            return new StringBuilder(text);
        }

        private static void ProcFileRecurs(ref string src)
        {
            PreproServer prepro = new();
            MacroTable macroTable = new();
            prepro.SetMacroTable(ref macroTable);
            prepro.StartPrePro(ref src);
            src = prepro.GetSrc();
        }

        private void EndIfHandler()
        {
            ifdefCnt--;
            RemoveMacro();
        }

        private void MacroArgLack()
        {
            SendFrontMessage("宏定义没有参数", LogMsgItem.Type.ERROR);
            throw new Exception();
        }

        private void CheckIfMismatch()
        {
            if (ifdefCnt < 0 || ifdefCnt > 0)
            {
                SendFrontMessage("未能配对 if 宏定义", LogMsgItem.Type.ERROR);
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
            do
            {
                nextMacroPos = MacroNoteIndex();
                StringBuilder macro = GetSubString(rearPos);
                if(macro.ToString() == "ifndef") { ifdefCnt++; }
                else if(macro.ToString() == "endif") { ifdefCnt--; }
            } while (ifdefCnt > 0 || nextMacroPos != Invalid);
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
            if (basePos >= src.Length)
            {
                rearPos = Invalid;
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
                rearPos = Invalid;
            }
            else
            {
                rearPos = basePos + 1;
            }
        }

        private StringBuilder GetSubString(int pos)
        {
            StringBuilder str = new();
            while(src[pos] == ' ' && pos < src.Length) { pos++; }

            while (pos < src.Length && pos >= 0)
            {
                if(IsWhiteSpace(src[pos])) { break; }
                str.Append(src[pos]);
                pos++;
            }
            if (pos >= src.Length || pos < 0) { rearPos = Invalid; }
            else { rearPos = pos; }
            return str;
        }
        
        private int MacroNoteIndex()
        {
            if (basePos == Invalid) { return Invalid; }
            for (int i = basePos; i < src.Length; i++)
            {
                if(i < 0) { break; }
                if (src[i] == MacroNote)
                {
                    basePos = i;
                    rearPos = i + 1;
                    return i;
                }
                else if(src[i] == StringNote) { i = SetPosToStringEnd(i + 1); } 
                else if (src[i] == '/') { i = CommentHandler(i); }
                else if (src[i] == NewlineNote) { linePos++; }
                else if(src[i] == WhiteNote) { MergeWhiteSpace(i); }
            }
            basePos = rearPos = Invalid;
            return Invalid;
        }

        private void MergeWhiteSpace(int pos)
        {
            if (pos < 0 || pos >= src.Length)
            {
                SendBackMessage("非法的函数参数：CommentHandler", LogMsgItem.Type.ERROR);
                throw new Exception();
            }
            while(pos+1 < src.Length && src[pos+1] == WhiteNote)
            {
                src.Remove(pos + 1, 1);
            }
            basePos = pos + 1;
            UpdateRear();
        }
        
        private int CommentHandler(int pos)
        {
            if(pos < 0 || pos >= src.Length) {
                SendBackMessage("非法的函数参数：CommentHandler", LogMsgItem.Type.ERROR);
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
                    } else if(src[pos] == NewlineNote)
                    {
                        linePos++;
                    }
                }
                SendFrontMessage("未闭合的注释符号", LogMsgItem.Type.ERROR);
                throw new Exception();
            }

            SendFrontMessage("未闭合的注释符号", LogMsgItem.Type.ERROR);
            throw new Exception();
        }

        private int SetPosToStringEnd(int pos)
        {
            if(pos < 0 || pos >= src.Length) {
                SendBackMessage("非法的函数参数：SetPosToStringEnd", LogMsgItem.Type.ERROR);
                throw new Exception();
            }
            for(int i = pos; i < src.Length; i++)
            {
                if(src[i] == StringNote)
                {
                    basePos = i + 1;
                    UpdateRear();
                    return i;
                }
            }
            SendFrontMessage("未闭合的字符串", LogMsgItem.Type.ERROR);
            throw new Exception();
            /*basePos = srcArg.Length;
            rearPos = invalid;
            return invalid;*/
        }

        private static bool IsNewLine(char c)
        {
            if (c == NewlineNote) { return true; }
            return false;
        }

        private static bool IsWhiteSpace(char c)
        {
            return c is ' ' or NewlineNote;
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

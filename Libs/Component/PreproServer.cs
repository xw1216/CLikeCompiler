﻿using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Util.LogItem;
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
        private MacroTable macroTable;
        private static string rootPath;

        private StringBuilder src = new();
        private int basePos;
        private int rearPos;
        private int ifdefCnt;
        private int linePos;
        private bool IsOver;

        private static readonly char macroNote = '#';
        private static readonly char newlineNote = '\n';
        private static readonly char stringNote = '"';
        private static readonly char whiteNote = ' ';
        private readonly int invalid = -1;


        public PreproServer()
        {
            ResetPrePro();
        }

        public void ResetPrePro()
        {
            if(src != null) src.Clear();
            basePos = 0;
            rearPos = 1;
            ifdefCnt = 0;
            linePos = 1;
            IsOver = false;
            ResetMacroTable();
        }

        private void ResetMacroTable()
        {
            if(macroTable != null)
                macroTable.ResetMacroTable();
        }

        public void SetSrc(ref string src)
        {
            src = src.Replace("\r\n", "\n").Replace("\r", "\n").Replace('\t', ' ').Trim();
            this.src = new(src);
            
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
            return IsOver;
        }

        public void StartPrePro(ref string src)
        {
            SetSrc(ref src);
            MacroRecognize();
            IsOver = true;
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

            while(macroPos != invalid)
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
            CheckIfDismatch();
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

        private void ProcFileRecurs(ref string src)
        {
            PreproServer prepro = new();
            MacroTable macroTable = new MacroTable();
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

        private void CheckIfDismatch()
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
            StringBuilder macro;
            do
            {
                nextMacroPos = MacroNoteIndex();
                macro = GetSubString(rearPos);
                if(macro.ToString() == "ifndef") { ifdefCnt++; }
                else if(macro.ToString() == "endif") { ifdefCnt--; }
            } while (ifdefCnt > 0 || nextMacroPos != invalid);
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
            while(src[pos] == ' ' && pos < src.Length) { pos++; }

            while (pos < src.Length && pos >= 0)
            {
                if(IsWhiteSpace(src[pos])) { break; }
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
                SendBackMessage("非法的函数参数：CommentHandler", LogMsgItem.Type.ERROR);
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
                     } else if(src[pos] == newlineNote)
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
                if(src[i] == stringNote)
                {
                    basePos = i + 1;
                    UpdateRear();
                    return i;
                }
            }
            SendFrontMessage("未闭合的字符串", LogMsgItem.Type.ERROR);
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
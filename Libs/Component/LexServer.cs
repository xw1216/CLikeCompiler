using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CLikeCompiler.Libs.Unit.Analy;
using CLikeCompiler.Libs.Util.LogItem;

namespace CLikeCompiler.Libs.Component
{
    internal class LexServer
    {
        private static readonly Dictionary<string, string> keywords = new() {
            { "", "blank"}, {"true","true"}, {"false", "false" }, {"void","void" },
            {"int","int"}, {"long","long" }, {"float","float" }, {"double","double" },
            { "bool","bool" }, {"string","string" }, {"if","if" }, {"else","else" },
            {"while","while" }, {"for","for" }, {"break","break" }, {"continue","continue" },
            {"switch","switch" }, {"case","case" }, {"default","default" }, {"return","return" },
            {"char", "char"}
        };

        private static readonly Dictionary<string, string> operators = new()
        {
            {"=","assign" }, {"+","plus" }, {"-","sub" }, {"*","mul" }, 
            {"/","div" }, {"==","eq" }, {"!=","neq" }, {"<=","leq" }, 
            {">=","geq" }, {"<","les" }, { ">","gre" }, {"&", "and" }, 
            {"|","or" }, {"!","not" }, {";","smc" }, {",","cma" }, 
            {"(","lpar" }, {")","rpar" }, { "{","lbrc" }, {"}","rbrc" },
            {"[","lsbrc" }, {"]","rsbrc" },{":","colon" }
        };

        private static readonly List<string> constants = new()
        {
            "id",
            "integer",
            "decimal",
            "str",
            "ch"
        };

        private readonly LexUnit endUnit = new();

        private const char NewlineNote = '\n';
        private const char WhiteNote = ' ';
        private const char StringNote = '"';
        private const char CharNote = '\'';
        private const char DotNote = '.';

        private string src = "";
        private bool isEnd;

        private int linePos;
        private int basePos;
        private int rearPos;

        public LexServer()
        {
            ResetLex();
            endUnit.type = LexUnit.Type.END;
            endUnit.name = "end";
            endUnit.cont = "end";
            endUnit.line = 0;
        }

        internal void SetSrc(ref string srcArg)
        {
            this.src = srcArg;
            endUnit.line = srcArg.Count(IsNewLine);
        }

        private static bool IsNewLine(char c)
        {
            return c == NewlineNote;
        }

        public void ResetLex()
        {
            src = "";
            isEnd = false;
            linePos = 1;
            basePos = 0;
            rearPos = 1;
        }

        internal bool GetUnit(out LexUnit unit)
        {
            unit = new LexUnit();
            if(isEnd || src.Length == 0) 
            {
                unit = endUnit;
                return false; 
            }

            return StartLexStep(ref unit);
        }

        internal static bool IsKeyRecognize(string str)
        {
            return  (keywords.ContainsValue(str) || operators.ContainsValue(str) ||
                constants.Contains(str));
        }

        private bool StartLexStep(ref LexUnit unit)
        {
            if(isEnd) { unit = endUnit;  return false; }
            for(; basePos < src.Length; basePos++, rearPos++)
            {
                if(src[basePos] == 0) { unit = endUnit;  return false; }
                else if(src[basePos] == NewlineNote) { linePos++; continue; }
                else if(src[basePos] == WhiteNote) { continue; }
                else if(src[basePos] == StringNote) 
                {
                    StringHandler(ref unit);
                    return !isEnd;
                } 
                else if(src[basePos] == CharNote)
                {
                    CharHandler(ref unit); 
                    return !isEnd;
                }
                else if(IsNumber(src[basePos]))
                {
                    NumberHandler(ref unit);
                    return !isEnd;
                } 
                else if(IsIdentifierChar(src[basePos]))
                {
                    IdHandler(ref unit);
                    return !isEnd;
                } 
                else
                {
                    if(OperatorHandler(ref unit))
                    {
                        return !isEnd;
                    }
                    SendFrontMessage("不能识别的字符", LogMsgItem.Type.ERROR);
                    throw new Exception();
                }
            }
            unit = endUnit;
            isEnd = true;
            return false;
        }

        private void CharHandler(ref LexUnit unit)
        {
            for (; rearPos < src.Length; rearPos++)
            {
                if (src[rearPos] == CharNote)
                {
                    unit.type = LexUnit.Type.CH;
                    unit.name = "ch";
                    unit.cont = src.Substring(basePos, rearPos - basePos + 1);
                    RemoveBesetNote(unit.cont, CharNote);
                    unit.line = linePos;
                    rearPos++;
                    UpdatePosHandlerEnd();
                    return;
                }
                else if (src[rearPos] == NewlineNote)
                {
                    linePos++;
                }
            }
            SendFrontMessage("未闭合的字符", LogMsgItem.Type.ERROR);
            throw new Exception();
        }

        private bool OperatorHandler(ref LexUnit unit) 
        {
            char first = src[basePos];
            StringBuilder builder = new();
            builder.Append(first);
            if (!IsOperators(builder.ToString(), out string valueA)) return false;
            unit.type = LexUnit.Type.OP;
            unit.line = linePos;
            if (rearPos < src.Length)
            {
                char last = src[rearPos];
                StringBuilder inBuilder = new();
                inBuilder.Append(first);
                inBuilder.Append(last);
                if(IsOperators(inBuilder.ToString(), out string valueB))
                {
                    unit.name = valueB;
                    unit.cont = inBuilder.ToString();
                    rearPos += 1;
                    UpdatePosHandlerEnd();
                    return true;
                }
            }
            unit.name = valueA;
            unit.cont = builder.ToString();
            UpdatePosHandlerEnd();
            return true;
        }

        private void IdHandler(ref LexUnit unit)
        {
            for (; rearPos < src.Length; rearPos++)
            {
                if(!(IsIdentifierChar(src[rearPos])))
                {
                    break;
                }
            }

            string cont = src.Substring(basePos, rearPos - basePos);
            if(IsKeyword(ref cont, out string type))
            {
                unit.type = LexUnit.Type.KEYWORD;
                unit.name = type;
                unit.cont = type;
            } else
            {
                unit.type = LexUnit.Type.ID;
                unit.name = "id";
                unit.cont = cont;
            }
            unit.line = linePos;
            UpdatePosHandlerEnd();
        }

        private void NumberHandler(ref LexUnit unit)
        {
            for(; rearPos < src.Length; rearPos++)
            {
                if(!(src[rearPos] == DotNote || IsNumber(src[rearPos])))
                {
                    break;
                }
            }
            string num = src.Substring(basePos, rearPos - basePos);
            unit.cont = num;
            if (num.Contains(DotNote)) 
            { 
                unit.type = LexUnit.Type.DEC;
                unit.name = "decimal";
            }
            else 
            { 
                unit.type = LexUnit.Type.INT;
                unit.name = "integer";
            }
            unit.line = linePos;

            UpdatePosHandlerEnd();
        }

        private void StringHandler(ref LexUnit unit)
        {
            for(; rearPos < src.Length; rearPos++)
            {
                switch (src[rearPos])
                {
                    case StringNote:
                        unit.type = LexUnit.Type.STR;
                        unit.name = "str";
                        unit.cont = src.Substring(basePos, rearPos - basePos + 1);
                        RemoveBesetNote(unit.cont, StringNote);
                        unit.line = linePos;
                        rearPos++;
                        UpdatePosHandlerEnd();
                        return;
                    case NewlineNote:
                        linePos++;
                        break;
                }
            }
            SendFrontMessage("未闭合的字符串", LogMsgItem.Type.ERROR);
            throw new Exception();
        }

        private static string RemoveBesetNote(string str, char ch)
        {
            if(str.First() == ch) { str = str.Remove(0, 1); }
            if(str.Last() == ch) { str = str.Remove(str.Length - 1 , 1); }
            return str;
        } 

        // Make sure that rearPos is at n+1 while the last character in content is at n
        private void UpdatePosHandlerEnd()
        {
            if(rearPos >= src.Length)
            {
                isEnd = true;
                basePos = src.Length;
                rearPos = basePos + 1;
            } else
            {
                isEnd= false;
                basePos = rearPos;
                rearPos++;
            }
        }

        private bool IsIdentifierChar(char c)
        {
            return (IsAlphabet(c) || IsNumber(c) || c == '_');
        }

        private static bool IsKeyword(ref string key, out string value)
        {
            bool isIn = keywords.ContainsKey(key);
            if(isIn)
            {
                value = keywords[key];
                return true;
            } else
            {
                value="";
                return false;
            }
        }

        private static bool IsOperators(string key, out string value)
        {
            bool isIn = operators.ContainsKey(key);
            if (isIn)
            {
                value = operators[key];
                return true;
            }
            else
            {
                value = "";
                return false;
            }
        }

        private static bool IsNumber(char c)
        {
            return c is >= '0' and <= '9';
        }

        private static bool IsAlphabet(char c)
        {
            return (c is >= 'A' and <= 'Z' || c is >= 'a' and <= 'z');
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

    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class LexUnit
    {
        internal enum Type
        {
            ID, KEYWD, OP,
            INT, DEC, STR, END
        }

        internal Type type;
        internal string name;
        internal string cont;
        internal int line;
    }

    internal class LexServer
    {
        private static Dictionary<string, string> keywords = new Dictionary<string, string>() { 
            { "", "blank"}, {"true","true"}, {"false", "false" }, {"void","void" },
            {"int","int"}, {"long","long" }, {"float","float" }, {"double","double" },
            { "bool","bool" }, {"string","string" }, {"if","if" }, {"else","else" },
            {"while","while" }, {"for","for" }, {"break","break" }, {"continue","continue" },
            {"switch","switch" }, {"case","case" }, {"default","default" }, {"return","return" }
        };

        private static Dictionary<string, string> operators = new Dictionary<string, string>()
        {
            {"=","assign" }, {"+","plus" }, {"-","sub" }, {"*","mul" }, 
            {"/","div" }, {"==","eq" }, {"!=","neq" }, {"<=","leq" }, 
            {">=","geq" }, {"<","les" }, { ">","gre" }, {"&", "and" }, 
            {"|","or" }, {"!","not" }, {";","smc" }, {",","cma" }, 
            {"(","lpar" }, {")","rpar" }, { "{","lbrc" }, {"}","rbrc" },
            {"[","lsbrc" }, {"]","rsbrc" },{":","colon" }
        };

        private static List<string> constants = new()
        {
            "id",
            "integer",
            "decimal",
            "str"
        };

        private readonly LexUnit endUnit = new();

        private readonly char newlineNote = '\n';
        private readonly char whiteNote = ' ';
        private readonly char stringNote = '"';
        private readonly char dotNote = '.';

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

        internal void SetSrc(ref string src)
        {
            this.src = src;
            endUnit.line = src.Count(IsNewLine);
        }

        private bool IsNewLine(char c)
        {
            return c == newlineNote;
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
            unit = new();
            if(isEnd || src.Length == 0) 
            {
                unit = endUnit;
                return false; 
            }

            return StartLexStep(ref unit);
        }

        internal bool IsKeyRecog(string str)
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
                else if(src[basePos] == newlineNote) { linePos++; continue; }
                else if(src[basePos] == whiteNote) { continue; }
                else if(src[basePos] == stringNote) 
                {
                    StringHandler(ref unit);
                    return !isEnd;
                }
                else if(IsNumber(src[basePos]))
                {
                    NumberHandler(ref unit);
                    return !isEnd;
                } 
                else if(IsIdentifierChar(src[basePos]))
                {
                    IdHanlder(ref unit);
                    return !isEnd;
                } 
                else
                {
                    if(OpratorHandler(ref unit))
                    {
                        return !isEnd;
                    }
                    SendFrontMessage("不能识别的字符", LogItem.MsgType.ERROR);
                    throw new Exception();
                }
            }
            unit = endUnit;
            isEnd = true;
            return false;
        }

        private bool OpratorHandler(ref LexUnit unit) 
        {
            char first = src[basePos], last;
            StringBuilder builder = new();
            builder.Append(first);
            if(IsOprators(builder.ToString(), out string valueA))
            {
                unit.type = LexUnit.Type.OP;
                unit.line = linePos;
                if (rearPos < src.Length)
                {
                    last = src[rearPos];
                    StringBuilder inBuilder = new();
                    inBuilder.Append(first);
                    inBuilder.Append(last);
                    if(IsOprators(inBuilder.ToString(), out string valueB))
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
            return false;
        }

        private void IdHanlder(ref LexUnit unit)
        {
            string cont;
            for (; rearPos < src.Length; rearPos++)
            {
                if(!(IsIdentifierChar(src[rearPos])))
                {
                    break;
                }
            }

            cont = src.Substring(basePos, rearPos - basePos);
            if(IsKeyword(ref cont, out string type))
            {
                unit.type = LexUnit.Type.KEYWD;
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
            return;
        }

        private void NumberHandler(ref LexUnit unit)
        {
            string num;
            for(; rearPos < src.Length; rearPos++)
            {
                if(!(src[rearPos] == dotNote || IsNumber(src[rearPos])))
                {
                    break;
                }
            }
            num = src.Substring(basePos, rearPos - basePos);
            unit.cont = num;
            if (num.Contains(dotNote)) 
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
            return;
        }

        private void StringHandler(ref LexUnit unit)
        {
            for(; rearPos < src.Length; rearPos++)
            {
                if(src[rearPos] == stringNote)
                {
                    unit.type = LexUnit.Type.STR;
                    unit.name = "str";
                    unit.cont = src.Substring(basePos, rearPos - basePos + 1);
                    if(unit.cont.First() == stringNote) { unit.cont.Remove(0, 1); }
                    RemoveStrNote(unit.cont);
                    unit.line = linePos;
                    rearPos++;
                    UpdatePosHandlerEnd();
                    return;
                } else if(src[rearPos] == newlineNote)
                {
                    linePos++;
                }
            }
            SendFrontMessage("未闭合的字符串", LogItem.MsgType.ERROR);
            throw new Exception();
        }

        private string RemoveStrNote(string str)
        {
            if (str.First() == stringNote) { str.Remove(0, 1); }
            if(str.Last() == stringNote) { str.Remove(str.Length - 1 , 1); }
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

        private bool IsKeyword(ref string key, out string value)
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

        private bool IsOprators(string key, out string value)
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

        private bool IsNumber(char c)
        {
            return c >= '0' && c <= '9';
        }

        private bool IsAlphabet(char c)
        {
            return (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z');
        }

        private void SendFrontMessage(string msg, LogItem.MsgType type)
        {
            CompilerReportArgs args = new(type, msg, linePos);
            Compiler.GetInstance().ReportFrontInfo(this, args);
        }

        private void SendBackMessage(string msg, LogItem.MsgType type)
        {
            CompilerReportArgs args = new(type, msg, linePos);
            Compiler.GetInstance().ReportBackInfo(this, args);
        }

    }

}

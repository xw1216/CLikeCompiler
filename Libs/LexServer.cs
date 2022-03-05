using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
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

        private readonly string idKey = "id";
        private readonly string intKey = "integer";
        private readonly string decKey = "decimal";
        private readonly string stringKey = "str";

        private string src;

        private string unitType;
        private string unitCont;
        private int linePos;

        private void SetSrc(ref string src)
        {
            this.src = src;
        }

        private void ResetLex()
        {
            this.src = "";
            this.unitType = "";
            this.unitCont = "";
            this.linePos = 1;
        }

        private bool GetUnit(out string type, out string cont)
        {
            if(src.Length == 0) {
                type = null;
                cont = null;
                return false; 
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

        private bool IsOprators(ref string key, out string value)
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
    }

    
}

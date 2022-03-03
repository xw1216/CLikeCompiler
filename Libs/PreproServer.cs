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

        private string src;
        private int basePos;
        private int rearPos;

        public void SetSrc(string src)
        {
            this.src = src;
            
        }

        private void MacroRecognize()
        {
           
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

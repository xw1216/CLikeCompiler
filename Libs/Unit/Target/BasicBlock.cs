using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Unit.Quads;

namespace CLikeCompiler.Libs.Unit.Target
{
    internal class BasicBlock
    {
        internal List<QuadDescriptor> QuadList { get; }
        internal List<VarDescriptor> VarList { get; }

        internal List<BasicBlock> NextBlocks { get; }

        internal readonly List<VarRecord> inActiveList = new();
        internal readonly List<VarRecord> outActiveList = new();
        internal readonly List<VarRecord> useVarList;
        internal readonly List<VarRecord> defVarList;

        #region Constructor

        internal BasicBlock()
        {
            QuadList = new List<QuadDescriptor>();
            VarList = new List<VarDescriptor>();
            NextBlocks = new List<BasicBlock>();
            useVarList = new List<VarRecord>();
            defVarList = new List<VarRecord>();
        }

        internal BasicBlock(List<QuadDescriptor> quadList)
        {
            QuadList = quadList;
            useVarList = new List<VarRecord>();
            defVarList = new List<VarRecord>();
            VarList = CreateVarDescriptorList(quadList);
            NextBlocks = new List<BasicBlock>();
        }
        
        #endregion

        #region Var Descriptor

        private List<VarDescriptor> CreateVarDescriptorList(List<QuadDescriptor> block) 
        {
            List<VarDescriptor> descriptorList = new();
            List<VarRecord> varList = new(), refList = new(), defList = new();
            foreach (QuadDescriptor quad in block)
            {
                DetectVarRecord(descriptorList, varList, refList, defList, quad.ToQuad.Lhs, false);
                DetectVarRecord(descriptorList, varList, refList, defList,quad.ToQuad.Rhs, false);
                DetectVarRecord(descriptorList, varList, refList, defList,quad.ToQuad.Dst,true);
            }
            return descriptorList;
        }

        private void DetectVarRecord(
            List<VarDescriptor> descriptorList, List<VarRecord> varList, 
            List<VarRecord> refList, List<VarRecord> defList,
            IRecord rec, bool isDst)
        {
            if (rec is not VarRecord unit)
            {
                return;
            }

            if (isDst)
            {
                UniqueAdd(defList, unit);
                if (!(refList.Contains(unit)))
                {
                    UniqueAdd(defVarList, unit);
                }
            }
            else
            {
                UniqueAdd(refList, unit);
                if (!(defList.Contains(unit)))
                {
                    UniqueAdd(useVarList, unit);
                }
            }

            if(varList.Contains(unit)) { return; }

            varList.Add(unit);
            descriptorList.Add(new VarDescriptor(unit));
        }

        private static void UniqueAdd(List<VarRecord> list, VarRecord unit)
        {
            if(list.Contains(unit)) { return; }
            list.Add(unit);
        }

        #endregion


        #region Dataflow Judge

        internal bool IsQuadIn(Quad quad)
        {
            foreach (QuadDescriptor descriptor in QuadList)
            {
                if (descriptor.ToQuad == quad)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool IsStartWith(Quad quad)
        {
            if (QuadList.Count == 0)
            {
                return false;

            }
            return (QuadList[0].ToQuad == quad);
        }

        internal bool IsLastJump()
        {
            if (QuadList.Count == 0)
            {
                return false;
            }
            Quad lastQuad = QuadList.Last().ToQuad;
            return Quad.IsJumpOp(lastQuad.Name) && lastQuad.Name != "jal" & lastQuad.Name != "jr";
        }

        #endregion

    }
}

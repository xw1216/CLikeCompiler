using System.Collections.Generic;
using System.Linq;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Unit.Quads;

namespace CLikeCompiler.Libs.Unit.Descriptor
{
    internal class BasicBlock
    {
        internal List<QuadDescriptor> QuadList { get; }
        internal List<VarDescriptor> VarList { get; }

        internal List<BasicBlock> NextBlocks { get; }

        internal List<VarRecord> inActiveList = new();
        internal List<VarRecord> outActiveList = new();
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

        // 创建基本块内所有变量（含临时变量）对应的描述符对象
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

        // 检测记录类型 是变量则不重复地记录 use, def 性质，创建对应描述符
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

        internal VarDescriptor FindVarDescriptor(VarRecord rec)
        {
            foreach (VarDescriptor descriptor in VarList)
            {
                if (descriptor.Var == rec)
                {
                    return descriptor;
                }
            }
            return null;
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

        internal bool IsLastUnconditionalJump()
        {
            if (QuadList.Count == 0)
            {
                return false;
            }
            Quad lastQuad = QuadList.Last().ToQuad;
            return lastQuad.Name == "j";
        }

        #endregion

        #region Active Info

        // 初始化出口活跃变量
        // 需要在流图关系计算完后才能调用
        internal void InitOutActiveVars()
        {
            for (int i = 0; i < outActiveList.Count; i++)
            {
                VarDescriptor descriptor = FindVarDescriptor(outActiveList[i]);
                if (descriptor != null)
                {
                    descriptor.Active.IsActive = true;
                }
            }
        }

        #endregion


    }
}

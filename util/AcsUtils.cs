using System;
using ACS.SPiiPlusNET;
using CO.Common.Logger;

namespace CO.Systems.Services.Acs.AcsWrapper.util
{
    internal class AcsUtils
    {
        private readonly Api api;
        private readonly ILogger logger;

        public AcsUtils(Api api, ILogger logger)
        {
            this.api = api;
            this.logger = logger;
        }

        public int GetDBufferIndex()
        {
            if (!api.IsConnected) {
                logger.Info("AcsUtil.ReadVar: Controller not connected");
                return 0;
            }

            double num = 0.0;
            try {
                num = api.GetDBufferIndex();
            }
            catch (Exception ex) {
                logger.Error("AcsUtil.ReadVar: Failed to get DBuffer index " + ex.Message);
            }

            return (int) num;
        }

        public object ReadVar(string varName, ProgramBuffer bufferIndex = ProgramBuffer.ACSC_NONE,
            int indFrom = -1, int indFromTo = -1)
        {
            if (!api.IsConnected) {
                logger.Info("AcsUtil.ReadVar: Controller not connected");
                return null;
            }

            try {
                return api.ReadVariable(varName, bufferIndex, indFrom, indFromTo);
            }
            catch (Exception ex) {
                logger.Error($"AcsUtil.ReadVar: Exception while accessing '{varName}': " + ex.Message);
                return null;
            }
        }

        public object ReadVariableAsVector(string varName, ProgramBuffer bufferIndex = ProgramBuffer.ACSC_NONE,
            int from1 = -1, int to1 = -1, int from2 = -1, int to2 = -1)
        {
            if (!api.IsConnected) {
                logger.Info("AcsUtil.ReadVariableAsVector: Controller not connected");
                return null;
            }

            try {
                return api.ReadVariableAsVector(varName, bufferIndex, from1, to1, from2, to2);
            }
            catch (Exception ex) {
                logger.Error($"AcsUtil.ReadVariableAsVector: Exception while accessing '{varName}': " + ex.Message);
                return null;
            }
        }

        public object ReadVariableAsMatrix(string varName, ProgramBuffer bufferIndex = ProgramBuffer.ACSC_NONE,
            int from1 = -1, int to1 = -1, int from2 = -1, int to2 = -1)
        {
            if (!api.IsConnected) {
                logger.Info("AcsUtil.ReadVariableAsMatrix: Controller not connected");
                return null;
            }

            try {
                return api.ReadVariableAsMatrix(varName, bufferIndex, from1, to1, from2, to2);
            }
            catch (Exception ex) {
                logger.Error($"AcsUtil.ReadVariableAsMatrix: Exception while accessing '{varName}': " + ex.Message);
                return null;
            }
        }

        public void WriteVariable(object value, string variable, int index = -1, int from1 = -1, int to1 = -1,
            int from2 = -1, int to2 = -1)
        {
            if (!api.IsConnected) {
                logger.Info("AcsUtil.WriteVariable: Controller not connected");
            }
            else {
                try {
                    api.WriteVariable(value, variable, (ProgramBuffer) index, from1, to1, from2, to2);
                }
                catch (Exception ex) {
                    logger.Error($"AcsUtil.WriteVariable: Exception while accessing '{variable}': " + ex.Message);
                }
            }
        }

        public void WriteDouble(double varVal, string varName)
        {
            if (!api.IsConnected) {
                logger.Info("AcsUtils.WriteDouble: Controller not connected");
            }
            else {
                try {
                    api.WriteVariable(varVal, varName);
                }
                catch (Exception ex) {
                    logger.Error($"AcsUtils.WriteDouble: Exception while accessing '{varName}': " + ex.Message);
                }
            }
        }

        public void WriteVar(int[] num, string varName)
        {
            if (!api.IsConnected) {
                logger.Info("AcsUtils.WriteVar: Controller not connected");
            }
            else {
                try {
                    api.WriteVariable(num, varName, from1: 0, to1: (num.Length - 1));
                }
                catch (Exception ex) {
                    logger.Error($"AcsUtils.WriteVar: Exception while accessing '{varName}': " + ex.Message);
                }
            }
        }

        public void WriteGlobalReal(object value, string variable, int index = -1)
        {
            if (!api.IsConnected) {
                logger.Info("AcsUtils.WriteGlobalReal: Controller not connected");
                return;
            }

            try {
                api.WriteVariable(value, variable, from1: index, to1: index);
            }
            catch (Exception e) {
                logger.Error($"AcsUtils.WriteGlobalReal: Exception while writing '{variable}': " + e.Message);
            }
        }

        public double ReadGlobalReal(string variable, int index = -1)
        {
            if (!api.IsConnected) {
                logger.Info("AcsUtils.ReadGlobalReal: Controller not connected");
                return 0.0;
            }

            try {
                return (double) api.ReadVariableAsScalar(variable, ProgramBuffer.ACSC_NONE, index);
            }
            catch (Exception e) {
                logger.Error($"AcsUtils.ReadGlobalReal: Exception while reading '{variable}': " + e.Message);
                return 0.0;
            }
        }

        public int ReadInt(string variable, int index)
        {
            try {
                return Convert.ToInt32(ReadVar(variable, indFrom: index, indFromTo: index));
            }
            catch (Exception e) {
                logger.Error($"AcsUtils.ReadInt: Exception while reading '{variable}', '{index}': " + e.Message);
                return 0;
            }
        }

        public void WriteInt(string varName, int val)
        {
            WriteInt(varName, -1, val);
        }

        public void WriteInt(string varName, int index, int val)
        {
            WriteVariable(val, varName, from1: index, to1: index);
        }

        public void WriteBit(string varName, int index, int bit, bool val)
        {
            if (val)
                SetBits(varName, index, (uint) (1 << bit));
            else
                ClearBits(varName, index, (uint) (1 << bit));
        }

        public void ClearBits(string var, int index, int mask)
        {
            ClearBits(var, index, (uint) mask);
        }

        public void ClearBits(string var, int index, uint mask)
        {
            for (int index1 = 0; index1 < 32; ++index1) {
                if (((uint) (1 << index1) & mask) > 0U)
                    Command(string.Format("{0}({1}).{2} = 0", var, index, index1));
            }
        }

        public void SetBits(string var, int index, int mask)
        {
            SetBits(var, index, (uint) mask);
        }

        public void SetBits(string var, int index, uint mask)
        {
            for (int index1 = 0; index1 < 32; ++index1) {
                if (((uint) (1 << index1) & mask) > 0U)
                    Command(string.Format("{0}({1}).{2} = 1", var, index, index1));
            }
        }

        public bool ReadBit(string variable, int index, int mask)
        {
            return ReadBit(variable, index, (uint) mask);
        }

        public bool ReadBit(string variable, int index, uint mask)
        {
            if (!api.IsConnected) {
                logger.Info("AcsUtils.ReadBit: Controller not connected");
                return false;
            }

            try {
                return ((ulong) (int) api.ReadVariableAsScalar(variable, ProgramBuffer.ACSC_NONE, index) & mask) > 0UL;
            }
            catch (Exception e) {
                logger.Error($"AcsUtils.Command: Exception accessing '{variable}': " + e.Message);
                return false;
            }
        }

        private void Command(string cmd)
        {
            try {
                api.Command(cmd);
            }
            catch (Exception e) {
                logger.Error($"AcsUtils.Command: Exception sending '{cmd}': " + e.Message);
            }
        }
    }
}
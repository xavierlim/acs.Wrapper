using System;
using System.IO;
using System.Threading;
using ACS.SPiiPlusNET;
using CO.Common.Logger;

namespace CO.Systems.Services.Acs.AcsWrapper.util
{
    internal class AcsUtils
    {
        private readonly Api api;
        private readonly ILogger logger = LoggersManager.SystemLogger;

        public bool anyBufferChanged = false;
        private const int ACSC_MAX_LINE = 100000;

        public AcsUtils(Api api)
        {
            this.api = api;
        }

        public void ClearBuffer(ProgramBuffer buffer, int fromLine = 1, int toLine = 100000)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                try {
                    api.ClearBuffer(buffer, fromLine, toLine);
                }
                catch (Exception ex) {
                    logger.Info(
                        string.Format("failed to clear buffer {0} {1}", buffer, ex.Message));
                }
            }
        }

        public void CompareAndCompileBuffer(
            ProgramBuffer buffer,
            string program,
            bool run,
            string runLabel = null,
            bool runIfDifferent = false)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                string s = UploadBuffer(buffer);
                if (s == null)
                    s = "";
                else
                    s.Trim();
                StringReader stringReader1 = new StringReader(program);
                StringReader stringReader2 = new StringReader(s);
                bool flag = false;
                string str1;
                string str2;
                do {
                    str1 = stringReader1.ReadLine();
                    str2 = stringReader2.ReadLine();
                    if (str1 != null || str2 != null) {
                        if (str1 == null && str2 != null || str2 == null && str1 != null)
                            goto label_8;
                    }
                    else
                        goto label_12;
                } while ((uint) str1.Trim().CompareTo(str2.Trim()) <= 0U);

                goto label_10;
                label_8:
                flag = true;
                goto label_12;
                label_10:
                flag = true;
                label_12:
                if (flag) {
                    anyBufferChanged = true;
                    if (buffer == (ProgramBuffer) GetDBufferIndex()) {
                        // when there's any 'compiled' buffer exist in the controller, it does not allow modification to D-Buffer.
                        // compiling D-Buffer will change all the other buffers' status to 'not compiled', hence allowing
                        // modification to the D-Buffer
                        CompileBuffer(buffer);
                        LoadBuffer(buffer, program);
                        CompileBuffer(ProgramBuffer.ACSC_NONE);
                    }
                    else {
                        LoadBuffer(buffer, program);
                        CompileBuffer(buffer);
                    }
                }

                if (!run && !(flag & runIfDifferent))
                    return;
                RunBuffer(buffer, runLabel);
            }
        }

        public void CompileBuffer(ProgramBuffer buffer)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                try {
                    api.CompileBuffer(buffer);
                }
                catch (ACSException ae) {
                    logger.Error($"failed to compile buffer {buffer} {ae.Message}");
                }
                catch (Exception ex) {
                    logger.Info(
                        string.Format("failed to compile buffer {0} {1}", buffer, ex.Message));
                }
            }
        }

        public int GetDBufferIndex()
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
                return 0;
            }

            double num = 0.0;
            try {
                num = api.GetDBufferIndex();
            }
            catch (Exception ex) {
                logger.Info("failed to get DBuffer index " + ex.Message);
            }

            return (int) num;
        }

        public void LoadBuffer(ProgramBuffer buffer, string program)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                try {
                    api.LoadBuffer(buffer, program);
                }
                catch (Exception ex) {
                    logger.Info(string.Format("failed to load buffer {0} {1}", buffer, ex.Message));
                }
            }
        }

        public void RunBuffer(ProgramBuffer buffer, string label = null)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                try {
                    if (IsProgramRunning(buffer)) {
                        StopBuffer(buffer);
                        Thread.Sleep(100);
                    }

                    api.RunBuffer(buffer, label);
                }
                catch (Exception ex) {
                    logger.Info(
                        string.Format("failed to run buffer {0}:{1} {2}", buffer,
                            label == null ? "the top" : (object) label, ex.Message));
                }
            }
        }

        public void StopBuffer(ProgramBuffer buffer)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                try {
                    api.StopBuffer(buffer);
                }
                catch (Exception ex) {
                    logger.Info(string.Format("failed to stop buffer {0} {1}", buffer, ex.Message));
                }
            }
        }

        public string UploadBuffer(ProgramBuffer buffer)
        {
            try {
                return api.UploadBuffer(buffer);
            }
            catch (Exception ex) {
                logger.Info(string.Format("failed to upload buffer {0} {1}", buffer, ex.Message));
                return "";
            }
        }

        public bool IsProgramRunning(ProgramBuffer buffer)
        {
            if (api.IsConnected)
                return (uint) (api.GetProgramState(buffer) & ProgramStates.ACSC_PST_RUN) > 0U;
            logger.Info("Controller not connected");
            return false;
        }

        public object ReadVar(string varName, ProgramBuffer bufferIndex = ProgramBuffer.ACSC_NONE,
            int indFrom = -1, int indFromTo = -1)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
                return null;
            }

            try {
                return api.ReadVariable(varName, bufferIndex, indFrom, indFromTo);
            }
            catch (Exception ex) {
                logger.Info("ReadVar error " + ex.Message);
                return null;
            }
        }

        public object ReadVariableAsVector(string varName, ProgramBuffer bufferIndex = ProgramBuffer.ACSC_NONE,
            int From1 = -1, int To1 = -1, int From2 = -1, int To2 = -1)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
                return null;
            }

            try {
                return api.ReadVariableAsVector(varName, bufferIndex, From1, To1, From2, To2);
            }
            catch (Exception ex) {
                logger.Info("ReadVariableAsVector error " + ex.Message);
                return null;
            }
        }

        public object ReadVariableAsMatrix(string varName, ProgramBuffer bufferIndex = ProgramBuffer.ACSC_NONE,
            int From1 = -1, int To1 = -1, int From2 = -1, int To2 = -1)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
                return null;
            }

            try {
                return api.ReadVariableAsMatrix(varName, bufferIndex, From1, To1, From2, To2);
            }
            catch (Exception ex) {
                logger.Info("ReadVariableAsMatrix error " + ex.Message);
                return null;
            }
        }

        public void WriteVariable(object Value, string Variable, int NBuf = -1, int From1 = -1, int To1 = -1,
            int From2 = -1, int To2 = -1)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                try {
                    api.WriteVariable(Value, Variable, (ProgramBuffer) NBuf, From1, To1, From2, To2);
                }
                catch (Exception ex) {
                    logger.Info($"WriteVariable '{Variable}' error " + ex.Message);
                }
            }
        }

        public void WriteVar(double varVal, string varName)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                try {
                    api.WriteVariable(varVal, varName);
                }
                catch (Exception ex) {
                    logger.Info("WriteVar error " + ex.Message);
                }
            }
        }

        public void WriteVar(int[] num, string varName)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
            }
            else {
                try {
                    api.WriteVariable(num, varName, from1: 0, to1: (num.Length - 1));
                }
                catch (Exception ex) {
                    logger.Info("WriteVar error " + ex.Message);
                }
            }
        }

        public void WriteGlobalReal(object Value, string Variable, int indx = -1)
        {
            if (!api.IsConnected)
                logger.Info("Controller not connected");
            else
                api.WriteVariable(Value, Variable, from1: indx, to1: indx);
        }

        public double ReadGlobalReal(string Variable, int indx = -1)
        {
            if (!api.IsConnected) {
                logger.Info("Controller not connected");
                return 0.0;
            }

            try {
                return (double) api.ReadVariableAsScalar(Variable, ProgramBuffer.ACSC_NONE, indx);
            }
            catch (Exception ex) {
                logger.Info(ex.Message);
                return 0.0;
            }
        }

        public int ReadInt(string varName, int index)
        {
            return Convert.ToInt32(ReadVar(varName, indFrom: index, indFromTo: index));
        }

        public void WriteInt(string varName, int val)
        {
            WriteInt(varName, -1, val);
        }

        public void WriteInt(string varName, int index, int val)
        {
            WriteVariable(val, varName, From1: index, To1: index);
        }

        public void WriteBit(string varName, int index, int bit, bool val)
        {
            if (val)
                SetBits(varName, index, (uint) (1 << bit));
            else
                ClearBits(varName, index, (uint) (1 << bit));
        }

        public void ClearBits(string Var, int index, int mask)
        {
            ClearBits(Var, index, (uint) mask);
        }

        public void ClearBits(string Var, int index, uint mask)
        {
            for (int index1 = 0; index1 < 32; ++index1) {
                if (((uint) (1 << index1) & mask) > 0U)
                    Command(string.Format("{0}({1}).{2} = 0", Var, index, index1));
            }
        }

        public void SetBits(string Var, int index, int mask)
        {
            SetBits(Var, index, (uint) mask);
        }

        public void SetBits(string Var, int index, uint mask)
        {
            for (int index1 = 0; index1 < 32; ++index1) {
                if (((uint) (1 << index1) & mask) > 0U)
                    Command(string.Format("{0}({1}).{2} = 1", Var, index, index1));
            }
        }

        public bool ReadBit(string Var, int index, int mask)
        {
            return ReadBit(Var, index, (uint) mask);
        }

        public bool ReadBit(string Var, int index, uint mask)
        {
            if (api.IsConnected)
                return ((ulong) (int) api.ReadVariableAsScalar(Var, ProgramBuffer.ACSC_NONE, index) &
                        mask) > 0UL;
            logger.Info("Controller not connected");
            return false;
        }

        public void Command(string cmd)
        {
            try {
                api.Command(cmd);
            }
            catch (Exception ex) {
            }
        }

        public bool GetEtherCATSlaveIndex(int vendorID, int productID, int count, out int slaveIndex)
        {
            throw new NotImplementedException();
        }

        public bool GetEtherCATSlaveOffset(string ecatVarName, int slaveIndex, out int slaveOffset)
        {
            throw new NotImplementedException();
        }

        public bool MapEtherCATInput(int offset, string acsVarName)
        {
            throw new NotImplementedException();
        }

        public bool MapEtherCATOutput(int offset, string acsVarName)
        {
            throw new NotImplementedException();
        }
    }
}
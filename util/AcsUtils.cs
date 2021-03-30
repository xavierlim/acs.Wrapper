// Decompiled with JetBrains decompiler
// Type: AcsWrapperImpl.AcsUtils
// Assembly: AcsWrapper, Version=1.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: DC1EDF75-AE0E-403A-BB79-8497514E3B04
// Assembly location: D:\git\tfs\NextGen.UI\SQDev.complete\CO.Phoenix\Source\CO.Systems\CO.Systems\TestApps\Acs\AcsPlatform\lib\AcsWrapper.dll

using System;
using System.IO;
using System.Threading;
using ACS.SPiiPlusNET;
using CO.Common.Logger;

namespace CO.Systems.Services.Acs.AcsWrapper.util
{
  internal class AcsUtils
  {
    private Api Ch = (Api) null;
    private readonly ILogger _logger = LoggersManager.SystemLogger;
    public bool anyBufferChanged = false;
    private const int ACSC_MAX_LINE = 100000;

    public AcsUtils(Api ch)
    {
      this.Ch = ch;
    }

    public void ClearBuffer(ProgramBuffer buffer, int fromLine = 1, int toLine = 100000)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 28, nameof (ClearBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      }
      else
      {
        try
        {
          this.Ch.ClearBuffer(buffer, fromLine, toLine);
        }
        catch (Exception ex)
        {
          this._logger.Info(string.Format("failed to clear buffer {0} {1}", (object) buffer, (object) ex.Message), 38, nameof (ClearBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
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
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 46, nameof (CompareAndCompileBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      }
      else
      {
        string s = this.UploadBuffer(buffer);
        if (s == null)
          s = "";
        else
          s.Trim();
        StringReader stringReader1 = new StringReader(program);
        StringReader stringReader2 = new StringReader(s);
        bool flag = false;
        string str1;
        string str2;
        do
        {
          str1 = stringReader1.ReadLine();
          str2 = stringReader2.ReadLine();
          if (str1 != null || str2 != null)
          {
            if (str1 == null && str2 != null || str2 == null && str1 != null)
              goto label_8;
          }
          else
            goto label_12;
        }
        while ((uint) str1.Trim().CompareTo(str2.Trim()) <= 0U);
        goto label_10;
label_8:
        flag = true;
        goto label_12;
label_10:
        flag = true;
label_12:
        if (flag)
        {
          this.anyBufferChanged = true;
          if (buffer == (ProgramBuffer) this.GetDBufferIndex())
          {
            // when there's any 'compiled' buffer exist in the controller, it does not allow modification to D-Buffer.
            // compiling D-Buffer will change all the other buffers' status to 'not compiled', hence allowing
            // modification to the D-Buffer
            CompileBuffer(buffer);
            LoadBuffer(buffer, program);
            CompileBuffer(ProgramBuffer.ACSC_NONE);
          }
          else {
            this.LoadBuffer(buffer, program);
            this.CompileBuffer(buffer);
          }
        }
        if (!run && !(flag & runIfDifferent))
          return;
        this.RunBuffer(buffer, runLabel);
      }
    }

    public void CompileBuffer(ProgramBuffer buffer)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 128, nameof (CompileBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      }
      else
      {
        try {
          this.Ch.CompileBuffer(buffer);
        }
        catch (ACS.SPiiPlusNET.ACSException ae) {
          _logger.Error($"failed to compile buffer {buffer} {ae.Message}");
        }
        catch (Exception ex)
        {
          this._logger.Info(string.Format("failed to compile buffer {0} {1}", (object) buffer, (object) ex.Message), 138, nameof (CompileBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        }
      }
    }

    public int GetDBufferIndex()
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 146, nameof (GetDBufferIndex), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        return 0;
      }
      double num = 0.0;
      try
      {
        num = this.Ch.GetDBufferIndex();
      }
      catch (Exception ex)
      {
        this._logger.Info("failed to get DBuffer index " + ex.Message, 158, nameof (GetDBufferIndex), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      }
      return (int) num;
    }

    public void LoadBuffer(ProgramBuffer buffer, string program)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 166, nameof (LoadBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      }
      else
      {
        try
        {
          this.Ch.LoadBuffer(buffer, program);
        }
        catch (Exception ex)
        {
          this._logger.Info(string.Format("failed to load buffer {0} {1}", (object) buffer, (object) ex.Message), 176, nameof (LoadBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        }
      }
    }

    public void RunBuffer(ProgramBuffer buffer, string label = null)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 183, nameof (RunBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      }
      else
      {
        try
        {
          if (this.IsProgramRunning(buffer))
          {
            this.StopBuffer(buffer);
            Thread.Sleep(100);
          }
          this.Ch.RunBuffer(buffer, label);
        }
        catch (Exception ex)
        {
          this._logger.Info(string.Format("failed to run buffer {0}:{1} {2}", (object) buffer, label == null ? (object) "the top" : (object) label, (object) ex.Message), 198, nameof (RunBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        }
      }
    }

    public void StopBuffer(ProgramBuffer buffer)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 206, nameof (StopBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      }
      else
      {
        try
        {
          this.Ch.StopBuffer(buffer);
        }
        catch (Exception ex)
        {
          this._logger.Info(string.Format("failed to stop buffer {0} {1}", (object) buffer, (object) ex.Message), 216, nameof (StopBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        }
      }
    }

    public string UploadBuffer(ProgramBuffer buffer)
    {
      try
      {
        return this.Ch.UploadBuffer(buffer);
      }
      catch (Exception ex)
      {
        this._logger.Info(string.Format("failed to upload buffer {0} {1}", (object) buffer, (object) ex.Message), 229, nameof (UploadBuffer), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        return "";
      }
    }

    public bool IsProgramRunning(ProgramBuffer buffer)
    {
      if (this.Ch.IsConnected)
        return (uint) (this.Ch.GetProgramState(buffer) & ProgramStates.ACSC_PST_RUN) > 0U;
      this._logger.Info("Controller not connected", 237, nameof (IsProgramRunning), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      return false;
    }

    public object ReadVar(string varName, ProgramBuffer bufferIndex = ProgramBuffer.ACSC_NONE, int indFrom = -1, int indFromTo = -1)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 250, nameof (ReadVar), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        return (object) null;
      }
      try
      {
        return this.Ch.ReadVariable(varName, bufferIndex, indFrom, indFromTo);
      }
      catch (Exception ex)
      {
        this._logger.Info("ReadVar error " + ex.Message, 259, nameof (ReadVar), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        return (object) null;
      }
    }

    public object ReadVariableAsVector(
      string varName,
      ProgramBuffer bufferIndex = ProgramBuffer.ACSC_NONE,
      int From1 = -1,
      int To1 = -1,
      int From2 = -1,
      int To2 = -1)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 271, nameof (ReadVariableAsVector), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        return (object) null;
      }
      try
      {
        return this.Ch.ReadVariableAsVector(varName, bufferIndex, From1, To1, From2, To2);
      }
      catch (Exception ex)
      {
        this._logger.Info("ReadVariableAsVector error " + ex.Message, 282, nameof (ReadVariableAsVector), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        return (object) null;
      }
    }

    public object ReadVariableAsMatrix(
      string varName,
      ProgramBuffer bufferIndex = ProgramBuffer.ACSC_NONE,
      int From1 = -1,
      int To1 = -1,
      int From2 = -1,
      int To2 = -1)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 292, nameof (ReadVariableAsMatrix), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        return (object) null;
      }
      try
      {
        return this.Ch.ReadVariableAsMatrix(varName, bufferIndex, From1, To1, From2, To2);
      }
      catch (Exception ex)
      {
        this._logger.Info("ReadVariableAsMatrix error " + ex.Message, 301, nameof (ReadVariableAsMatrix), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        return (object) null;
      }
    }

    public void WriteVariable(
      object Value,
      string Variable,
      int NBuf = -1,
      int From1 = -1,
      int To1 = -1,
      int From2 = -1,
      int To2 = -1)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 313, nameof (WriteVariable), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      }
      else
      {
        try
        {
          this.Ch.WriteVariable(Value, Variable, (ProgramBuffer) NBuf, From1, To1, From2, To2);
        }
        catch (Exception ex)
        {
          this._logger.Info("WriteVariable error " + ex.Message, 322, nameof (WriteVariable), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        }
      }
    }

    public void WriteVar(double varVal, string varName)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 330, nameof (WriteVar), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      }
      else
      {
        try
        {
          this.Ch.WriteVariable((object) varVal, varName);
        }
        catch (Exception ex)
        {
          this._logger.Info("WriteVar error " + ex.Message, 339, nameof (WriteVar), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        }
      }
    }

    public void WriteVar(int[] num, string varName)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 347, nameof (WriteVar), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      }
      else
      {
        try
        {
          this.Ch.WriteVariable((object) num, varName, from1: 0, to1: (num.Length - 1));
        }
        catch (Exception ex)
        {
          this._logger.Info("WriteVar error " + ex.Message, 356, nameof (WriteVar), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        }
      }
    }

    public void WriteGlobalReal(object Value, string Variable, int indx = -1)
    {
      if (!this.Ch.IsConnected)
        this._logger.Info("Controller not connected", 364, nameof (WriteGlobalReal), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      else
        this.Ch.WriteVariable(Value, Variable, from1: indx, to1: indx);
    }

    public double ReadGlobalReal(string Variable, int indx = -1)
    {
      if (!this.Ch.IsConnected)
      {
        this._logger.Info("Controller not connected", 373, nameof (ReadGlobalReal), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        return 0.0;
      }
      try
      {
        return (double) this.Ch.ReadVariableAsScalar(Variable, ProgramBuffer.ACSC_NONE, indx);
      }
      catch (Exception ex)
      {
        this._logger.Info(ex.Message, 382, nameof (ReadGlobalReal), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
        return 0.0;
      }
    }

    public int ReadInt(string varName, int indx)
    {
      return Convert.ToInt32(this.ReadVar(varName, indFrom: indx, indFromTo: indx));
    }

    public void WriteInt(string varName, int val)
    {
      this.WriteInt(varName, -1, val);
    }

    public void WriteInt(string varName, int index, int val)
    {
      this.WriteVariable((object) val, varName, From1: index, To1: index);
    }

    public void WriteBit(string varName, int index, int bit, bool val)
    {
      if (val)
        this.SetBits(varName, index, (uint) (1 << bit));
      else
        this.ClearBits(varName, index, (uint) (1 << bit));
    }

    public void ClearBits(string Var, int index, int mask)
    {
      this.ClearBits(Var, index, (uint) mask);
    }

    public void ClearBits(string Var, int index, uint mask)
    {
      for (int index1 = 0; index1 < 32; ++index1)
      {
        if (((uint) (1 << index1) & mask) > 0U)
          this.Command(string.Format("{0}({1}).{2} = 0", (object) Var, (object) index, (object) index1));
      }
    }

    public void SetBits(string Var, int index, int mask)
    {
      this.SetBits(Var, index, (uint) mask);
    }

    public void SetBits(string Var, int index, uint mask)
    {
      for (int index1 = 0; index1 < 32; ++index1)
      {
        if (((uint) (1 << index1) & mask) > 0U)
          this.Command(string.Format("{0}({1}).{2} = 1", (object) Var, (object) index, (object) index1));
      }
    }

    public bool ReadBit(string Var, int index, int mask)
    {
      return this.ReadBit(Var, index, (uint) mask);
    }

    public bool ReadBit(string Var, int index, uint mask)
    {
      if (this.Ch.IsConnected)
        return ((ulong) (int) this.Ch.ReadVariableAsScalar(Var, ProgramBuffer.ACSC_NONE, index) & (ulong) mask) > 0UL;
      this._logger.Info("Controller not connected", 444, nameof (ReadBit), "C:\\TruckProject\\trunk\\ExternalHardware\\AcsWrapper\\AcsUtils.cs");
      return false;
    }

    public void Command(string cmd)
    {
      try
      {
        this.Ch.Command(cmd);
      }
      catch (Exception ex)
      {
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

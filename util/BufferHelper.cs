
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ACS.SPiiPlusNET;
using CO.Common.Logger;
using CO.Systems.Services.Acs.AcsWrapper.wrapper.exceptions;
using CO.Systems.Services.Acs.AcsWrapper.wrapper.models;
using CO.Systems.Services.Robot.RobotBase;

namespace CO.Systems.Services.Acs.AcsWrapper.util
{
    internal class BufferHelper
    {
        private const string DefaultBuffersDirectory = "AppData\\acs\\buffers";
        private const string BuffersRealSubDir = "real";
        private const string BuffersSimulationSubDir = "simulation";
        private const string BufferExtension = ".prg";

        private readonly string buffersDirectory;
        private readonly ILogger logger;

        private bool changesMadeToBuffer;

        private bool IsSimulation { get; }

        private Api Api { get; }
        private AcsUtils AcsUtils { get; }

        public BufferHelper(Api acsApi, AcsUtils acsUtils, ILogger logger, bool isSimulation, string buffersDirectory = null)
        {
            Api = acsApi;
            AcsUtils = acsUtils;
            IsSimulation = isSimulation;

            this.logger = logger;

            if (buffersDirectory == null) {
                this.buffersDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultBuffersDirectory);
            }
            else {
                this.buffersDirectory = buffersDirectory;
            }

            this.buffersDirectory = Path.Combine(this.buffersDirectory,
                IsSimulation ? BuffersSimulationSubDir : BuffersRealSubDir);
        }

        public bool IsProgramRunning(ProgramBuffer index)
        {
            try {
                return (uint) (Api.GetProgramState(index) & ProgramStates.ACSC_PST_RUN) > 0U;
            }
            catch (Exception ex) {
                logger.Error(string.Format("BufferHelper.IsProgramRunning: Exception {0}: {1}", index, ex.Message));
                return false;
            }
        }

        public void WaitProgramEnd(ProgramBuffer index, int timeout)
        {
            if (IsProgramRunning(index)) {
                Api.WaitProgramEnd(index, timeout);
            }
        }

        public void RunBuffer(ProgramBuffer index, string label = null)
        {
            if (!Api.IsConnected) {
                logger.Info("BufferHelper.RunBuffer: Controller not connected");
                return;
            }

            try {
                StopBufferIfRunning(index);
                Api.RunBuffer(index, label);
            }
            catch (Exception ex) {
                logger.Error(
                    string.Format("BufferHelper.RunBuffer: Failed to run buffer {0}:{1} {2}", index,
                        label == null ? "the top" : (object) label, ex.Message));
            }
        }

        public void StopBuffer(ProgramBuffer index)
        {
            if (!Api.IsConnected) {
                logger.Info("BufferHelper.StopBuffer: Controller not connected");
                return;
            }

            try {
                Api.StopBuffer(index);
            }
            catch (Exception ex) {
                logger.Error(string.Format("AcsUtil.ReadVar: Failed to stop buffer {0}: {1}", index, ex.Message));
            }
        }

        public void StopAllBuffers()
        {
            try {
                Api.StopBuffer(ProgramBuffer.ACSC_NONE);
            }
            catch (Exception e) {
                throw new AcsException("Failed to stop all buffer. Exception: " + e.Message);
            }
        }

        private void StopBufferIfRunning(ProgramBuffer index)
        {
            if (IsProgramRunning(index)) {
                Api.StopBuffer(index);
                Thread.Sleep(100);
            }
        }

        public void FlashAllBuffers()
        {
            if (!changesMadeToBuffer) return;
            try {
                Api.ControllerSaveToFlash(null, new[] {ProgramBuffer.ACSC_BUFFER_ALL}, null, null);
            }
            catch (Exception e) {
                throw new AcsException("Failed to flash all buffer. Exception: " + e.Message);
            }
        }

        public void InitGantryHomingBuffers()
        {
            WriteBuffer(AcsBuffers.GantryHomeX);
            WriteBuffer(AcsBuffers.GantryHomeY);
            WriteBuffer(AcsBuffers.GantryHomeZ);
        }

        public void InitConveyorHomingBuffers()
        {
            WriteBuffer(AcsBuffers.ConveyorHoming);
            WriteBuffer(AcsBuffers.WidthHoming);
            WriteBuffer(AcsBuffers.LifterHoming);
        }

        public void InitConveyorResetBuffers()
        {
            WriteBuffer(AcsBuffers.ConveyorReset);
        }

        public void InitIoBuffer()
        {
            WriteBuffer(AcsBuffers.InitIo);
        }

        /// <summary>
        /// Initialize conveyor related buffers
        /// </summary>
        public void InitConveyorBuffers()
        {
            WriteBuffer(AcsBuffers.BypassMode);
            WriteBuffer(AcsBuffers.ChangeWidth);
            WriteBuffer(AcsBuffers.EmergencyStop);
            WriteBuffer(AcsBuffers.FreePanel);
            WriteBuffer(AcsBuffers.InternalMachineLoad);
            WriteBuffer(AcsBuffers.LoadPanel);
            WriteBuffer(AcsBuffers.PowerOnRecoverFromEmergencyStop);
            WriteBuffer(AcsBuffers.PreReleasePanel);
            WriteBuffer(AcsBuffers.ReleasePanel);
            WriteBuffer(AcsBuffers.ReloadPanel);
            WriteBuffer(AcsBuffers.SecurePanel);
            WriteBuffer(AcsBuffers.InternalErrorExit);
        }

        /// <summary>
        /// Initialize D-Buffer
        /// </summary>
        public void InitDBuffer()
        {
            int index;
            try {
                index = (int) Api.GetDBufferIndex();
            }
            catch (Exception e) {
                throw new AcsException("Failed to get D-Buffer index. Exception: " + e.Message);
            }

            WriteDBuffer(index);

            AcsUtils.WriteVariable(AcsBuffers.InternalMachineLoad, "InternalMachineLoadBufferIndex", index);
            AcsUtils.WriteVariable(AcsBuffers.InternalErrorExit, "InternalErrorExitBufferIndex", index);
            AcsUtils.WriteVariable(AcsBuffers.FreePanel, "FreePanelBufferIndex", index);
            AcsUtils.WriteVariable(AcsBuffers.SecurePanel, "SecurePanelBufferIndex", index);
            AcsUtils.WriteVariable(AcsBuffers.BypassMode, "BypassModeBufferIndex", index);
            AcsUtils.WriteVariable(AcsBuffers.WidthHoming, "HomeConveyorBufferIndex", index);
            AcsUtils.WriteVariable(AcsBuffers.LoadPanel, "LoadPanelBufferIndex", index);
            AcsUtils.WriteVariable(AcsBuffers.ReleasePanel, "ReleasePanelBufferIndex", index);
            AcsUtils.WriteVariable(AcsBuffers.ReloadPanel, "ReloadPanelBufferIndex", index);
            AcsUtils.WriteVariable(AcsBuffers.ConveyorReset, "ConveyorResetBufferIndex", index);
            AcsUtils.WriteVariable(AcsBuffers.WidthHoming, "WidthHomingBufferIndex", index);
        }

        /// <summary>
        /// Prepare scanning buffer for hardware trigger run
        /// </summary>
        public void PrepareScanningBuffer(List<IPvTuple3D> motionPaths, int triggerToCameraStartPort, int triggerToCameraStartBit, int triggerFromCameraContinuePort, int triggerFromCameraContinueBit, int triggerFromCameraTimeOut)
        {
            var motionCount = motionPaths.Count;
            WriteScanningBuffer(motionCount, triggerToCameraStartPort, triggerToCameraStartBit,
                triggerFromCameraContinuePort, triggerFromCameraContinueBit, triggerFromCameraTimeOut);

            double[,] positions = new double[motionCount, 3];
            double[,] velocity = new double[motionCount, 3];
            double[,] delays = new double[motionCount, 3];
            int index = 0;
            foreach (var path in motionPaths) {
                positions[index, 0] = path.PvTuple[0].Position;
                positions[index, 1] = path.PvTuple[1].Position;
                positions[index, 2] = path.PvTuple[2].Position;
                velocity[index, 0] = path.PvTuple[0].Velocity;
                velocity[index, 1] = path.PvTuple[1].Velocity;
                velocity[index, 2] = path.PvTuple[2].Velocity;
                delays[index, 0] = path.PvTuple[0].Delay;
                delays[index, 1] = path.PvTuple[1].Delay;
                delays[index, 2] = path.PvTuple[2].Delay;
                ++index;
            }

            const int bufferIndex = (int) AcsBuffers.Scanning;
            AcsUtils.WriteVariable(positions, "SCAN_POINTS", bufferIndex);
            AcsUtils.WriteVariable(velocity, "SCAN_POINTS_VELOCITY", bufferIndex);
            AcsUtils.WriteVariable(delays, "SCAN_POINTS_DELAY", bufferIndex);
            AcsUtils.WriteVariable(0, "START_SCAN_POINT_INDEX", bufferIndex);
            AcsUtils.WriteVariable(motionCount - 1, "END_SCAN_POINT_INDEX", bufferIndex);
            AcsUtils.WriteVariable(0, "IS_NEED_WAIT_CONTINUE_COMMAND", bufferIndex);
        }

        private void WriteScanningBuffer(int motionCount, int triggerToCameraStartPort, int triggerToCameraStartBit,
            int triggerFromCameraContinuePort, int triggerFromCameraContinueBit, int triggerFromCameraTimeOut)
        {
            string rawBuffer = ReadBuffer(AcsBuffers.Scanning);

            string scanBuffer = rawBuffer
                .Replace("_NR_SCAN_POINTS_", motionCount.ToString())
                .Replace("_Z_AXIS_INDEX_", "0")
                .Replace("_X_AXIS_INDEX_", "1").Replace("_Y_AXIS_INDEX_", "2")
                .Replace("_TRIGGER_PORT_TO_CAMERA_START_", triggerToCameraStartPort.ToString())
                .Replace("_TRIGGER_BIT_TO_CAMERA_START_", triggerToCameraStartBit.ToString())
                .Replace("_TRIGGER_PORT_FROM_CAMERA_CONTINUE_", triggerFromCameraContinuePort.ToString())
                .Replace("_TRIGGER_BIT_FROM_CAMERA_CONTINUE_", triggerFromCameraContinueBit.ToString())
                .Replace("_TRIGGER_FROM_CAMERA_TIME_OUT_", triggerFromCameraTimeOut.ToString());

            WriteBuffer((ProgramBuffer) AcsBuffers.Scanning, scanBuffer);
        }

        private void WriteDBuffer(int bufferNumber)
        {
            var buffer = ReadBuffer("d_buffer");

            var index = (ProgramBuffer) bufferNumber;
            if (!CompareBuffer(index, buffer)) return;
            try {
                // when there's any 'compiled' buffer exist in the controller, it does not allow modification to D-Buffer.
                // compiling D-Buffer will change all the other buffers' status to 'not compiled', hence allowing
                // modification to the D-Buffer
                if (!IsBufferEmpty(index)) {
                    Api.CompileBuffer(index);
                }
                Api.LoadBuffer(index, buffer);
                Api.CompileBuffer(ProgramBuffer.ACSC_NONE);

                changesMadeToBuffer = true;
            }
            catch (Exception e) {
                throw new AcsException("Failed to load and compile D-Buffer. Exception: " + e.Message);
            }
        }

        public void WriteBuffer(AcsBuffers bufferNumber)
        {
            var buffer = ReadBuffer(bufferNumber);

            var index = (ProgramBuffer) bufferNumber;
            if (!CompareBuffer(index, buffer)) return;

            StopBufferIfRunning(index);
            try {
                Api.LoadBuffer(index, buffer);
                Api.CompileBuffer(index);
                changesMadeToBuffer = true;
            }
            catch (Exception e) {
                throw new AcsException($"Failed to load and compile buffer {bufferNumber}. Exception: " + e.Message);
            }
        }

        private void WriteBuffer(ProgramBuffer index, string buffer)
        {
            if (!CompareBuffer(index, buffer)) return;

            StopBufferIfRunning(index);
            try {
                Api.LoadBuffer(index, buffer);
                Api.CompileBuffer(index);
                changesMadeToBuffer = true;
            }
            catch (Exception e) {
                throw new AcsException($"Failed to load and compile buffer {index}. Exception: " + e.Message);
            }
        }

        private bool IsBufferEmpty(ProgramBuffer bufferIndex)
        {
            string buffer;
            try {
                buffer = Api.UploadBuffer(bufferIndex);
            }
            catch (Exception e) {
                throw new AcsException("Failed to upload buffer. Exception: " + e.Message);
            }

            return buffer == null;
        }

        /// <summary>
        /// compare buffer to be written with existing buffer on the ACS controller (if any)
        /// </summary>
        /// <param name="bufferIndex">index of the buffer</param>
        /// <param name="toWrite">buffer to be written</param>
        /// <returns>true when there's different between the buffer, or the buffer on the controller is empty</returns>
        /// <exception cref="AcsException">triggered when buffer uploading failed</exception>
        private bool CompareBuffer(ProgramBuffer bufferIndex, string toWrite)
        {
            string toCompare;
            try {
                toCompare = Api.UploadBuffer(bufferIndex);
            }
            catch (Exception e) {
                throw new AcsException("Failed to upload buffer. Exception: " + e.Message);
            }

            if (toCompare == null) return true;

            var writeReader = new StringReader(toWrite);
            var compareReader = new StringReader(toCompare);
            string writeLine;
            string compareLine;

            do {
                writeLine = writeReader.ReadLine();
                compareLine = compareReader.ReadLine();

                if (writeLine == null) {
                    return compareLine != null;
                }
                if (compareLine == null) {
                    return true;
                }
            } while (writeLine.CompareTo(compareLine) == 0);

            return true;
        }

        private string GetBufferFilename(AcsBuffers bufferNumber)
        {
            switch (bufferNumber) {
                case AcsBuffers.GantryHomeX:
                    return "Gantry_X_Homing";
                case AcsBuffers.GantryHomeY:
                    return "Gantry_Y_Homing";
                case AcsBuffers.GantryHomeZ:
                    return "Gantry_Z_Homing";
                case AcsBuffers.HomeX:
                    return "";
                case AcsBuffers.HomeY:
                    return "";
                case AcsBuffers.HomeZ:
                    return "";
                case AcsBuffers.Scanning:
                    return "ScanningBuffer";
                case AcsBuffers.BypassMode:
                    return "BypassModeBuffer";
                case AcsBuffers.ChangeWidth:
                    return "ChangeWidthBuffer";
                case AcsBuffers.EmergencyStop:
                    return "EmergencyStopBuffer";
                case AcsBuffers.FreePanel:
                    return "FreePanelBuffer";
                case AcsBuffers.InternalMachineLoad:
                    return "InternalMachineLoadBuffer";
                case AcsBuffers.LoadPanel:
                    return "LoadPanelBuffer";
                case AcsBuffers.PowerOnRecoverFromEmergencyStop:
                    return "PowerOnRecoverFromEmergencyStopBuffer";
                case AcsBuffers.PreReleasePanel:
                    return "PreReleasePanelBuffer";
                case AcsBuffers.ReleasePanel:
                    return "ReleasePanelBuffer";
                case AcsBuffers.ReloadPanel:
                    return "ReloadPanelBuffer";
                case AcsBuffers.SecurePanel:
                    return "SecurePanelBuffer";
                case AcsBuffers.InternalErrorExit:
                    return "InternalErrorExitBuffer";
                case AcsBuffers.ConveyorHoming:
                    return "ConveyorHomingBuffer";
                case AcsBuffers.WidthHoming:
                    return "WidthHoming";
                case AcsBuffers.LifterHoming:
                    return "LifterHoming";
                case AcsBuffers.ConveyorReset:
                    return "WidthLifterConveyorReset";
                case AcsBuffers.InitIo:
                    return "IO_InitializationBuffer";
                default:
                    throw new ArgumentOutOfRangeException(nameof(bufferNumber), bufferNumber, null);
            }
        }

        private string ReadBuffer(AcsBuffers bufferNumber)
        {
            return ReadBuffer(GetBufferFilename(bufferNumber));
        }

        private string ReadBuffer(string bufferName)
        {
            try {
                var path = GetBufferPath(bufferName);
                if (path == null) throw new AcsException("Failed to read buffer file. File not exist");

                var buffer = File.ReadAllText(path);

                // scan through the lines to remove Program Record lines for buffer script saved from ACS controller
                var lines = Regex.Split(buffer, Environment.NewLine);
                int lineToRemove = 0;
                foreach (var line in lines) {
                    if (line.StartsWith("#")) lineToRemove++;
                    else break;
                }
                buffer = string.Join(Environment.NewLine, lines.Skip(lineToRemove).ToArray());

                return buffer;
            }
            catch (AcsException) {
                throw;
            }
            catch (Exception e) {
                throw new AcsException("Failed to read buffer file. Exception: " + e.Message);
            }
        }

        private string GetBufferPath(string bufferName)
        {
            var path = GetBufferPath(buffersDirectory, bufferName);
            if (path == null) {
                if (!bufferName.EndsWith(BufferExtension)) {
                    bufferName += BufferExtension;
                    path = GetBufferPath(buffersDirectory, bufferName);
                }
            }

            return path;
        }

        private string GetBufferPath(string directory, string bufferName)
        {
            var path = Path.Combine(directory, bufferName);
            if (!File.Exists(path)) {
                var subDirectories = Directory.GetDirectories(directory);
                foreach (var subDirectory in subDirectories) {
                    path = GetBufferPath(subDirectory, bufferName);
                    if (path != null) return path;
                }

                return null;
            }

            return path;
        }
    }
}
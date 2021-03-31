//===================================================================================
// Copyright (c) CyberOptics Corporation. All rights reserved. The
// copyright to the computer program herein is the property of CyberOptics.
// The program may be used or copied or both only with the written permission
// of CyberOptics or in accordance with the terms and conditions stipulated
// in the agreement or contract under which the program has been supplied.
// This copyright notice must not be removed.
//===================================================================================

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.models
{
    public class SetOutputParameters
    {
        public bool LockStopper;
        public bool RaiseBoardStopStopper;
        public bool BeltShroudVacuumOn;
        public bool ClampPanel;
        public bool ResetButtonLight;
        public bool StartButtonLight;
        public bool StopButtonLight;
        public bool TowerLightRed;
        public bool TowerLightYellow;
        public bool TowerLightGreen;
        public bool TowerLightBlue;
        public bool TowerLightBuzzer;
        public bool StopSensor;
        public bool SmemaUpStreamMachineReady;
        public bool DownStreamBoardAvailable;
        public bool SmemaDownStreamFailedBoardAvailable;
    }
}
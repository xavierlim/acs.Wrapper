//===================================================================================
// Copyright (c) CyberOptics Corporation. All rights reserved. The
// copyright to the computer program herein is the property of CyberOptics.
// The program may be used or copied or both only with the written permission
// of CyberOptics or in accordance with the terms and conditions stipulated
// in the agreement or contract under which the program has been supplied.
// This copyright notice must not be removed.
//===================================================================================

using CO.Systems.Services.Acs.AcsWrapper.config;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper
{
    public static class AcsWrapperFactory
    {
        public static IAcsWrapper CreateInstance()
        {
            if (AcsSimHelper.IsEnable()) {
                AcsSimHelper.Start();
                if (AcsSimHelper.Config.SimulationMode == SimulationMode.MockPlatform) {
                    return new AcsMocker();
                }
            }

            return new AcsWrapper();
        }
    }
}
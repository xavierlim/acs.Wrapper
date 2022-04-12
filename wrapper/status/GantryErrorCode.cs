//===================================================================================
// Copyright (c) CyberOptics Corporation. All rights reserved. The
// copyright to the computer program herein is the property of CyberOptics.
// The program may be used or copied or both only with the written permission
// of CyberOptics or in accordance with the terms and conditions stipulated
// in the agreement or contract under which the program has been supplied.
// This copyright notice must not be removed.
//===================================================================================

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.status
{
    /// <summary>
    /// Error code for gantry operation. Error code for possible fault during scanning buffer
    /// run are also included here
    /// </summary>
    public enum GantryErrorCode
    {
        NoError,

        ZPositiveHardLimitHit,
        XPositiveHardLimitHit,
        YPositiveHardLimitHit,
        ZNegativeHardLimitHit,
        XNegativeHardLimitHit,
        YNegativeHardLimitHit,
        ZPositiveSoftLimitHit,
        XPositiveSoftLimitHit,
        YPositiveSoftLimitHit,
        ZNegativeSoftLimitHit,
        XNegativeSoftLimitHit,
        YNegativeSoftLimitHit,
        
        CameraBusyMissing = 901,
        CameraBusyStuck = 902,
        ConveyorAxisError = 903,
        
        XEncoderError = 904,
        XOverHeat,
        XOverCurrent,
        XCriticalPositionError,
        XHitLimit,
        YEncoderError,
        YOverHeat,
        YOverCurrent,
        YCriticalPositionError,
        YHitLimit,
        ZError,
    }
}
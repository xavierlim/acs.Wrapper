
namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.models
{
    public enum ConveyorErrorCode
    {
        Error_Safe = 0,

        BypassSensorBlockedError = 101,
        BypassAcqError = 102,
        BypassExitError = 103,
        BypassReleaseError = 104,
        BypassSmemaError = 105,

        ChangeWidthToError = 201,
        ChangeWidthToHomedError = 202,
        ChangeWidthToNotAtSpecifiedError = 203,
        ChangewidthPanelPresent = 204,

        FreePanelStopUpError = 301,
        FreePanelOptoBlockedError = 302,
        FreePanelToUnliftError = 303,
        FreePanelToUnclampError = 304,

        LoadPanelNotReleasedError = 401,
        LoadPanelSensorBlockedError = 402,
        LoadPanelAcqError = 403,
        LoadPanelSlowSensorError = 404,
        LoadPanelAlignBeforeSlowSensorError = 405,
        LoadPanelAlignError = 406,
        LoadPanelSecureError = 407,

        PreReleaseNotLoadedError = 501,
        PreReleasePanelNotFreedError = 502,
        PreReleaseWaitTOError = 503,

        ReleasePanelStateError = 601,
        ReleasePanelFreeError = 602,
        ReleasePanelExitError = 603,
        ReleasePanelReleaseError = 604,
        ReleasePanelSmemaError = 605,

        ReloadPanelStateError = 701,
        ReloadPanelFreeError = 702,
        ReloadPanelSearchError = 703,
        ReloadPanelSlowSensorError = 704,

        SecurePanelToClampedError = 801,
        SecurePanelToLiftedError = 802,
        SecurePanelToUnstopError = 803,

        PowerOnRecoveryWidthNotHomed = 901
    }

}
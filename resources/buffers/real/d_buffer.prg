global int I(100),I0,I1,I2,I3,I4,I5,I6,I7,I8,I9,I90,I91,I92,I93,I94,I95,I96,I97,I98,I99
global real V(100),V0,V1,V2,V3,V4,V5,V6,V7,V8,V9,V90,V91,V92,V93,V94,V95,V96,V97,V98,V99

!homing variables
global real HOME_VEL_IN(10)
global real HOME_VEL_OUT(10)
global real HOME_OFFSET(10)

global int X_AXIS
global int Y_AXIS
global int Z_AXIS

global int CONVEYOR_AXIS
global int CONVEYOR_WIDTH_AXIS
global int LIFTER_AXIS

global int ECOFFSETM(3)

global int MotionSettlingTimeBeforeScan
global int BeforeMoveDelay


global int SAFE_STATUS = 0,LOADED_STATUS = 1,ERROR_STATUS = 2
global int PRERELEASED_STATUS = 3 ,RELEASED_STATUS = 4,CHANGING_WIDTH_STATUS = 5
global int SEARGING_STATUS = 6,WIDTH_HOMING_STATUS = 7,LOADING_STATUS = 8,RELOADING_STATUS = 9
global int PRERELEASING_STATUS = 10, RELEASING_STATUS = 11,BYPASS_STATUS = 12

global int ControlWord_Conveyor, ControlWord_Width, ControlWord_Lifter 
global real ActualPos_Conveyor, ActualPos_Width, ActualPos_Lifter
global int Touch_Probe_Function 

global ERROR_SAFE = 0
global int CURRENT_STATUS
global int ERROR_CODE

!Gantry State Definition
global int GANTRY_STATE_NORMAL = 0, GANTRY_STATE_HOMING = 1, GANTRY_STATE_SCANNING = 2, GANTRY_STATE_ERROR = 3

!Gantry Error Definition
global int GANTRY_ERROR_GENERAL = 0

global int GANTRY_STATUS
global int GANTRY_ERROR

!BypassSensorBlockedError = 101
!BypassAcqError = 102
!BypassExitError = 103
!BypassReleaseError = 104
!BypassSmemaError = 105

!ChangeWidthToError = 201
!ChangeWidthToHomedError = 202
!ChangeWidthToNotAtSpecifiedError = 203
!ChangewidthPanelPresent = 204

!FreePanelStopUpError = 301
!FreePanelOptoBlockedError = 302
!FreePanelToUnliftError = 303
!FreePanelToUnclampError = 304

!LoadPanelNotReleasedError = 401
!LoadPanelSensorBlockedError = 402
!LoadPanelAcqError = 403
!LoadPanelSlowSensorError = 404
!LoadPanelAlignBeforeSlowSensorError = 405
!LoadPanelAlignError = 406
!LoadPanelSecureError = 407

!PreReleaseNotLoadedError = 501
!PreReleasePanelNotFreedError = 502
!PreReleaseWaitTOError = 503

!ReleasePanelStateError = 601
!ReleasePanelFreeError = 602
!ReleasePanelExitError = 603
!ReleasePanelReleaseError = 604
!ReleasePanelSmemaError = 605

!ReloadPanelStateError = 701
!ReloadPanelFreeError = 702
!ReloadPanelSearchError = 703
!ReloadPanelSlowSensorError = 704

!SecurePanelToClampedError = 801
!SecurePanelToLiftedError = 802
!SecurePanelToUnstopError = 803

!PowerOnRecoveryWidthNotHomed = 901


global int WidthLifterConveyor_Reset_Completed
global int ReleaseCommandReceived
global int Lifter_Lowered

global int ConveyorDirection
global int ChangeWidthPanelPresent
global real PanelLength
global real DistanceBetweenSlowPositionAndStopSensor = 70
global real DistanceBetweenSlowPositionAndExitSensor = 50
global real DistanceBetweenSlowPositionAndEntrySensor = 50
global real DistanceBetweenEntryAndStopSensor = 620
global real DistanceBetweenStopSensorAndExitSensor = 620
global real DistanceBetweenEntrySensorAndExitSensor = 1150

global real Stage_1_LifterOnlyDistance
global real Stage_2_LifterAndClamperDistance
global real Stage_1_LifterSpeed
global real Stage_2_LifterSpeed
global real LifterDownSpeed


global int TowerLightRedFlashing_Bit
global int TowerLightYellowFlashing_Bit
global int TowerLightGreenFlashing_Bit
global int TowerLightBlueFlashing_Bit

!///////////////////////////////////////////////////////////
!Inputs

!global int Estop_Bit ! done I 0.0
!global int DoorSwitch_Bit ! done I 0.0
global int EstopAndDoorOpenFeedback_Bit ! done I 0.2
global int Reset_Button_Bit! done I 0.3
global int Start_Button_Bit! done I 0.4
global int Stop_Button_Bit! done I 0.5
global int AlarmCancelPushButton_Bit! done I 0.6
global int BypassNormal_Bit ! done I 0.7


!global int MainPressureSwitchFeedback_Bit ! done I 1.0
!global int TwelveVoltPowerSuppyAndFuse_Bit ! done I 1.1
!global int TwentyFourVoltPowerSuppyAndFuse_Bit ! done I 1.2
!global int BeltShroudManifoldPressureSwitchFeedback_Bit ! done I 1.3
global int UpstreamBoardAvailableSignal_Bit ! done I 1.4
global int UpstreamFailedBoardAvailableSignal_Bit ! done I 1.5
global int DownstreamMachineReadySignal_Bit ! done I 1.6
global int CustomerDISignal_Bit ! done I 1.7


global int EntryOpto_Bit ! done I 2.0
global int ExitOpto_Bit ! done I 2.1
global int LifterLowered_Bit ! done I 2.2
global int BoardStopPanelAlignSensor_Bit ! done I 2.3
global int StopperArmUp_Bit ! done I 2.4
global int StopperArmDown_Bit ! done I 2.5
global int RearClampUp_Bit ! done I 2.6
global int RearClampDown_Bit ! done I 2.7


global int FrontClampUp_Bit ! done I 3.0
global int FrontClampDown_Bit ! done I 3.1
global int Width_RL ! done I 3.2
global int Width_LL ! done I 3.3
global int StopperLocked_Bit ! done I 3.4
global int StopperUnlocked_Bit ! done I 3.5
!global int ConveyorPressureSwitchFeedback_Bit ! done I 3.6
!global int Spare ! done I 3.7

global int WidthHomeSwitch_Bit ! not used in PBA modification
global int WidthLimitSwitch_Bit ! not used in PBA modification



!///////////////////////////////////////////////////////////
!Outputs
global int ResetButtonLight_Bit ! done O 0.0
global int StartButtonLight_Bit ! done O 0.1
global int StopButtonLight_Bit ! done O 0.2
!global int Spare ! done O 0.3
global int TowerLightRed_Bit ! done 0 0.4
global int TowerLightYellow_Bit ! done 0 0.5 
global int TowerLightGreen_Bit ! done 0 0.6
global int TowerLightBlue_Bit ! done 0 0.7

global int TowerLightBuzzer_Bit ! done 0 1.0
global int SensorPowerOnOff_Bit !done O 1.1
global int StopSensor_Bit ! done 0 1.2
global int SmemaUpStreamMachineReady_Bit ! done 0 1.3
global int DownStreamBoardAvailable_Bit ! done 0 1.4
global int SmemaDownStreamFailedBoardAvailable_Bit ! done 0 1.5
!global int CustomerDOSignal_Bit ! done O 1.6
global int CustomerDOSignal_Bit ! done O 1.7

global int ClampPanel_Bit ! done 0 2.0
global int LockStopper_Bit ! done 0 2.1
global int RaiseBoardStopStopper_Bit ! done 0 2.2
global int BeltShroudVaccumON_Bit ! done 0 2.3
global int VacuumChuckEjector_Bit ! done O 2.4
!global int VacuumChuckGeneratorOnOff_Bit ! done O 2.5
!global int VacuumReleaseChuckOnOff_Bit ! done O 2.6
!global int HighVacuumGeneratorOnOff_Bit ! done O 2.7




!//////////////////////////////////////////////////////////


global int PanelFreed

global int PanelSecured

global int ConveyorWidthHomed
global int ConveyorLifterHomed

global int InternalMachineLoadBufferIndex
global int InternalErrorExitBufferIndex
global int FreePanelBufferIndex
global int SecurePanelBufferIndex
global int BypassModeBufferIndex
global int HomeConveyorBufferIndex
global int LoadPanelBufferIndex
global int ReleasePanelBufferIndex = 20
global int ReloadPanelBufferIndex = 21
global int ConveyorResetBufferIndex = 7
global int LifterHomingBufferIndex = 6
global int WidthHomingBufferIndex = 5

global real ConveyorBeltAcquireSpeed = 350
global real ConveyorBeltLoadingSpeed = 350
global real ConveyorBeltSlowSpeed = 50
global real ConveyorBeltReleaseSpeed = 350
global real ConveyorBeltUnloadingSpeed = 350
global int PingPongMode = 0; ! 0 is Off 1 is on

!BypassModeBuffer
global int BypassModeBuffer_WaitTimeToSearch = 10000
global int BypassModeBuffer_WaitTimeToAcq = 10000
global int BypassModeBuffer_WaitTimeToCutout = 10000
global int BypassModeBuffer_WaitTimeToExit = 10000
global int BypassModeBuffer_WaitTimeToRelease = 10000
global int BypassModeBuffer_WaitTimeToSmema = 10000

!ChangeWidthBuffer
global int ChangeWidthBuffer_WaitTimeToSearch = 10000

!FreePanelBuffer
global int FreePanelBuffer_UnclampLiftDelayTime = 1000
global int FreePanelBuffer_WaitTimeToUnlift = 1000
global int FreePanelBuffer_WaitTimeToUnclamp = 1000

!InternalMachineLoadBuffer
global int InternalMachineLoadBuffer_WaitTimeToSlow = 10000
global int InternalMachineLoadBuffer_WaitTimeToAlign = 1000
global int InternalMachineLoadBuffer_SlowDelayTime = 10000

!LoadPanelBuffer
global int LoadPanelBuffer_WaitTimeToAcq = 10000

!PowerOnRecoveryBuffer
global int PowerOnRecoveryBuffer_WaitTimeToSearch = 10000
global int PowerOnRecoveryBuffer_WaitTimeToExit = 10000
global int PowerOnRecoveryBuffer_WaitTimeToReset = 10000

!PreReleasePanelBuffer
global int PreReleasePanelBuffer_WaitTimeToExit = 10000

!ReleasePanelBuffer
global int ReleasePanelBuffer_WaitTimeToExit = 10000
global int ReleasePanelBuffer_WaitTimeToRelease = 10000
global int ReleasePanelBuffer_WaitTimeToSmema = 10000
global int ReleasePanelBuffer_WaitTimeToCutout = 10000
global int ReleasePanelBuffer_WaitTimeToBeltVacuum = 10000

!ReloadPanelBuffer
global int ReloadPanelBuffer_WaitTimeToSearch = 10000
global int ReloadPanelBuffer_ReloadDelayTime = 2000

!SecurePanelBuffer
global int SecurePanelBuffer_ClampLiftDelayTime = 3000
global int SecurePanelBuffer_WaitTimeToPanelClamped = 3000
global int SecurePanelBuffer_WaitTimeToLifted = 3000
global int SecurePanelBuffer_WaitTimeToUnstop = 10000
!//////////////////////////////////////////////////////////

global int Gantry_Mode


STOP
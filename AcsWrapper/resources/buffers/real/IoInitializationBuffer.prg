AUTOEXEC:


!ECUNMAPOUT(396)
!ECUNMAPOUT(397)
!ECUNMAPOUT(398)
!ECUNMAPOUT(399)
!
!ECUNMAPIN(396)
!ECUNMAPIN(397)
!ECUNMAPIN(398)
!ECUNMAPIN(399)

!Outputs

!ECOUT/b(3168,)!0 0.0
!ECOUT/b(3169,)!0 0.1
!ECOUT/b(3170,)!0 0.2
!ECOUT/b(3171,)!0 0.3
ECOUT/b(3172,TowerLightRed_Bit)!0 0.4
ECOUT/b(3173,TowerLightYellow_Bit)!0 0.5
ECOUT/b(3174,TowerLightGreen_Bit)!0 0.6
ECOUT/b(3175,TowerLightBlue_Bit)!0 0.7

ECOUT/b(3176,TowerLightBuzzer_Bit)!0 1.0
!ECOUT/b(3177,)!0 1.1
ECOUT/b(3178,StopSensor_Bit)!0 1.2
ECOUT/b(3179,SmemaUpStreamMachineReady_Bit)!0 1.3
ECOUT/b(3180,DownStreamBoardAvailable_Bit)!0 1.4
ECOUT/b(3181,SmemaDownStreamFailedBoardAvailable_Bit)!0 1.5
!ECOUT/b(3182,)!0 1.6
!ECOUT/b(3183,)!0 1.7

ECOUT/b(3184,ClampPanel_Bit)!0 2.0
ECOUT/b(3185,LockStopper_Bit)!0 2.1
ECOUT/b(3186,RaiseBoardStopStopper_Bit)!0 2.2
ECOUT/b(3187,BeltShroudVaccumON_Bit)!0 2.3
!ECOUT/b(3188,)!0 2.4
!ECOUT/b(3189,)!0 2.5
!ECOUT/b(3190,)!0 2.6
!ECOUT/b(3191,)!0 2.7

!ECOUT/b(3192,)!0 3.0
!ECOUT/b(3193,)!0 3.1
!ECOUT/b(3194,)!0 3.2
!ECOUT/b(3195,)!0 3.3
!ECOUT/b(3196,)!0 3.4
!ECOUT/b(3197,)!0 3.5
!ECOUT/b(3198,)!0 3.6
!ECOUT/b(3299,)!0 3.7




!Inputs

!ECIN/b(3168,)!I 0.0
!ECIN/b(3169,)!I 0.1
ECIN/b(3170,EstopAndDoorOpenFeedback_Bit)!I 0.2
!ECIN/b(3172,)!I 0.4
!ECIN/b(3173,)!I 0.5
!ECIN/b(3174,)!I 0.6

ECIN/b(3171,Reset_Button_Bit)!I 0.3
ECIN/b(3175,BypassNormal_Bit)!I 0.7

!ECIN/b(3176,)!I 1.0
!ECIN/b(3177,)!I 1.1
!ECIN/b(3178,)!I 1.2
!ECIN/b(3179,)!I 1.3
ECIN/b(3180,UpstreamBoardAvailableSignal_Bit)!I 1.4
ECIN/b(3181,UpstreamFailedBoardAvailableSignal_Bit)!I 1.5
ECIN/b(3182,DownstreamMachineReadySignal_Bit)!I 1.6
!ECIN/b(3183,)!I 1.7

ECIN/b(3184,EntryOpto_Bit)!I 2.0
ECIN/b(3185,ExitOpto_Bit)!I 2.1
ECIN/b(3186,LifterLowered_Bit)!I 2.2
ECIN/b(3187,BoardStopPanelAlignSensor_Bit)!I 2.3
ECIN/b(3188,StopperArmUp_Bit)!I 2.4
ECIN/b(3189,StopperArmDown_Bit)!I 2.5
ECIN/b(3190,RearClampUp_Bit)!I 2.6
ECIN/b(3191,RearClampDown_Bit)!I 2.7

ECIN/b(3192,FrontClampUp_Bit)!I 3.0
ECIN/b(3193,FrontClampDown_Bit)!I 3.1
ECIN/b(3194,Width_LL)!I 3.2
ECIN/b(3195,Width_RL)!I 3.3
ECIN/b(3196,StopperLocked_Bit)!I 3.4
ECIN/b(3197,StopperUnlocked_Bit)!I 3.5
!ECIN/b(3198,)!I 3.6
!ECIN/b(3299,)!I 3.7

stop


! Width left limit
ON Width_LL = 0
SAFINI(6).#LL=0
RET
ON Width_LL = 1
SAFINI(6).#LL= 1
RET

! Width right limit
ON Width_RL = 0
SAFINI(6).#RL=0
RET
ON Width_RL = 1
SAFINI(6).#RL= 1
RET

! Lifter left limit
ON LifterLowered_Bit = 0
SAFINI(7).#LL=0
RET
ON LifterLowered_Bit = 1
SAFINI(7).#LL= 1
RET
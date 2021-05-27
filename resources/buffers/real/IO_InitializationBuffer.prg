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

ECOUT/b(3168,ResetButtonLight_Bit)!0 0.0
ECOUT/b(3169,StartButtonLight_Bit)!0 0.1
ECOUT/b(3170,StopButtonLight_Bit)!0 0.2
!ECOUT/b(3171,Spare)!0 0.3
ECOUT/b(3172,TowerLightRed_Bit)!0 0.4
ECOUT/b(3173,TowerLightYellow_Bit)!0 0.5
ECOUT/b(3174,TowerLightGreen_Bit)!0 0.6
ECOUT/b(3175,TowerLightBlue_Bit)!0 0.7

ECOUT/b(3176,TowerLightBuzzer_Bit)!0 1.0
ECOUT/b(3177,SensorPowerOnOff_Bit)!0 1.1
ECOUT/b(3178,StopSensor_Bit)!0 1.2
ECOUT/b(3179,SmemaUpStreamMachineReady_Bit)!0 1.3
ECOUT/b(3180,DownStreamBoardAvailable_Bit)!0 1.4
ECOUT/b(3181,SmemaDownStreamFailedBoardAvailable_Bit)!0 1.5
!ECOUT/b(3182,CustomerDOSignal_Bit)!0 1.6
!ECOUT/b(3183,CustomerDOSignal_Bit)!0 1.7

ECOUT/b(3184,ClampPanel_Bit)!0 2.0
ECOUT/b(3185,LockStopper_Bit)!0 2.1
ECOUT/b(3186,RaiseBoardStopStopper_Bit)!0 2.2
ECOUT/b(3187,BeltShroudVaccumON_Bit)!0 2.3
!ECOUT/b(3188,VacuumChuckEjector_Bit)!0 2.4
!ECOUT/b(3189,VacuumChuckGeneratorOnOff_Bit)!0 2.5
!ECOUT/b(3190,VacuumReleaseChuckOnOff_Bit)!0 2.6
!ECOUT/b(3191,HighVacuumGeneratorOnOff_Bit)!0 2.7

!ECOUT/b(3192,Spare)!0 3.0
!ECOUT/b(3193,Spare)!0 3.1
!ECOUT/b(3194,Spare)!0 3.2
!ECOUT/b(3195,Spare)!0 3.3
!ECOUT/b(3196,Spare)!0 3.4
!ECOUT/b(3197,Spare)!0 3.5
!ECOUT/b(3198,Spare)!0 3.6
!ECOUT/b(3299,Spare)!0 3.7




!Inputs

!ECIN/b(3168,Estop_Bit)!I 0.0
!ECIN/b(3169,DoorSwitch_Bit)!I 0.1
ECIN/b(3170,EstopAndDoorOpenFeedback_Bit)!I 0.2
ECIN/b(3171,Reset_Button_Bit)!I 0.3
ECIN/b(3172,Start_Button_Bit)!I 0.4
ECIN/b(3173,Stop_Button_Bit)!I 0.5
ECIN/b(3174,AlarmCancelPushButton_Bit)!I 0.6
ECIN/b(3175,BypassNormal_Bit)!I 0.7


!ECIN/b(3176,MainPressureSwitchFeedback_Bit)!I 1.0
!ECIN/b(3177,TwelveVoltPowerSuppyAndFuse_Bit)!I 1.1
!ECIN/b(3178,TwentyFourVoltPowerSuppyAndFuse_Bit)!I 1.2
!ECIN/b(3179,BeltShroudManifoldPressureSwitchFeedback_Bit)!I 1.3
ECIN/b(3180,UpstreamBoardAvailableSignal_Bit)!I 1.4
ECIN/b(3181,UpstreamFailedBoardAvailableSignal_Bit)!I 1.5
ECIN/b(3182,DownstreamMachineReadySignal_Bit)!I 1.6
!ECIN/b(3183,CustomerDISignal_Bit)!I 1.7


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
!ECIN/b(3198,ConveyorPressureSwitchFeedback_Bit)!I 3.6
!ECIN/b(3299,Spare)!I 3.7

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

ON TowerLightRedFlashing_Bit = 1 | TowerLightYellowFlashing_Bit = 1 | TowerLightGreenFlashing_Bit = 1 |TowerLightBlueFlashing_Bit = 1

	While TowerLightRedFlashing_Bit = 1 | TowerLightYellowFlashing_Bit = 1|  TowerLightGreenFlashing_Bit = 1 | TowerLightBlueFlashing_Bit = 1

		if(TowerLightRedFlashing_Bit = 1)
			TowerLightRed_Bit = 1
		end
		if(TowerLightYellowFlashing_Bit = 1)
			TowerLightYellow_Bit = 1
		end
		if(TowerLightGreenFlashing_Bit = 1)
			TowerLightGreen_Bit = 1
		end
		if(TowerLightBlueFlashing_Bit = 1)
			TowerLightBlue_Bit = 1
		end

		wait 500

		if(TowerLightRedFlashing_Bit = 1)
			TowerLightRed_Bit = 0
		end
		if(TowerLightYellowFlashing_Bit = 1)
			TowerLightYellow_Bit = 0
		end
		if(TowerLightGreenFlashing_Bit = 1)
			TowerLightGreen_Bit = 0
		end
		if(TowerLightBlueFlashing_Bit = 1)
			TowerLightBlue_Bit = 0
		end

		wait 500

	End

RET

ON TowerLightRedFlashing_Bit = 0
TowerLightRed_Bit = 0
RET

ON TowerLightYellowFlashing_Bit = 0
TowerLightYellow_Bit = 0
RET

ON TowerLightGreenFlashing_Bit = 0
TowerLightGreen_Bit = 0
RET

ON TowerLightBlueFlashing_Bit = 0
TowerLightBlue_Bit = 0
RET
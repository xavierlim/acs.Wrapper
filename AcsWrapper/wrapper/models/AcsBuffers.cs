// Decompiled with JetBrains decompiler
// Type: AcsWrapperImpl.AcsBuffers
// Assembly: AcsWrapper, Version=1.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: DC1EDF75-AE0E-403A-BB79-8497514E3B04
// Assembly location: D:\git\tfs\NextGen.UI\SQDev.complete\CO.Phoenix\Source\CO.Systems\CO.Systems\TestApps\Acs\AcsPlatform\lib\AcsWrapper.dll

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.models
{
    public enum AcsBuffers
    {
        GantryHomeX = 0,
        GantryHomeY = 1,
        GantryHomeZ = 3,
        ConveyorHoming = 4,
        WidthHoming = 5,
        LifterHoming = 6,
        ConveyorReset = 7,
        Scanning = 9,
        BypassMode = 12, // 0x0000000C
        ChangeWidth = 13, // 0x0000000D
        EmergencyStop = 14, // 0x0000000E
        FreePanel = 15, // 0x0000000F
        InternalMachineLoad = 16, // 0x00000010
        LoadPanel = 17, // 0x00000011
        PowerOnRecoverFromEmergencyStop = 18, // 0x00000012
        PreReleasePanel = 19, // 0x00000013
        ReleasePanel = 20, // 0x00000014
        ReloadPanel = 21, // 0x00000015
        SecurePanel = 22, // 0x00000016
        InternalErrorExit = 23, // 0x00000017
        HomeX = 55, // 0x00000037
        HomeY = 56, // 0x00000038
        HomeZ = 57, // 0x00000039
        initIO = 63, // 0x0000003F
    }
}
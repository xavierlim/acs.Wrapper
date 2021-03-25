//===================================================================================
// Copyright (c) CyberOptics Corporation. All rights reserved. The
// copyright to the computer program herein is the property of CyberOptics.
// The program may be used or copied or both only with the written permission
// of CyberOptics or in accordance with the terms and conditions stipulated
// in the agreement or contract under which the program has been supplied.
// This copyright notice must not be removed.
//===================================================================================

using System;
using Configurator.Util;

namespace CO.Systems.Services.Acs.AcsWrapper.config
{
    public static class AcsSimHelper {

        private static Helper<AcsSimConfig> helper;

        private static Helper<AcsSimConfig> Helper => helper?? (helper = new Helper<AcsSimConfig>("AcsPlatformSimulator"));

        public static AcsSimConfig Config { get; private set; }

        public static bool IsEnable() {
            return Helper.IsEnabled;
        }

        public static void Start()
        {
            InitConfig();
        }

        private static void InitConfig() {
            if (!Helper.IsEnabled) return;
            try {
                Config = Helper.Load();
            }
            catch (Exception e) {
                // ignored exception, attempt to initialize the config file instead
                Config = new AcsSimConfig();
                Helper.Save(Config);
            }
        }

        public static void SaveConfig() {
            if (!Helper.IsEnabled) return;
            Helper.Save(Config);
        }
    }
}
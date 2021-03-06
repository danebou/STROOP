﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM64_Diagnostic.Structs.Configurations
{
    public struct CameraHackConfig
    {
        public uint CameraHackStruct { get { return Config.SwitchRomVersion(CameraHackStructUS, CameraHackStructJP); } }
        public uint CameraHackStructUS;
        public uint CameraHackStructJP;

        public uint CameraModeOffset;
        public uint CameraXOffset;
        public uint CameraYOffset;
        public uint CameraZOffset;
        public uint FocusXOffset;
        public uint FocusYOffset;
        public uint FocusZOffset;
        public uint AbsoluteAngleOffset;
        public uint ThetaOffset;
        public uint RadiusOffset;
        public uint RelativeHeightOffset;
        public uint ObjectOffset;

    }
}
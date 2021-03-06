﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM64_Diagnostic.Structs.Configurations
{
    public class AreaConfig
    {
        public uint AreaStartAddress { get { return Config.SwitchRomVersion(AreaStartAddressUS, AreaStartAddressJP); } }
        public readonly uint AreaStartAddressUS = 0x8033B8D0;
        public readonly uint AreaStartAddressJP = 0x8033A560;

        public readonly uint AreaStructSize = 0x3C;

        public uint CurrentAreaPointerAddress { get { return Config.SwitchRomVersion(CurrentAreaPointerAddressUS, CurrentAreaPointerAddressJP); } }
        public readonly uint CurrentAreaPointerAddressUS = 0x8032DDCC;
        public readonly uint CurrentAreaPointerAddressJP = 0x8032CE6C;

        public readonly uint TerrainTypeOffset = 0x02;

        public int? GetAreaIndex(uint areaAddress)
        {
            for (int i = 0; i < 8; i++)
            {
                uint address = (uint)(AreaStartAddress + i * AreaStructSize);
                if (address == areaAddress) return i;
            }
            return null;
        }

        public uint GetAreaAddress(int index)
        {
            return (uint)(AreaStartAddress + index * AreaStructSize);
        }
    }
}

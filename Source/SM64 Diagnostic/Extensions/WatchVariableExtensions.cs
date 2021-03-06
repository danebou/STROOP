﻿using SM64_Diagnostic.Structs;
using SM64_Diagnostic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SM64_Diagnostic.ManagerClasses;
using SM64_Diagnostic.Controls;
using SM64_Diagnostic.Managers;
using SM64_Diagnostic.Structs.Configurations;

namespace SM64_Diagnostic.Extensions
{
    public static class WatchVariableExtensions
    {

        public static uint GetRamAddress(this WatchVariable watchVar, uint offset = 0, bool addressArea = true)
        {
            var offsetedAddress = new UIntPtr(offset + watchVar.Address);
            uint address;

            if (watchVar.UseAbsoluteAddressing)
                address = (uint) Config.Stream.ConvertAddressEndianess(
                    new UIntPtr(offsetedAddress.ToUInt64() - (ulong) Config.Stream.ProcessMemoryOffset.ToInt64()),
                    watchVar.ByteCount);
            else
                address = offsetedAddress.ToUInt32();

            return addressArea ? address | 0x80000000 : address & 0x0FFFFFFF;
        }

        public static UIntPtr GetProcessAddress(this WatchVariable watchVar, uint offset = 0)
        {
            uint address = GetRamAddress(watchVar, offset, false);
            return Config.Stream.ConvertAddressEndianess(
                new UIntPtr(address + (ulong) Config.Stream.ProcessMemoryOffset.ToInt64()), watchVar.ByteCount);
        }

        public static byte[] GetByteData(this WatchVariable watchVar, uint offset)
        {
            // Get dataBytes
            var dataBytes = Config.Stream.ReadRamLittleEndian(watchVar.HasAdditiveOffset ? new UIntPtr(offset + watchVar.Address)
                : new UIntPtr(watchVar.Address), watchVar.ByteCount, watchVar.UseAbsoluteAddressing);

            // Make sure offset is a valid pointer
            if (watchVar.HasAdditiveOffset && offset == 0)
                return null;

            return dataBytes;
        }

        public static string GetStringValue(this WatchVariable watchVar, uint offset)
        {
            // Get dataBytes
            var dataBytes = watchVar.GetByteData(offset);

            // Make sure offset is a valid pointer
            if (dataBytes == null)
                return "(none)";

            // Parse object type
            if (watchVar.IsObject)
            {
                var objAddress = BitConverter.ToUInt32(dataBytes, 0);
                if (objAddress == 0)
                    return "(none)";

                var slotName = ManagerContext.Current.ObjectSlotManager.GetSlotNameFromAddress(objAddress);
                if (slotName != null)
                    return "Slot: " + slotName;
            }

            // Parse floating point
            if (!watchVar.UseHex && (watchVar.Type == typeof(float) || watchVar.Type == typeof(double)))
            {
                if (watchVar.Type == typeof(float))
                    return BitConverter.ToSingle(dataBytes, 0).ToString();

                if (watchVar.Type == typeof(double))
                    return BitConverter.ToDouble(dataBytes, 0).ToString();
            }

            // Get Uint64 value
            var intBytes = new byte[8];
            dataBytes.CopyTo(intBytes, 0);
            UInt64 dataValue = BitConverter.ToUInt64(intBytes, 0);

            // Apply mask
            if (watchVar.Mask.HasValue)
                dataValue &= watchVar.Mask.Value;

            // Boolean parsing
            if (watchVar.IsBool)
                return (dataValue != 0x00).ToString();

            // Print hex
            if (watchVar.UseHex)
                return "0x" + dataValue.ToString("X" + watchVar.ByteCount * 2);

            // Print signed
            if (watchVar.Type == typeof(Int64))
                return ((Int64)dataValue).ToString();
            else if (watchVar.Type == typeof(Int32))
                return ((Int32)dataValue).ToString();
            else if (watchVar.Type == typeof(Int16))
                return ((Int16)dataValue).ToString();
            else if (watchVar.Type == typeof(sbyte))
                return ((sbyte)dataValue).ToString();
            else
                return dataValue.ToString();
        }
  
        public static byte[] GetBytesFromAngleString(this WatchVariable watchVar, string value, AngleViewModeType viewMode)
        {
            if (watchVar.Type != typeof(UInt32) && watchVar.Type != typeof(UInt16)
                && watchVar.Type != typeof(Int32) && watchVar.Type != typeof(Int16))
                return null;

            UInt32 writeValue = 0;

            // Print hex
            if (ParsingUtilities.IsHex(value))
            {
                ParsingUtilities.TryParseHex(value, out writeValue);
            }
            else
            {
                switch (viewMode)
                {
                    case AngleViewModeType.Signed:
                    case AngleViewModeType.Unsigned:
                    case AngleViewModeType.Recommended:
                        int tempValue;
                        if (int.TryParse(value, out tempValue))
                            writeValue = (uint)tempValue;
                        else if (!uint.TryParse(value, out writeValue))
                            return null;
                        break;
                        

                    case AngleViewModeType.Degrees:
                        double degValue;
                        if (!double.TryParse(value, out degValue))
                            return null;
                        writeValue = (UInt16)(degValue / (360d / 65536));
                        break;

                    case AngleViewModeType.Radians:
                        double radValue;
                        if (!double.TryParse(value, out radValue))
                            return null;
                        writeValue = (UInt16)(radValue / (2 * Math.PI / 65536));
                        break;
                }
            }

            return BitConverter.GetBytes(writeValue).Take(watchVar.ByteCount).ToArray();
        }

        public static bool SetAngleStringValue(this WatchVariable watchVar, uint offset, string value, AngleViewModeType viewMode)
        {
            var dataBytes = watchVar.GetBytesFromAngleString(value, viewMode);
            return watchVar.SetBytes(offset, dataBytes);
        }

        public static string GetAngleStringValue(this WatchVariable watchVar, uint offset, AngleViewModeType viewMode, bool truncated = false)
        {
            // Get dataBytes
            var dataBytes = watchVar.GetByteData(offset);

            // Make sure offset is a valid pointer
            if (dataBytes == null)
                return "(none)";

            // Make sure dataType is a valid angle type
            if (watchVar.Type != typeof(UInt32) && watchVar.Type != typeof(UInt16)
                && watchVar.Type != typeof(Int32) && watchVar.Type != typeof(Int16))
                return "Error: datatype";

            // Get Uint32 value
            UInt32 dataValue = (watchVar.Type == typeof(UInt32)) ? BitConverter.ToUInt32(dataBytes, 0) 
                : BitConverter.ToUInt16(dataBytes, 0);

            // Apply mask
            if (watchVar.Mask.HasValue)
                dataValue = (UInt32)(dataValue & watchVar.Mask.Value);

            // Truncate by 16
            if (truncated)
                dataValue &= ~0x000FU;

            // Print hex
            if (watchVar.UseHex)
            {
                if (viewMode == AngleViewModeType.Recommended && watchVar.ByteCount == 4)
                    return "0x" + dataValue.ToString("X8"); 
                else
                    return "0x" + ((UInt16)dataValue).ToString("X4");
            }

            switch(viewMode)
            {
                case AngleViewModeType.Recommended:
                    if (watchVar.Type == typeof(Int16))
                        return ((Int16)dataValue).ToString();
                    else if (watchVar.Type == typeof(UInt16))
                        return ((UInt16)dataValue).ToString();
                    else if (watchVar.Type == typeof(Int32))
                        return ((Int32)dataValue).ToString();
                    else
                        return dataValue.ToString();

                case AngleViewModeType.Unsigned:
                    return ((UInt16)dataValue).ToString();

                case AngleViewModeType.Signed:
                    return ((Int16)(dataValue)).ToString();

                case AngleViewModeType.Degrees:
                    return (((UInt16)dataValue) * (360d / 65536)).ToString();

                case AngleViewModeType.Radians:
                    return (((UInt16)dataValue) * (2 * Math.PI / 65536)).ToString();
            }

            return "Error: ang. parse";
        }

        public static bool GetBoolValue(this WatchVariable watchVar, uint offset)
        {
            // Get dataBytes
            var dataBytes = watchVar.GetByteData(offset);

            // Make sure offset is a valid pointer
            if (dataBytes == null)
                return false;

            // Get Uint64 value
            var intBytes = new byte[8];
            dataBytes.CopyTo(intBytes, 0);
            UInt64 dataValue = BitConverter.ToUInt64(intBytes, 0);

            // Apply mask
            if (watchVar.Mask.HasValue)
                dataValue &= watchVar.Mask.Value;

            // Boolean parsing
            bool value = (dataValue != 0x00);
            value = watchVar.InvertBool ? !value : value;
            return value;
        }


        public static void SetBoolValue(this WatchVariable watchVar, uint offset, bool value)
        {
            // Get dataBytes
            var address = watchVar.HasAdditiveOffset ? offset + watchVar.Address : watchVar.Address;
            var dataBytes = watchVar.GetByteData(offset);

            // Make sure offset is a valid pointer
            if (dataBytes == null)
                return;

            if (watchVar.InvertBool)
                value = !value;

            // Get Uint64 value
            var intBytes = new byte[8];
            dataBytes.CopyTo(intBytes, 0);
            UInt64 dataValue = BitConverter.ToUInt64(intBytes, 0);

            // Apply mask
            if (watchVar.Mask.HasValue)
            {
                if (value)
                    dataValue |= watchVar.Mask.Value;
                else
                    dataValue &= ~watchVar.Mask.Value;
            }
            else
            {
                dataValue = value ? 1U : 0U;
            }

            var writeBytes = new byte[watchVar.ByteCount];
            var valueBytes = BitConverter.GetBytes(dataValue);
            Array.Copy(valueBytes, 0, writeBytes, 0, watchVar.ByteCount);

            Config.Stream.WriteRamLittleEndian(writeBytes, address, watchVar.UseAbsoluteAddressing);
        }

        public static byte[] GetBytesFromString(this WatchVariable watchVar, uint offset, string value)
        {
            // Get dataBytes
            var address = watchVar.HasAdditiveOffset ? offset + watchVar.Address : watchVar.Address;
            var dataBytes = new byte[8];
            Config.Stream.ReadRamLittleEndian(new UIntPtr(address), watchVar.ByteCount, watchVar.UseAbsoluteAddressing).CopyTo(dataBytes, 0);
            UInt64 oldValue = BitConverter.ToUInt64(dataBytes, 0);
            UInt64 newValue;


            // Handle object values
            uint? objectAddress;
            if (watchVar.IsObject && (objectAddress = ManagerContext.Current.ObjectSlotManager.GetSlotAddressFromName(value)).HasValue)
            {
                newValue = objectAddress.Value;
            }
            else
            // Handle hex variable
            if (ParsingUtilities.IsHex(value))
            {
                if (!ParsingUtilities.TryParseExtHex(value, out newValue))
                    return null;
            }
            // Handle floats
            else if (watchVar.Type == typeof(float))
            {
                float newFloatValue;
                if (!float.TryParse(value, out newFloatValue))
                    return null;

                // Get bytes
                newValue = BitConverter.ToUInt32(BitConverter.GetBytes(newFloatValue), 0);
            }
            else if (watchVar.Type == typeof(double))
            {
                double newFloatValue;
                if (double.TryParse(value, out newFloatValue))
                    return null;

                // Get bytes
                newValue = BitConverter.ToUInt64(BitConverter.GetBytes(newFloatValue), 0);
            }
            else if (watchVar.Type == typeof(UInt64))
            {
                if (!UInt64.TryParse(value, out newValue))
                {
                    Int64 newValueInt;
                    if (!Int64.TryParse(value, out newValueInt))
                        return null;

                    newValue = (UInt64)newValueInt;
                }
            }
            else
            {
                Int64 tempInt;
                if (!Int64.TryParse(value, out tempInt))
                    return null;
                newValue = (UInt64)tempInt;
            }

            // Apply mask
            if (watchVar.Mask.HasValue)
                newValue = (newValue & watchVar.Mask.Value) | ((~watchVar.Mask.Value) & oldValue);

            var writeBytes = new byte[watchVar.ByteCount];
            var valueBytes = BitConverter.GetBytes(newValue);
            Array.Copy(valueBytes, 0, writeBytes, 0, watchVar.ByteCount);

            return writeBytes;
        }

        public static bool SetStringValue(this WatchVariable watchVar, uint offset, string value)
        {
            var dataBytes = watchVar.GetBytesFromString(offset, value);
            return watchVar.SetBytes(offset, dataBytes);
        }

        public static bool SetBytes(this WatchVariable watchVar, uint offset, byte[] dataBytes)
        {
            if (dataBytes == null)
                return false;

            return Config.Stream.WriteRamLittleEndian(dataBytes, watchVar.HasAdditiveOffset ? offset + watchVar.Address
                : watchVar.Address, watchVar.UseAbsoluteAddressing);
        }
    }
}

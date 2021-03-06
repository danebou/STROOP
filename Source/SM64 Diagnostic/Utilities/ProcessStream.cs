﻿using SM64_Diagnostic.Structs;
using SM64_Diagnostic.Structs.Configurations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SM64_Diagnostic.Utilities
{
    public class ProcessStream
    {
        Emulator _emulator;
        IntPtr _processHandle;
        Process _process;
        IntPtr _ramStart = new IntPtr();
        Queue<double> _fpsTimes = new Queue<double>();
        BackgroundWorker _streamUpdater;
        byte[] _ram;
        bool _lastUpdateBeforePausing = false;
        object _enableLocker = new object();
        object _fpsQueueLocker = new object();

        public event EventHandler OnUpdate;
        public event EventHandler OnStatusChanged;
        public event EventHandler OnDisconnect;
        public event EventHandler FpsUpdated;
        public event EventHandler WarnReadonlyOff;
        public event EventHandler OnClose;

        public bool Readonly = false;
        public bool ShowWarning = false;

        public ConcurrentDictionary<WatchVariableLock, WatchVariableLock> LockedVariables = 
            new ConcurrentDictionary<WatchVariableLock, WatchVariableLock>();

        public IntPtr ProcessMemoryOffset
        {
            get
            {
                return _ramStart; 
            }
        }

        public byte[] Ram
        {
            get
            {
                return _ram;
            }
        }

        public string ProcessName
        {
            get
            {
                return _emulator == null ? "(No Emulator)" : _emulator.ProcessName;
            }
        }

        public double FpsInPractice
        {
            get
            {
                double fps;
                lock (_fpsQueueLocker)
                {
                    fps = _fpsTimes.Count == 0 ? 0 : 1000 / _fpsTimes.Average();
                }
                return fps;
            }
        }

        public Boolean IsSuspended = false;
        public Boolean IsClosed = true;
        public Boolean IsEnabled = false;
        public Boolean IsRunning
        {
            get
            {
                bool running;
                lock(_enableLocker)
                {
                    running = !(IsSuspended || IsClosed || !IsEnabled || !_streamUpdater.IsBusy);
                }
                return running;
            }
        }

        public ProcessStream(Process process = null, Emulator emulator = null)
        {
            _process = process;

            _ram = new byte[Config.RamSize];

            _streamUpdater = new BackgroundWorker();
            _streamUpdater.DoWork += ProcessUpdateTask;
            _streamUpdater.WorkerSupportsCancellation = true;
            _streamUpdater.RunWorkerAsync();

            SwitchProcess(_process, _emulator);
        }

        private void LogException(Exception e)
        {
            try
            {
                var log = String.Format("{0}\n{1}\n{2}\n", e.Message, e.TargetSite.ToString(), e.StackTrace);
                File.AppendAllText("error.txt", log);
            }
            catch (Exception) { }
        }

        private void ExceptionHandler(Task obj)
        {
            LogException(obj.Exception);
            throw obj.Exception;
        }

        ~ProcessStream()
        {
            if (_process != null)
                _process.Exited -= ProcessClosed;

            _streamUpdater.CancelAsync();
        }

        public bool SwitchProcess(Process newProcess, Emulator emulator)
        {
            lock (_enableLocker)
            {
                IsEnabled = false;
            }

            // Make sure old process is running
            if (IsSuspended)
                Resume();

            // Close old process
            Kernal32NativeMethods.CloseProcess(_processHandle);

            // Disconnect events
            if (_process != null)
                _process.Exited -= ProcessClosed;

            // Find DLL offset if needed
            IntPtr dllOffset = new IntPtr();
            bool dllSuccess = true;
            if (emulator != null && emulator.Dll != null)
            {
                var dll = newProcess.Modules.Cast<ProcessModule>()
                    .FirstOrDefault(d => d.ModuleName == emulator.Dll);
                if (dll == null)
                {
                    dllSuccess = false;
                }
                else
                {
                    dllOffset = dll.BaseAddress;
                }
            }

            // Make sure the new process has a value and that all DLLs where found
            if (newProcess == null || emulator == null || !dllSuccess)
            {
                _processHandle = new IntPtr();
                _process = null;
                _emulator = null;
                _ramStart = new IntPtr();
                OnStatusChanged?.Invoke(this, new EventArgs());
                return false;
            }

            // Open and set new process
            _process = newProcess;
            _emulator = emulator;
            _ramStart = new IntPtr(_emulator.RamStart + dllOffset.ToInt64());
            _processHandle = Kernal32NativeMethods.ProcessGetHandleFromId(0x0838, false, _process.Id);

            if ((int)_processHandle == 0)
            {
                OnStatusChanged?.Invoke(this, new EventArgs());
                return false;
            }

            try
            {
                _process.EnableRaisingEvents = true;
            }
            catch (Exception)
            {
                return false;
            }
            _process.Exited += ProcessClosed;

            IsSuspended = false;
            IsClosed = false;
            lock (_enableLocker)
            {
                IsEnabled = true;
            }
            OnStatusChanged?.Invoke(this, new EventArgs());

            return true;
        }

        public bool ReadProcessMemory(int address, byte[] buffer, bool absoluteAddressing = false)
        {
            if (_process == null)
                return false;

            int numOfBytes = 0;
            return Kernal32NativeMethods.ProcessReadMemory(_processHandle, absoluteAddressing ? new IntPtr(address) : new IntPtr(address + ProcessMemoryOffset.ToInt64()),
                buffer, (IntPtr)buffer.Length, ref numOfBytes);
        }

        public bool WriteProcessMemory(UIntPtr address, byte[] buffer, bool absoluteAddressing = false)
        {
            int numOfBytes = 0;
            if (!absoluteAddressing)
                address = new UIntPtr(address.ToUInt32() & ~0x80000000U);
            return Kernal32NativeMethods.ProcessWriteMemory(_processHandle, absoluteAddressing ? new IntPtr((long) address.ToUInt64()) :
                new IntPtr((long) ConvertAddressEndianess(new UIntPtr(address.ToUInt32() + (ulong) ProcessMemoryOffset.ToInt64()), buffer.Length)),
                buffer, (IntPtr)buffer.Length, ref numOfBytes);
        }

        public void Suspend()
        {
            if (IsSuspended || _process == null)
                return;

            _lastUpdateBeforePausing = true;

            Kernal32NativeMethods.SuspendProcess(_process);

            IsSuspended = true;
            OnStatusChanged?.Invoke(this, new EventArgs());
        }

        public void Resume()
        {
            if (!IsSuspended || _process == null)
                return;

            // Resume all threads
            Kernal32NativeMethods.ResumeProcess(_process);

            IsSuspended = false;
            OnStatusChanged?.Invoke(this, new EventArgs());
        }

        private void ProcessClosed(object sender, EventArgs e)
        {
            IsClosed = true;
            lock (_enableLocker)
            {
                IsEnabled = false;
            }
            OnStatusChanged?.Invoke(this, new EventArgs());
            OnDisconnect?.Invoke(this, new EventArgs());
        }

        public object GetValue(Type type, uint address, bool absoluteAddress = false)
        {
            if (type == typeof(byte)) return GetByte(address, absoluteAddress);
            if (type == typeof(sbyte)) return GetSByte(address, absoluteAddress);
            if (type == typeof(short)) return GetInt16(address, absoluteAddress);
            if (type == typeof(ushort)) return GetUInt16(address, absoluteAddress);
            if (type == typeof(int)) return GetInt32(address, absoluteAddress);
            if (type == typeof(uint)) return GetUInt32(address, absoluteAddress);
            if (type == typeof(float)) return GetSingle(address, absoluteAddress);

            throw new ArgumentOutOfRangeException("Cannot call ProcessStream.GetValue with type " + type);
        }

        public byte GetByte(uint address, bool absoluteAddress = false)
        {
            return ReadRamLittleEndian(new UIntPtr(address), 1, absoluteAddress)[0];
        }

        public sbyte GetSByte(uint address, bool absoluteAddress = false)
        {
            return (sbyte)ReadRamLittleEndian(new UIntPtr(address), 1, absoluteAddress)[0];
        }

        public short GetInt16(uint address, bool absoluteAddress = false)
        { 
            return BitConverter.ToInt16(ReadRamLittleEndian(new UIntPtr(address), 2, absoluteAddress), 0);
        }

        public ushort GetUInt16(uint address, bool absoluteAddress = false)
        {
            return BitConverter.ToUInt16(ReadRamLittleEndian(new UIntPtr(address), 2, absoluteAddress), 0);
        }

        public int GetInt32(uint address, bool absoluteAddress = false)
        {
            return BitConverter.ToInt32(ReadRamLittleEndian(new UIntPtr(address), 4, absoluteAddress), 0);
        }

        public uint GetUInt32(uint address, bool absoluteAddress = false)
        {
            return BitConverter.ToUInt32(ReadRamLittleEndian(new UIntPtr(address), 4, absoluteAddress), 0);
        }

        public float GetSingle(uint address, bool absoluteAddress = false)
        {
            return BitConverter.ToSingle(ReadRamLittleEndian(new UIntPtr(address), 4, absoluteAddress), 0);
        }

        public byte[] ReadRamLittleEndian(UIntPtr address, int length, bool absoluteAddress = false)
        {
            byte[] readBytes = new byte[length];
            uint localAddress;

            if (absoluteAddress)
                localAddress = (uint) (address.ToUInt64() - (ulong)ProcessMemoryOffset.ToInt64());
            else
                localAddress = ConvertAddressEndianess(address.ToUInt32() & ~0x80000000, length);

            if (localAddress + length > _ram.Length)
                return new byte[length];

            Buffer.BlockCopy(_ram, (int)localAddress, readBytes, 0, length);
            return readBytes;
        }

        readonly byte[] _fixAddress = { 0x03, 0x02, 0x01, 0x00 };
        public byte[] ReadRam(uint address, int length)
        {
            byte[] readBytes = new byte[length];
            address &= ~0x80000000U;

            if (address + length > _ram.Length)
                return new byte[length];

            for (uint i = 0; i < length; i++, address++)
            {
                readBytes[i] = _ram[address & ~0x00000003 | _fixAddress[address & 0x3]];
            }

            return readBytes;
        }

        public bool CheckReadonlyOff()
        {
            if (ShowWarning)
                WarnReadonlyOff?.Invoke(this, new EventArgs());

            return Readonly;
        }

        public bool SetValue(Type type, object value, uint address, bool absoluteAddress = false)
        {
            if (value is string)
            {
                if (type == typeof(byte)) value = ParsingUtilities.ParseByteNullable((string)value);
                if (type == typeof(sbyte)) value = ParsingUtilities.ParseSByteNullable((string)value);
                if (type == typeof(short)) value = ParsingUtilities.ParseShortNullable((string)value);
                if (type == typeof(ushort)) value = ParsingUtilities.ParseUShortNullable((string)value);
                if (type == typeof(int)) value = ParsingUtilities.ParseIntNullable((string)value);
                if (type == typeof(uint)) value = ParsingUtilities.ParseUIntNullable((string)value);
                if (type == typeof(float)) value = ParsingUtilities.ParseFloatNullable((string)value);
            }

            if (value == null) return false;

            if (type == typeof(byte)) return SetValue((byte)value, address, absoluteAddress);
            if (type == typeof(sbyte)) return SetValue((sbyte)value, address, absoluteAddress);
            if (type == typeof(short)) return SetValue((short)value, address, absoluteAddress);
            if (type == typeof(ushort)) return SetValue((ushort)value, address, absoluteAddress);
            if (type == typeof(int)) return SetValue((int)value, address, absoluteAddress);
            if (type == typeof(uint)) return SetValue((uint)value, address, absoluteAddress);
            if (type == typeof(float)) return SetValue((float)value, address, absoluteAddress);

            throw new ArgumentOutOfRangeException("Cannot call ProcessStream.SetValue with type " + type);
        }

        public bool SetValue(byte value, uint address, bool absoluteAddress = false)
        {
            return WriteRamLittleEndian(new byte[] { value }, address, absoluteAddress);
        }

        public bool SetValue(sbyte value, uint address, bool absoluteAddress = false)
        {
            return WriteRamLittleEndian(new byte[] { (byte)value }, address, absoluteAddress);
        }

        public bool SetValue(Int16 value, uint address, bool absoluteAddress = false)
        {
            return WriteRamLittleEndian(BitConverter.GetBytes(value), address, absoluteAddress);
        }

        public bool SetValue(UInt16 value, uint address, bool absoluteAddress = false)
        {
            return WriteRamLittleEndian(BitConverter.GetBytes(value), address, absoluteAddress);
        }

        public bool SetValue(Int32 value, uint address, bool absoluteAddress = false)
        {
            return WriteRamLittleEndian(BitConverter.GetBytes(value), address, absoluteAddress);
        }

        public bool SetValue(UInt32 value, uint address, bool absoluteAddress = false)
        {
            return WriteRamLittleEndian(BitConverter.GetBytes(value), address, absoluteAddress);
        }

        public bool SetValue(float value, uint address, bool absoluteAddress = false)
        {
            return WriteRamLittleEndian(BitConverter.GetBytes(value), address, absoluteAddress);
        }

        public bool WriteRamLittleEndian(byte[] buffer, uint address, bool absoluteAddress = false, int bufferStart = 0, int? length = null, bool safeWrite = true)
        {
            if (length == null)
                length = buffer.Length - bufferStart;

            if (CheckReadonlyOff())
                return false;

            byte[] writeBytes = new byte[length.Value];
            Array.Copy(buffer, bufferStart, writeBytes, 0, length.Value);

            // Attempt to pause the game before writing 
            bool preSuspended = IsSuspended;
            if (safeWrite)
                Suspend();

            // Write memory to game/process
            bool result = WriteProcessMemory(new UIntPtr(address), writeBytes, absoluteAddress);

            // Resume stream 
            if (safeWrite && !preSuspended)
                Resume();

            return result;
        }

        public bool WriteRam(byte[] buffer, uint address, int bufferStart = 0, int? length = null, bool safeWrite = true)
        {
            address &= ~0x80000000U;

            if (length == null)
                length = buffer.Length - bufferStart;

            if (CheckReadonlyOff())
                return false;

            bool success = true;

            // Attempt to pause the game before writing 
            bool preSuspended = IsSuspended;
            if (safeWrite)
                Suspend();

            // Take care of first alignment
            int bufPos = bufferStart;
            uint alignment = _fixAddress[address & 0x03] + 1U;
            if (alignment < 4)
            {
                byte[] writeBytes = new byte[Math.Min(alignment, length.Value)];
                Array.Copy(buffer, bufPos, writeBytes, 0, writeBytes.Length);
                success &= WriteProcessMemory(new UIntPtr(address), writeBytes.Reverse().ToArray());
                length -= writeBytes.Length;
                bufPos += writeBytes.Length;
                address += alignment;
            }

            // Take care of middle
            if (length >= 4)
            {
                byte[] writeBytes = new byte[length.Value & ~0x03];
                for (int i = 0; i < writeBytes.Length; bufPos += 4, i += 4)
                {
                    writeBytes[i] = buffer[bufPos + 3];
                    writeBytes[i + 1] = buffer[bufPos + 2];
                    writeBytes[i + 2] = buffer[bufPos + 1];
                    writeBytes[i + 3] = buffer[bufPos];
                }
                success &= WriteProcessMemory(new UIntPtr(address + (ulong) ProcessMemoryOffset.ToInt64()), writeBytes, true);
                address += (uint)writeBytes.Length;
                length -= writeBytes.Length;
            }

            // Take care of last
            if (length > 0)
            {
                byte[] writeBytes = new byte[length.Value];
                Array.Copy(buffer, bufPos, writeBytes, 0, writeBytes.Length);
                success &= WriteProcessMemory(new UIntPtr(address), writeBytes.Reverse().ToArray());
            }

            // Resume stream 
            if (safeWrite && !preSuspended)
                Resume();

            return success;
        }

        public void Stop()
        {
            _streamUpdater.CancelAsync();
        }

        public bool RefreshRam()
        {
            try
            {
                // Read whole ram value to buffer
                return ReadProcessMemory(0, _ram);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ProcessUpdateTask(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            var prevTime = Stopwatch.StartNew();
            while (!e.Cancel)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                prevTime.Restart();
                if (!IsRunning & !_lastUpdateBeforePausing)
                    goto FrameLimitStreamUpdate;

                _lastUpdateBeforePausing = false;

                int timeToWait;

                if (!RefreshRam())
                    goto FrameLimitStreamUpdate;

                OnUpdate?.Invoke(this, new EventArgs());

                foreach (var lockVar in LockedVariables)
                    lockVar.Value.Update();

                FrameLimitStreamUpdate:

                // Calculate delay to match correct FPS
                prevTime.Stop();
                timeToWait = (int)Config.RefreshRateInterval - (int)prevTime.ElapsedMilliseconds;
                timeToWait = Math.Max(timeToWait, 0);

                // Calculate Fps
                lock (_fpsQueueLocker)
                {
                    if (_fpsTimes.Count() >= 10)
                        _fpsTimes.Dequeue();
                    _fpsTimes.Enqueue(prevTime.ElapsedMilliseconds + timeToWait);
                }
                FpsUpdated?.Invoke(this, new EventArgs());

                if (timeToWait > 0)
                    Thread.Sleep(timeToWait);
                else
                    Thread.Yield();
            }

            OnClose?.BeginInvoke(this, new EventArgs(), null, null);
        }

        public UIntPtr ConvertAddressEndianess(UIntPtr address, int dataSize)
        {
            switch (dataSize)
            {
                case 1:
                case 2:
                case 3:
                    return new UIntPtr((address.ToUInt64() & ~0x03UL) | (_fixAddress[dataSize - 1] - address.ToUInt64() & 0x03UL));
                default:
                    return address;
            }
        }

        public uint ConvertAddressEndianess(uint address, int dataSize)
        {
            switch (dataSize)
            {
                case 1:
                case 2:
                case 3:
                    return (uint)(address & ~0x03) | (_fixAddress[dataSize - 1] - address & 0x03);
                default:
                    return address;
            }
        }
    }
}

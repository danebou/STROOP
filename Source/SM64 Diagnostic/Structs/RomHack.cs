﻿using SM64_Diagnostic.Structs.Configurations;
using SM64_Diagnostic.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SM64_Diagnostic.Structs
{
    public class RomHack
    {
        public bool Enabled = false;
        public string Name;
        List<Tuple<uint, byte[]>> _payload = new List<Tuple<uint, byte[]>>();
        List<Tuple<uint, byte[]>> _originalMemory = new List<Tuple<uint, byte[]>>();

        public RomHack(string hackFileName, string hackName)
        {
            Name = hackName;
            LoadHackFromFile(hackFileName);
        }

        void LoadHackFromFile(string hackFileName)
        {
            // Load file and remove whitespace
            var dataUntrimmed = File.ReadAllText(hackFileName);
            var data = Regex.Replace(dataUntrimmed, @"\s+", "");

            int nextEnd;
            int prevEnd = data.IndexOf(":");

            // Failed to parse file
            if (prevEnd < 8 || prevEnd == data.Length - 1)
                return;

            string remData = data.Substring(prevEnd + 1);

            do
            {
                nextEnd = remData.IndexOf(":");

                uint address = ParsingUtilities.ParseHex(data.Substring(prevEnd - 8, 8));
                string byteData = (nextEnd == -1) ? remData : remData.Substring(0, nextEnd - 8);

                var hackBytes = new byte[byteData.Length / 2];
                for (int i = 0; i < hackBytes.Length; i++)
                {
                    hackBytes[i] = (byte)ParsingUtilities.ParseHex(byteData.Substring(i * 2, 2));
                }

                _payload.Add(new Tuple<uint, byte[]>(address, hackBytes));

                remData = remData.Substring(nextEnd + 1);
                prevEnd += nextEnd + 1;
            }
            while (nextEnd != -1);
        }

        public void LoadPayload(bool suspendStream = true)
        {
            var originalMemory = new List<Tuple<uint, byte[]>>();
            bool success = true;

            if (suspendStream)
                Config.Stream.Suspend();

            foreach (var address in _payload)
            {
                // Read original memory before replacing
                originalMemory.Add(new Tuple<uint, byte[]>(address.Item1, Config.Stream.ReadRam(address.Item1, address.Item2.Length)));
                success &= Config.Stream.WriteRam(address.Item2, address.Item1);
            }

            if (suspendStream)
                Config.Stream.Resume();

            // Update original memory upon success
            if (success)
            {
                _originalMemory.Clear();
                _originalMemory.AddRange(originalMemory);
            }

            Enabled = success;
        }

        public bool ClearPayload()
        {
            bool success = true;

            if (_originalMemory.Count != _payload.Count)
                return false;

            Config.Stream.Suspend();

            foreach (var address in _originalMemory)
                // Read original memory before replacing
                success &= Config.Stream.WriteRam(address.Item2, address.Item1);

            Config.Stream.Resume();

            Enabled = !success;

            return success;
        }

        public void UpdateEnabledStatus()
        {
            Enabled = true;
            foreach (var address in _payload)
                Enabled &= address.Item2.SequenceEqual(Config.Stream.ReadRam(address.Item1, address.Item2.Length));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

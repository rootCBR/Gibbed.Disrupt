﻿/* Copyright (c) 2014 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Gibbed.IO;
using CBR;

namespace Gibbed.Disrupt.FileFormats
{
    public class BinaryObjectFile
    {
        private const uint _Signature = 0x4643626E; // 'FCbn' FarCry Binary N???

        public ushort Version = 3;
        public HeaderFlags Flags = HeaderFlags.None;
        public BinaryObject Root;

        public void Serialize(Stream output)
        {
            if (this.Version != 3)
            {
                throw new FormatException("unsupported file version");
            }

            if (this.Flags != HeaderFlags.None)
            {
                throw new FormatException("unsupported file flags");
            }

            var endian = Endian.Little;
            using (var data = new MemoryStream())
            {
                uint totalObjectCount = 0, totalValueCount = 0;

                this.Root.Serialize(data, ref totalObjectCount,
                                    ref totalValueCount,
                                    endian);
                data.Flush();
                data.Position = 0;

                output.WriteValueU32(_Signature, endian);
                output.WriteValueU16(this.Version, endian);
                output.WriteValueEnum<HeaderFlags>(this.Flags, endian);
                output.WriteValueU32(totalObjectCount, endian);
                output.WriteValueU32(totalValueCount, endian);
                output.WriteFromStream(data, data.Length);
            }
        }

        public void Deserialize(Stream input)
        {
            /*
            var magic = input.ReadValueU32(Endian.Little);
            var endian = Endian.Little;

            Console.WriteLine("0");

            if (magic != _Signature)
            {
                magic = input.ReadValueU32(Endian.Big);

                if (magic == _Signature)
                {
                    Console.WriteLine("1A");
                    endian = Endian.Big;
                }
                else
                {
                    Console.WriteLine("1B");
                    //throw new FormatException("invalid header magic");
                }
            }

            var version = input.ReadValueU16(endian);

            if (version != 3)
            {
                Console.WriteLine("2");
                //throw new FormatException("unsupported file version");
            }

            var flags = input.ReadValueEnum<HeaderFlags>(endian);

            if (flags != HeaderFlags.None)
            {
                Console.WriteLine("3");
                //throw new FormatException("unsupported file flags");
            }

            Console.Write($"\nFile info:\n\tMagic = {magic:X8}\n\tVersion = {version}\n\tFlags = {flags}\n");
            */

            string prefix = "";

            var endian = Endian.Little;

            uint magic = 0;

            Utility.Log("Searching for header...");

            while (magic != _Signature)
            {
                magic = input.ReadValueU32(endian);

                if (magic != _Signature)
                {
                    prefix += $"{magic.Swap():X8}";
                }
            }

            Utility.Log("Header found!");

            var version = input.ReadValueU16(endian);
            var flags = input.ReadValueEnum<HeaderFlags>(endian);

            var totalObjectCount = input.ReadValueU32(endian);
            var totalValueCount = input.ReadValueU32(endian);

            var pointers = new List<BinaryObject>();

            this.Version = version;
            this.Flags = flags;
            this.Root = BinaryObject.Deserialize(null, input, pointers, endian);

            Console.Write($"Magic: {magic:X8}\nVersion: {version}\nFlags: {flags}\nPrefix: {prefix}\n\n");
        }

        [Flags]
        public enum HeaderFlags : ushort
        {
            None = 0,

            Debug = 1 << 0, // "Not Stripped"
        }
    }
}

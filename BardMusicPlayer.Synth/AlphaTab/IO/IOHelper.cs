﻿using BardMusicPlayer.Synth.AlphaTab.CSharp.Collections;

namespace BardMusicPlayer.Synth.AlphaTab.IO
{
    internal static class IOHelper
    {

        public static uint ReadUInt32LE(this IReadable input)
        {
            var ch1 = input.ReadByte();
            var ch2 = input.ReadByte();
            var ch3 = input.ReadByte();
            var ch4 = input.ReadByte();

            return Platform.Platform.ToUInt32((ch4 << 24) | (ch3 << 16) | (ch2 << 8) | ch1);
        }

        public static ushort ReadUInt16LE(this IReadable input)
        {
            var ch1 = input.ReadByte();
            var ch2 = input.ReadByte();

            return Platform.Platform.ToUInt16((ch2 << 8) | ch1);
        }

        public static short ReadInt16LE(this IReadable input)
        {
            var ch1 = input.ReadByte();
            var ch2 = input.ReadByte();

            return Platform.Platform.ToInt16((ch2 << 8) | ch1);
        }



        public static string Read8BitStringLength(this IReadable input, int length)
        {
            var s = new StringBuilder();
            var z = -1;
            for (var i = 0; i < length; i++)
            {
                var c = input.ReadByte();
                if (c == 0 && z == -1)
                {
                    z = i;
                }

                s.AppendChar(c);
            }

            var t = s.ToString();
            if (z >= 0)
            {
                return t.Substring(0, z);
            }

            return t;
        }


    }
}
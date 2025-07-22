using ClientMessenger.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClientMessenger.Network
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PacketHeader // 통신 용도
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Signature;

        public int Type;     // PacketType enum
        public int Length;   // Body 전체 길이
        public int JsonLength; // Json 길이        

        public PacketHeader(PacketType type, int length, int jsonLength)
        {
            Signature = Encoding.UTF8.GetBytes(Constants.Signature);
            Type = (int)type;
            Length = length;
            JsonLength = jsonLength;
        }

        public bool IsValidSignature()
        {
            return Encoding.UTF8.GetString(Signature) == Constants.Signature;
        }
    }
}

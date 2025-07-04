using System;
using System.Runtime.InteropServices;
using System.Text;
using ServerMessenger.Common;

namespace ServerMessenger.Network
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)] //필드들이 선언된 순서대로 메모리에 배치됨, 1바이트 단위로 정렬
    public struct PacketHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Signature;
        public int Type;
        public int Length;

        public PacketHeader(PacketType type, int length)
        {
            Signature = Encoding.UTF8.GetBytes(Constants.Signature);
            Type = (int)type;
            Length = length;
        }

        public bool IsValidSignature()
        {
            return Encoding.UTF8.GetString(Signature) == Constants.Signature;
        }
    }
}

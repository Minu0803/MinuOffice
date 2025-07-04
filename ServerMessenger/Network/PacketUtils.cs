using System;
using System.Runtime.InteropServices;

namespace ServerMessenger.Network
{
    public static class PacketUtils
    {
        public static byte[] SerializeHeader(PacketHeader header)
        {
            int size = Marshal.SizeOf(header); // 구조체 크기 계산
            byte[] buffer = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(header, ptr, true);
                Marshal.Copy(ptr, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return buffer;
        }

        public static PacketHeader DeserializeHeader(byte[] buffer)
        {
            PacketHeader header = new PacketHeader();
            int size = Marshal.SizeOf(header);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(buffer, 0, ptr, size);
                header = (PacketHeader)Marshal.PtrToStructure(ptr, typeof(PacketHeader));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return header;
        }
    }
}

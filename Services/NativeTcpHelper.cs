using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;

namespace LocalhostManager.Services
{
    public static class NativeTcpHelper
    {
        private const int AF_INET = 2;   // IPv4
        private const int AF_INET6 = 23; // IPv6

        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private const int ERROR_SUCCESS = 0;

        private const uint MIB_TCP_STATE_LISTEN = 2;

        public enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_PID
        {
            public uint dwState;
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwRemoteAddr;
            public uint dwRemotePort;
            public uint dwOwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCP6ROW_OWNER_PID
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ucLocalAddr;
            public uint dwLocalScopeId;
            public uint dwLocalPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ucRemoteAddr;
            public uint dwRemoteScopeId;
            public uint dwState;
            public uint dwOwningPid;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int pdwSize,
            bool bOrder,
            int ulAf,
            TCP_TABLE_CLASS TableClass,
            uint reserved = 0);

        public record RawSocketInfo(int Port, string LocalAddress, int Pid);

        public static List<RawSocketInfo> GetListeningSockets()
        {
            var results = new List<RawSocketInfo>();
            GetListeningIpv4(results);
            GetListeningIpv6(results);
            return results;
        }

        private static void GetListeningIpv4(List<RawSocketInfo> results)
        {
            int bufferSize = 0;
            // First call to determine required buffer size
            uint ret = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

            IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);
            try
            {
                ret = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != ERROR_SUCCESS) return;

                int rowCount = Marshal.ReadInt32(tcpTablePtr);
                IntPtr rowPtr = IntPtr.Add(tcpTablePtr, 4);

                int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

                for (int i = 0; i < rowCount; i++)
                {
                    var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                    rowPtr = IntPtr.Add(rowPtr, rowSize);

                    if (row.dwState == MIB_TCP_STATE_LISTEN)
                    {
                        ushort port = (ushort)(((row.dwLocalPort & 0xFF) << 8) | ((row.dwLocalPort >> 8) & 0xFF));
                        var ip = new IPAddress(row.dwLocalAddr).ToString();
                        results.Add(new RawSocketInfo(port, ip, (int)row.dwOwningPid));
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTablePtr);
            }
        }

        private static void GetListeningIpv6(List<RawSocketInfo> results)
        {
            int bufferSize = 0;
            uint ret = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, false, AF_INET6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

            IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);
            try
            {
                ret = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, false, AF_INET6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != ERROR_SUCCESS) return;

                int rowCount = Marshal.ReadInt32(tcpTablePtr);
                IntPtr rowPtr = IntPtr.Add(tcpTablePtr, 4);

                int rowSize = Marshal.SizeOf<MIB_TCP6ROW_OWNER_PID>();

                for (int i = 0; i < rowCount; i++)
                {
                    var row = Marshal.PtrToStructure<MIB_TCP6ROW_OWNER_PID>(rowPtr);
                    rowPtr = IntPtr.Add(rowPtr, rowSize);

                    if (row.dwState == MIB_TCP_STATE_LISTEN)
                    {
                        ushort port = (ushort)(((row.dwLocalPort & 0xFF) << 8) | ((row.dwLocalPort >> 8) & 0xFF));
                        var ip = new IPAddress(row.ucLocalAddr).ToString();
                        results.Add(new RawSocketInfo(port, ip, (int)row.dwOwningPid));
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTablePtr);
            }
        }
    }
}

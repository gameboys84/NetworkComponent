using System;
using System.Net;
using System.Text;

namespace TPFramework
{
    public class NetworkSerialization
    {
        public static Byte[] ReadBytes(byte[] buf, ref int offset)
        {
            Int16 size = ReadInt16(buf, ref offset);

            byte[] writeBuf = new byte[size];
            Array.Copy(buf, offset, writeBuf, 0, size);
            offset += size;
            return writeBuf;
        }

        public static Byte ReadByte(byte[] buf, ref int offset)
        {
            Byte val = buf[offset];
            offset += 1;
            return val;
        }

        public static SByte ReadSByte(byte[] buf, ref int offset)
        {
            return (SByte)ReadByte(buf, ref offset);
        }

        public static Int16 ReadInt16(byte[] buf, ref int offset)
        {
            Int16 val = BitConverter.ToInt16(buf, offset);
            offset += 2;
            return IPAddress.NetworkToHostOrder(val);
        }

        public static UInt16 ReadUInt16(byte[] buf, ref int offset)
        {
            return (UInt16)ReadInt16(buf, ref offset);
        }

        public static Int32 ReadInt32(byte[] buf, ref int offset)
        {
            Int32 val = BitConverter.ToInt32(buf, offset);
            offset += 4;
            return IPAddress.NetworkToHostOrder(val);
        }

        public static UInt32 ReadUInt32(byte[] buf, ref int offset)
        {
            return (UInt32)ReadInt32(buf, ref offset);
        }

        public static Int64 ReadInt64(byte[] buf, ref int offset)
        {
            Int64 val = BitConverter.ToInt64(buf, offset);
            offset += 8;
            return IPAddress.NetworkToHostOrder(val);
        }

        public static UInt64 ReadUInt64(byte[] buf, ref int offset)
        {
            return (UInt64)ReadInt64(buf, ref offset);
        }

        public static Boolean ReadBoolean(byte[] buf, ref int offset)
        {
            Byte val = ReadByte(buf, ref offset);
            return val == 1;
        }

        public static String ReadString(byte[] buf, ref int offset)
        {
            Int16 size = ReadInt16(buf, ref offset);

            byte[] strBuf = new byte[size];
            Array.Copy(buf, offset, strBuf, 0, size);
            offset += size;
            return Encoding.UTF8.GetString(strBuf);
        }

        public static void Write(ref byte[] buf, ref int offset, byte[] val)
        {
            if (buf.Length < offset + val.Length) {
                Array.Resize(ref buf, offset * 2 + val.Length);
            }
            Array.Copy(val, 0, buf, offset, val.Length);
            offset += val.Length;
        }

        public static void Write(ref byte[] buf, ref int offset, Int16 val)
        {
            val = IPAddress.HostToNetworkOrder(val);
            byte[] bytes = BitConverter.GetBytes(val);
            Write(ref buf, ref offset, bytes);
        }

        public static void Write(ref byte[] buf, ref int offset, UInt16 val)
        {
            Write(ref buf, ref offset, (Int16)val);
        }

        public static void Write(ref byte[] buf, ref int offset, Int32 val)
        {
            val = IPAddress.HostToNetworkOrder(val);
            byte[] bytes = BitConverter.GetBytes(val);
            Write(ref buf, ref offset, bytes);
        }

        public static void Write(ref byte[] buf, ref int offset, UInt32 val)
        {
            Write(ref buf, ref offset, (Int32)val);
        }

        public static void Write(ref byte[] buf, ref int offset, Int64 val)
        {
            val = IPAddress.HostToNetworkOrder(val);
            byte[] bytes = BitConverter.GetBytes(val);
            Write(ref buf, ref offset, bytes);
        }

        public static void Write(ref byte[] buf, ref int offset, UInt64 val)
        {
            Write(ref buf, ref offset, (Int64)val);
        }

        public static void Write(ref byte[] buf, ref int offset, Byte val)
        {
            byte[] singleByte = new byte[1];
            singleByte[0] = val;
            Write(ref buf, ref offset, singleByte);
        }

        public static void Write(ref byte[] buf, ref int offset, SByte val)
        {
            Write(ref buf, ref offset, (Byte)val);
        }

        public static void Write(ref byte[] buf, ref int offset, Boolean val)
        {
            Write(ref buf, ref offset, (byte)(val ? 1 : 0));
        }

        public static void Write(ref byte[] buf, ref int offset, String val)
        {
            byte[] str = Encoding.UTF8.GetBytes(val);
            Write(ref buf, ref offset, (Int16)str.Length);
            Write(ref buf, ref offset, str);
        }

        public static void WriteBytesWithSize(ref byte[] buf, ref int offset, Byte[] val)
        {
            Write(ref buf, ref offset, (Int16)val.Length);
            Write(ref buf, ref offset, val);
        }
    }
}
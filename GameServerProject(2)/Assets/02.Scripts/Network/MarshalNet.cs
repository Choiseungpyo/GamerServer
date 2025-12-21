using System;
using System.Runtime.InteropServices;
using System.Text;

public static class MarshalNet
{
    public static byte[] StructToBytes<T>(T obj) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        byte[] buffer = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.StructureToPtr(obj, ptr, false);
            Marshal.Copy(ptr, buffer, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return buffer;
    }

    public static T BytesToStruct<T>(byte[] buffer, int offset = 0) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        IntPtr ptr = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.Copy(buffer, offset, ptr, size);
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public static void WriteFixedAscii(byte[] dst, string s)
    {
        if (dst == null) return;

        for (int i = 0; i < dst.Length; i++) dst[i] = 0;

        if (string.IsNullOrEmpty(s)) return;

        byte[] src = Encoding.ASCII.GetBytes(s);
        int n = src.Length;
        if (n > dst.Length - 1) n = dst.Length - 1;

        Buffer.BlockCopy(src, 0, dst, 0, n);
        dst[n] = 0;
    }

    public static string ReadFixedAscii(byte[] src)
    {
        if (src == null) return "";

        int n = 0;
        for (int i = 0; i < src.Length; i++)
        {
            if (src[i] == 0) break;
            n++;
        }
        return Encoding.ASCII.GetString(src, 0, n);
    }
}
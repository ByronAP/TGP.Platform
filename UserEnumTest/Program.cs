using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Enumerating users...");
        var users = GetLocalUsers();
        Console.WriteLine($"Found {users.Count} users:");
        foreach (var user in users)
        {
            Console.WriteLine($"- {user}");
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct USER_INFO_0
    {
        public string usri0_name;
    }

    [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int NetUserEnum(
        [MarshalAs(UnmanagedType.LPWStr)] string servername,
        int level,
        int filter,
        out IntPtr bufptr,
        int prefmaxlen,
        out int entriesread,
        out int totalentries,
        out int resume_handle);

    [DllImport("Netapi32.dll", SetLastError = true)]
    public static extern int NetApiBufferFree(IntPtr Buffer);

    public const int FILTER_NORMAL_ACCOUNT = 0x0002;

    public static List<string> GetLocalUsers()
    {
        var users = new List<string>();
        int entriesRead, totalEntries, resumeHandle = 0;
        IntPtr bufPtr = IntPtr.Zero;

        try
        {
            int result = NetUserEnum(null, 0, FILTER_NORMAL_ACCOUNT, out bufPtr, -1, out entriesRead, out totalEntries, out resumeHandle);
            
            if (result == 0 || result == 234)
            {
                var iter = bufPtr;
                int structSize = Marshal.SizeOf<USER_INFO_0>();

                for (int i = 0; i < entriesRead; i++)
                {
                    var userInfo = Marshal.PtrToStructure<USER_INFO_0>(iter);
                    if (!string.IsNullOrEmpty(userInfo.usri0_name))
                    {
                        users.Add(userInfo.usri0_name);
                    }
                    iter += structSize;
                }
            }
            else 
            {
                Console.WriteLine($"NetUserEnum failed with error: {result}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
        finally
        {
            if (bufPtr != IntPtr.Zero)
            {
                NetApiBufferFree(bufPtr);
            }
        }

        return users;
    }
}

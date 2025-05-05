#if !UNITY_EDITOR_OSX

using System;
using System.IO;
using System.Text;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using UnityEditor;
using System.Diagnostics;

public static class ShowInExplorer
{
    // API 常量声明
    private const int FILE_QUERY_ACCESS = 0;
    private const int FILE_SHARE_READ = 2;
    private const int CREATION_DISPOSITION_OPEN_EXISTING = 3;
    private const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const int MAX_PATH = 260;

    // Windows API 声明
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, int dwShareMode, IntPtr lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern int GetFinalPathNameByHandle(IntPtr hFile, [Out] StringBuilder lpszFilePath, int cchFilePath, int dwFlags);

    private static string GetRealPath(string path)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
            throw new IOException("Path not found");

        DirectoryInfo symlink = new DirectoryInfo(path);// No matter if it's a file or folder
        //Handle file / folder
        using (var handle = CreateFile( symlink.FullName, FILE_QUERY_ACCESS, FILE_SHARE_READ, IntPtr.Zero, CREATION_DISPOSITION_OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero))
        {
            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // 动态调整缓冲区大小
            int bufferSize = MAX_PATH;
            StringBuilder result = new StringBuilder(bufferSize);

            while (true)
            {
                int size = GetFinalPathNameByHandle(handle.DangerousGetHandle(), result, result.Capacity, 0);

                if (size == 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (size < result.Capacity)
                {
                    result.Length = size;
                    break;
                }

                result.Capacity = size + 1;
            }

            // 标准化路径格式
            const string prefix = @"\\?\";
            return result.ToString().StartsWith(prefix)
                ? result.ToString().Substring(prefix.Length)
                : result.ToString();
        }
    }

    [MenuItem("Assets/Show in Explorer(Link) %G", false, 18)]
    private static void OpenInExplorer()
    {
        if (Selection.assetGUIDs.Length == 0) return;

        string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
        path = GetRealPath(path);

        Process.Start("explorer.exe", $"/select,\"{path}\"");
    }
}

#endif
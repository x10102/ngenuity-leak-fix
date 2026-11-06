using Microsoft.Win32;
using System;

namespace NGenPatcher
{
    public static partial class PatcherUtils
    {

        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        public static bool CheckDevMode()
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

            // Check for the policy, we can't override that
            using (var policy = hklm.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Appx"))
            {
                if (policy?.GetValue("AllowDevelopmentWithoutDevLicense") is int p && p == 1)
                {
                    Console.WriteLine("Developer mode is disabled by your organization, cannot continue.");
                    return false;
                }
                    
            }

            var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock", writable: true);
            if((key?.GetValue("AllowDevelopmentWithoutDevLicense") is int v && v == 1) &&
                (key?.GetValue("AllowAllTrustedApps") is int w && w == 1))
            {
                Console.WriteLine("Developer mode is already enabled");
                return true;
            } else
            {
                key?.Close();
                Console.WriteLine("Developer mode is disabled, attempting to enable it...");
                try { 
                    var newkey = hklm.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock", writable: true);
                    newkey.SetValue("AllowDevelopmentWithoutDevLicense", 1, RegistryValueKind.DWord);
                    newkey.SetValue("AllowAllTrustedApps", 1, RegistryValueKind.DWord);
                    Console.WriteLine("Developer mode enabled");
                    return true;
                } catch
                {
                    Console.WriteLine("Failed to enable developer mode, please enable it manually.");
                    return false;
                }
            }
        }
    }
}

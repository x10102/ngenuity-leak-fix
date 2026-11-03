using System;
using System.Collections.Generic;
using Windows.Management.Deployment;
using System.Text;

namespace NGenPatcher 
{
    public static partial class PatcherUtils
    {
        public static bool UninstallNgenuity()
        {
            var pacman = new PackageManager();
            var ngenPackage = pacman.FindPackagesForUser("").FirstOrDefault(pack => pack.DisplayName.ToLower().Contains("ngenuity"));
            if (ngenPackage is null)
            {
                Console.WriteLine("Unable to locate NGenuity package.");
                return false;
            }
            pacman.RemovePackageAsync(ngenPackage.Id.FullName).Wait();
            return true;
        }

        public static bool ReinstallNgenuity(string path)
        {
            var pacman = new PackageManager();
            var manifest = Path.Combine(path, "AppxManifest.xml");
            var result = pacman.RegisterPackageAsync(new Uri(manifest), null, DeploymentOptions.DevelopmentMode).AsTask().GetAwaiter().GetResult();
            if(result.ExtendedErrorCode == null)
            {
                return true;
            } else
            {
                Console.WriteLine("Error: " + result.ErrorText);
                return false;
            }
        }
    }
}

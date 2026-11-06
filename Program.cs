using System.Diagnostics;
using NGenPatcher;

const string banner = @"
 @@@@@@@  @@@@@@@    @@@@@@    @@@@@@@  @@@  @@@  @@@@@@@  @@@@@@@@  @@@  @@@  
@@@@@@@@  @@@@@@@@  @@@@@@@@  @@@@@@@@  @@@  @@@  @@@@@@@  @@@@@@@@  @@@  @@@  
!@@       @@!  @@@  @@!  @@@  !@@       @@!  !@@    @@!    @@!       @@!  !@@  
!@!       !@!  @!@  !@!  @!@  !@!       !@!  @!!    !@!    !@!       !@!  @!!  
!@!       @!@!!@!   @!@!@!@!  !@!       @!@@!@!     @!!    @!!!:!    @!@@!@!   
!!!       !!@!@!    !!!@!!!!  !!!       !!@!!!      !!!    !!!!!:    !!@!!!    
:!!       !!: :!!   !!:  !!!  :!!       !!: :!!     !!:    !!:       !!: :!!   
:!:       :!:  !:!  :!:  !:!  :!:       :!:  !:!    :!:    :!:       :!:  !:!  
 ::: :::  ::   :::  ::   :::   ::: :::   ::  :::     ::     :: ::::   ::  :::  
 :: :: :   :   : :   :   : :   :: :: :   :   :::     :     : :: ::    :   :::  
";
static string? findNGenInstallFolder()
{
    var procList = Process.GetProcessesByName("NGenuity2Helper");
    if (procList.Length < 1)
    {
        Console.WriteLine("Process not found. Run as admin or make sure NGenuity is running.");
        return null;
    }
    else if (procList.Length > 1)
    {
        Console.WriteLine("Multiple running instances of NGenuity2Helper.exe found. This is weird. Restart your PC i guess.");
        return null;
    }
    if (procList[0].MainModule is null)
    {
        Console.WriteLine("Could not find path for NGenuity, is it really running?");
    }
    string filename = procList[0].MainModule!.FileName;
    return Path.GetFullPath(
            Path.Combine(
                Path.GetDirectoryName(filename)!, "..", "..")
            );
}

static string copyNGenFiles(string path)
{
    var tempDir = Directory.CreateTempSubdirectory(prefix: "ngenpatcher_");
    PatcherUtils.CopyDirectory(path, tempDir.FullName, true);
    return tempDir.FullName;
}

Console.Title = "NGenuity Legacy Patcher";
Console.WriteLine(banner);
Console.WriteLine(new string('=', 75));
Console.WriteLine("HyperX NGenuity Legacy leak fix patch");
Console.WriteLine("Press any key to begin...");
Console.WriteLine(new string('=', 75));
Console.ReadKey();

if (!PatcherUtils.CheckDevMode())
{
    Console.ReadKey();
    return;
}

// 1. Copy the install out of the locked down store apps folder and into a temp directory

var installFolder = findNGenInstallFolder();
Console.WriteLine("Found NGenuity install folder at: " + installFolder);
if(installFolder == null)
{
    Console.WriteLine("NGenuity install not found");
    return;
}
Console.WriteLine("Copying files to temp directory...");
var copyPath = copyNGenFiles(installFolder);

// 2. Patch the helper exe

Console.WriteLine("Patching NGenuity2Helper.exe...");
var exeCopyPath = Path.Combine(copyPath, "Assets", "Native", "NGenuity2Helper.exe");
if(!PatcherUtils.PatchHelperExecutable(exeCopyPath))
{
    Console.WriteLine("Patch failed");
    Directory.Delete(copyPath, true);
    return;
}

// 3. Uninstall the current version

Console.WriteLine("Uninstalling old NGenuity...");
PatcherUtils.UninstallNgenuity();

// 4. Copy the patched files into a permanent directory and register the package again

Console.WriteLine("Copying patched install directory to ProgramFiles...");
var newInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NGENUITY");
PatcherUtils.CopyDirectory(copyPath, newInstallPath, true);
Console.WriteLine("Removing temporary directory...");
Directory.Delete(copyPath, true);
Console.WriteLine("Installing patched package...");
if(!PatcherUtils.ReinstallNgenuity(newInstallPath))
{
    Console.WriteLine("Reinstall failed");
    Directory.Delete(newInstallPath, true);
    return;
}
Console.WriteLine(new string('=', 75));
Console.WriteLine("Done. Press any key to exit.");
Console.ReadKey();
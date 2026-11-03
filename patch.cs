using System;
using dnlib;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace NGenPatcher
{
    public static partial class PatcherUtils
    {
        public static bool PatchHelperExecutable(string path)
        {
            // The problem here is a function called GetProcessName in the class Ngenuity2.Common.Utils
            // It takes a PID as a parameter, grabs a handle to the process, gets its name and returns it
            // But the handle is never closed!
            // Since this runs on a timed loop to check for application profiles or interfering software or some other bs,
            // we end up with millions of handles to processes that exited days ago
            // The leak then comes from the windows kernel overhead required for an open handle times 900 billion
            // We will manually insert IL instructions that call CloseHandle() on the process handle before the function returns
            // The wrapper for the winapi call is already in the code, they kinda just forgot to use it

            // Load the EXE and find the Utils class where the leaking method is
            var module = ModuleDefMD.Load(File.ReadAllBytes(path));
            var utilsClass = module.GetTypes().FirstOrDefault(type => type.Namespace == "NGenuity2.Common" && type.Name == "Utils");
            if(utilsClass is null)
            {
                Console.WriteLine("Could not find namespace or class: NGenuity2.Common.Utils; Make sure you're using the latest version of NGenuity.");
                return false;
            }
            Console.WriteLine("Found class NGenuity2.Common.Utils");
            var targetMethod = utilsClass.Methods.FirstOrDefault(method => method.Name == "GetProcessName");
            if(targetMethod is null)
            {
                Console.WriteLine("Could not find method: GetProcessName; Make sure you're using the latest version of NGenuity.");
                return false;
            }

            // Find the CloseHandle wrapper inside the Win32API class
            var w32APIInterface = module.GetTypes().FirstOrDefault(type => type.Namespace == "NGenuity2" && type.Name == "Win32API");

            if (w32APIInterface is null) {
                Console.WriteLine("Could not find namespace or class: NGenuity2.Win32API; Make sure you're using the latest version of NGenuity.");
                return false;
            }

            var closeHandleMethod = w32APIInterface.Methods.FirstOrDefault(method => method.Name == "CloseHandle");
            if (closeHandleMethod is null)
            {
                Console.WriteLine("Could not find method: CloseHandle; Make sure you're using the latest version of NGenuity.");
                return false;
            }

            // The call to CloseHandle is three IL instructions:
            // ldloc.2 - Take variable at local index 2 (the handle) and push it onto the stack
            // call - Call the method
            // pop - Clean up the value from the stack
            var instLdloc2 = OpCodes.Ldloc_2.ToInstruction();
            var instCallCloseHandle = OpCodes.Call.ToInstruction(closeHandleMethod);
            var instPop = OpCodes.Pop.ToInstruction();

            // We need to insert this sequence twice, in both of the branches
            var patchStart = 20;
            targetMethod.Body.Instructions.Insert(patchStart, instLdloc2);
            targetMethod.Body.Instructions.Insert(patchStart + 1, instCallCloseHandle);
            targetMethod.Body.Instructions.Insert(patchStart + 2, instPop);

            patchStart = 26;
            targetMethod.Body.Instructions.Insert(patchStart, instLdloc2);
            targetMethod.Body.Instructions.Insert(patchStart + 1, instCallCloseHandle);
            targetMethod.Body.Instructions.Insert(patchStart + 2, instPop);

            // Inserting new instructions messed up the jump indices for the if statement
            // So we point them to the new correct location
            var instJumpAfterIf = OpCodes.Brfalse_S.ToInstruction(targetMethod.Body.Instructions[26]);
            targetMethod.Body.Instructions[13] = instJumpAfterIf;
            targetMethod.Body.Instructions[19] = instJumpAfterIf;

            // TODO:
            // Technically, in the fallthrough branch, it's possible for the handle to be NULL
            // You shouldn't ever call CloseHandle() on NULL, but in practice, it will just set the error flag instead of crashing
            // I leave it like this because I'm kinda bad at working with IL lmao

            module.Write(path);
            return true;
        }
    }
}

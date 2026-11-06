# NGenuity Leak fix
Stops NGenuity Legacy from leaking process handles and filling up your memory. Works by patching in the missing `CloseHandle()` calls in a helper function.

Download the release or build in Visual Studio, run as Administrator, it's automatic from there. Make sure NGenuity is installed **and running**.
(Developer mode has to be enabled, otherwise the patch will work but NGenuity won't get reinstalled)

Written in about two hours, probably buggy, send issues and PRs.

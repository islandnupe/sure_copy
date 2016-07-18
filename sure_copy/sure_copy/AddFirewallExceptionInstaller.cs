using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Configuration.Install;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.Remoting.Contexts;
using System.IO;

namespace sure_copy
{
    
    [RunInstaller(true)]
    class AddFirewallExceptionInstaller : Installer
    {
        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);

            var path = Path.GetDirectoryName(Context.Parameters["assemblypath"]);
            OpenFirewallForProgram(Path.Combine(path, "YourExe.exe"),
                                   "Your program name for display");
        }

        private static void OpenFirewallForProgram(string exeFileName, string displayName)
        {
            var proc = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments =
                        string.Format(
                            "firewall add allowedprogram program=\"{0}\" name=\"{1}\" profile=\"ALL\"",
                            exeFileName, displayName),
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            proc.WaitForExit();
        }
    }
}

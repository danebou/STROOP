using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting.Providers;
using SM64_Diagnostic.Structs.Configurations;
using System.IO;
using System.Reflection;

namespace SM64_Diagnostic.Managers
{
    static class DecompilerManager
    {
        static string marioRamPath = @"Resources\DAKompiler\marioRam";
        static string decompile_py;

        static DecompilerManager()
        {
            // Load python script
            var asm = Assembly.GetExecutingAssembly();
            using (StreamReader stream = new StreamReader(
                asm.GetManifestResourceStream("SM64_Diagnostic.EmbeddedResources.decompile.py")))
            {
                decompile_py = stream.ReadToEnd();
            }
        }

        static void UpdateSnapshot()
        {
            using (var file = new StreamWriter(marioRamPath))
            {
                file.Write(Config.Stream.Ram);
            }
        }

        static public void Test() {

            //return;
            ScriptEngine python = Python.CreateEngine();

            // Disable Zip
            var pc = HostingHelpers.GetLanguageContext(python) as PythonContext;
            var hooks = pc.SystemState.Get__dict__()["path_hooks"] as List;
            hooks.Clear();

            python.SetSearchPaths(new List<string>() { @"Resources\DAKompiler", @"Lib" });
            var compiler = python.ExecuteFile(@"Resources\DAKompiler\dakompiler.py");
            var result = compiler.Engine.Execute(decompile_py, compiler);
        }
    }
}

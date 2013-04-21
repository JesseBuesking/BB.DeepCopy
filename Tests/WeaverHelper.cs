using System;
using System.IO;
using System.Reflection;
using BB.DeepCopy;
using Mono.Cecil;

namespace Tests
{
    public class WeaverHelper
    {
//        private const string _folder = "BB.Genetics";
//        private const string _name = "BB.Genetics.Common";

        private const string _folder = "AssemblyToProcess";

        private const string _name = "AssemblyToProcess";

        private const string _marker = ".copy.";

        public static Assembly WeaveAssembly()
        {
            var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory,
                String.Format(@"..\..\..\{0}\{1}.csproj", _folder, _name)));

            string directoryName = Path.GetDirectoryName(projectPath);
            if (String.IsNullOrWhiteSpace(directoryName))
                throw new Exception(String.Format("Invalid project path {0}.", projectPath));

            string dir = Path.Combine(directoryName, @"bin\Debug\");

            WeaverHelper.CleanupExistingFiles(dir);

            var assemblyPath = Path.Combine(dir, String.Format(@"{0}.dll", _name));
            var pdbPath = Path.Combine(dir, String.Format(@"{0}.pdb", _name));

#if (!DEBUG)
        assemblyPath = assemblyPath.Replace("Debug", "Release");
#endif

            // Copy the dll over.

            WeaverHelper.CopyAssembly2ForDebugging(assemblyPath);

            var newAssembly = assemblyPath.Replace(
                ".dll", String.Format("{0}{1}.dll", WeaverHelper._marker, Guid.NewGuid()));
            File.Copy(assemblyPath, newAssembly, true);

            // Copy the pdb over too.
            var newPdb = pdbPath.Replace(
                ".pdb", String.Format("{0}{1}.pdb", WeaverHelper._marker, Guid.NewGuid()));
            File.Copy(pdbPath, newPdb, true);

            var moduleDefinition = ModuleDefinition.ReadModule(newAssembly);
            var weavingTask = new ModuleWeaver {ModuleDefinition = moduleDefinition};

            weavingTask.Execute();

            moduleDefinition.Write(newAssembly);
            return Assembly.LoadFile(newAssembly);
        }

        /// <summary>
        /// Copying 1 assembly under the same name each time (so that I can just refresh IL spy and get
        /// the updated contents).
        /// </summary>
        /// <param name="assemblyPath"></param>
        private static void CopyAssembly2ForDebugging(string assemblyPath)
        {
            var assemblyToInspect = assemblyPath.Replace(".dll", "2.dll");
            File.Copy(assemblyPath, assemblyToInspect, true);

            var moduleDefinition = ModuleDefinition.ReadModule(assemblyToInspect);
            var weavingTask = new ModuleWeaver {ModuleDefinition = moduleDefinition};

            weavingTask.Execute();

            moduleDefinition.Write(assemblyToInspect);
        }

        private static void CleanupExistingFiles(string dir)
        {
            foreach (string path in Directory.EnumerateFiles(dir))
            {
                try
                {
                    if (path.Contains(WeaverHelper._marker))
                        File.Delete(path);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
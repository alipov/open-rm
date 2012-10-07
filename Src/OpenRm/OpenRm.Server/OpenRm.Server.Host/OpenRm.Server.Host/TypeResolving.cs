using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenRm.Server.Host
{
    public static class TypeResolving
    {
        public static void RegisterTypeResolving()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveHandler;
            AppDomain.CurrentDomain.TypeResolve += TypeResolveHandler;
        }

        public static Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Name)) return null;

            string requestedAssembly = args.Name.Substring(0, args.Name.IndexOf(","));
            return LoadAssembly(requestedAssembly);
        }

        public static Assembly TypeResolveHandler(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("OpenRm.Common.Entities"))
                return LoadAssembly("OpenRm.Common.Entities");
            return null;

            //return DefaultResolveHandler(args.Name);

            //string assemblyPath = string.Empty;

            //if (args.Name.StartsWith("OpenRm.Common.Entities"))
            //{
            //    DirectoryInfo rootDirecotory = Directory.GetParent(Directory.GetCurrentDirectory());
            //    assemblyPath = Directory.GetFiles
            //                    (rootDirecotory.FullName, "OpenRm.Common.Entities" + ".dll", SearchOption.AllDirectories).Single();
            //}

            ////Load the assembly from the specified path.
            //Assembly myAssembly = null;

            //if (!string.IsNullOrWhiteSpace(assemblyPath))
            //    myAssembly = Assembly.LoadFrom(assemblyPath);

            ////Return the loaded assembly.
            //return myAssembly;
        }

        // took from http://www.chilkatsoft.com/p/p_502.asp
        //This handler is called only when the common language runtime tries to bind to the assembly and fails.
        private static Assembly LoadAssembly(string requestedAssembly)
        {
            DirectoryInfo rootDirectory = GetOpenRmRootDirectory(requestedAssembly);
            if (rootDirectory == null) return null;

            List<string> assemblyPaths = Directory.GetFiles
                (rootDirectory.FullName, requestedAssembly + ".dll", SearchOption.AllDirectories).ToList();

            if (assemblyPaths.Count > 1)
            {
                throw new TypeLoadException(string.Format
                    ("Found more that one '{0}' file in the following root directory: '{1}'", requestedAssembly, rootDirectory));
            }

            if (assemblyPaths.Count == 0)
            {
                throw new DllNotFoundException(string.Format
                    ("Not found the '{0}' file in the following root directory: '{1}'", requestedAssembly, rootDirectory));
            }

            string assemblyPath = assemblyPaths.Single();

            //Load the assembly from the specified path.
            Assembly myAssembly = null;

            // failing to ignore queries for satellite resource assemblies or using [assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)] 
            // in AssemblyInfo.cs will crash the program on non en-US based system cultures.
            if (!string.IsNullOrWhiteSpace(assemblyPath))
                myAssembly = Assembly.LoadFrom(assemblyPath);

            //Return the loaded assembly.
            return myAssembly;
        }

        private static DirectoryInfo GetOpenRmRootDirectory(string requestedAssembly)
        {
            if (requestedAssembly.EndsWith("resources")) return null;

            string entryAssemblyPath = Assembly.GetEntryAssembly().Location;
            string entryAssemblyDirectoryName = Path.GetDirectoryName(entryAssemblyPath);

            if (string.IsNullOrEmpty(entryAssemblyDirectoryName)) return null;

            DirectoryInfo rootDirectory = Directory.GetParent(entryAssemblyDirectoryName);
            return rootDirectory;
        }
    }
}
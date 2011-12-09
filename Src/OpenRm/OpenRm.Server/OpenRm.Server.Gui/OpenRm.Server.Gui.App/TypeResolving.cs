using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenRm.Server.Gui
{
    public static class TypeResolving
    {
        public static void RegisterTypeResolving()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveHandler;
            AppDomain.CurrentDomain.TypeResolve += TypeResolveHandler;
        }

        // took from http://www.chilkatsoft.com/p/p_502.asp
        public static Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            //var assemblyPath = string.Empty;

            //Assembly objExecutingAssemblies = Assembly.GetExecutingAssembly();
            //AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

            // Note: no need to check whether or not the assembly is referenced
            ////Loop through the array of referenced assembly names.
            //foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            //{
            //    var requestedAssembly = args.Name.Substring(0, args.Name.IndexOf(","));

            //    //Check for the assembly names that have raised the "AssemblyResolve" event.
            //    if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == requestedAssembly)
            //    {
            //        //Build the path of the assembly from where it has to be loaded.
            //        var rootDirecotory = Directory.GetParent(Directory.GetCurrentDirectory());
            //        assemblyPath = Directory.GetFiles
            //                        (rootDirecotory.FullName, requestedAssembly + ".dll", SearchOption.AllDirectories).Single();
            //        break;
            //        //assemblyPath = Path.Combine(rootDirecotory.FullName, "Common", requestedAssembly + ".dll");
            //    }
            //}

#if DEBUG
            if (args.Name.Contains("Snoop")) return null;
#endif

            var requestedAssembly = args.Name.Substring(0, args.Name.IndexOf(","));

            if (requestedAssembly.EndsWith("resources")) return null;

            var rootDirecotory = Directory.GetParent(Directory.GetCurrentDirectory());
            var assemblyPaths = Directory.GetFiles
                (rootDirecotory.FullName, requestedAssembly + ".dll", SearchOption.AllDirectories).ToList();

            if (assemblyPaths.Count > 1)
            {
                throw new TypeLoadException(string.Format
                    ("Found more that one '{0}' file in the following root directory: '{1}'",
                                                requestedAssembly, rootDirecotory));
            }

            if (assemblyPaths.Count == 0)
            {
                throw new DllNotFoundException(string.Format
                    ("Not found the '{0}' file in the following root directory: '{1}'",
                                                requestedAssembly, rootDirecotory));
            }

            var assemblyPath = assemblyPaths.Single();

            //Load the assembly from the specified path.
            Assembly myAssembly = null;

            // failing to ignore queries for satellite resource assemblies or using [assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)] 
            // in AssemblyInfo.cs will crash the program on non en-US based system cultures.
            if (!string.IsNullOrWhiteSpace(assemblyPath))
                myAssembly = Assembly.LoadFrom(assemblyPath);

            //Return the loaded assembly.
            return myAssembly;
        }

        public static Assembly TypeResolveHandler(object sender, ResolveEventArgs args)
        {
            var assemblyPath = string.Empty;

            if (args.Name.StartsWith("OpenRm.Common.Entities"))
            {
                var rootDirecotory = Directory.GetParent(Directory.GetCurrentDirectory());
                assemblyPath = Directory.GetFiles
                                (rootDirecotory.FullName, "OpenRm.Common.Entities" + ".dll", SearchOption.AllDirectories).Single();
            }

            //Load the assembly from the specified path.
            Assembly myAssembly = null;

            if (!string.IsNullOrWhiteSpace(assemblyPath))
                myAssembly = Assembly.LoadFrom(assemblyPath);

            //Return the loaded assembly.
            return myAssembly;
        }
    }
}

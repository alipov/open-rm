using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using OpenRm.Common.Entities;


namespace OpenRm.Server.Host
{
    class Server
    {
        public static int Port = 3777;
        public static int MaxNumConnections = 1000;     //maximum number of connections
        public static int ReceiveBufferSize = 16;      //recieve buffer size for tcp connection
        private const string LogFilenamePattern = "server-<date>.log";
        
        static void Main(string[] args)
        {
            // Done because exception was thrown before Main. Solution found here:
            // http://www.codeproject.com/Questions/184743/AssemblyResolve-event-not-hit
            var appDomain = AppDomain.CurrentDomain;
            appDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveHandler);
            AppDomain.CurrentDomain.TypeResolve += new ResolveEventHandler(TypeResolveHandler);

            //Main code
            StartHost();

            // End Of program
        }


        private static void StartHost()
        {
            //TODO: get directory from app.config
            //System.Configuration.
            Logger.CreateLogFile("logs", LogFilenamePattern);
            Logger.WriteStr("Started");

            TCPServerListener srv = new TCPServerListener(Port, MaxNumConnections, ReceiveBufferSize);

        }



        // took from http://www.chilkatsoft.com/p/p_502.asp
        static Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            var assemblyPath = string.Empty;

            Assembly objExecutingAssemblies = Assembly.GetExecutingAssembly();
            AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                var requestedAssembly = args.Name.Substring(0, args.Name.IndexOf(","));

                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == requestedAssembly)
                {
                    //Build the path of the assembly from where it has to be loaded.
                    var rootDirecotory = Directory.GetParent(Directory.GetCurrentDirectory());
                    assemblyPath = Directory.GetFiles
                                    (rootDirecotory.FullName, requestedAssembly + ".dll", SearchOption.AllDirectories).Single();
                    break;
                    //assemblyPath = Path.Combine(rootDirecotory.FullName, "Common", requestedAssembly + ".dll");
                }
            }
            //Load the assembly from the specified path.
            Assembly myAssembly = null;

            // failing to ignore queries for satellite resource assemblies or using [assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)] 
            // in AssemblyInfo.cs will crash the program on non en-US based system cultures.
            if (!string.IsNullOrWhiteSpace(assemblyPath))
                myAssembly = Assembly.LoadFrom(assemblyPath);

            //Return the loaded assembly.
            return myAssembly;
        }

        static Assembly TypeResolveHandler(object sender, ResolveEventArgs args)
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

using System;
using System.Net.Sockets;
using System.Windows;
using OpenRm.Agent.CustomControls;
using OpenRm.Common.Entities;
using System.Configuration;
using System.Reflection;
using System.IO;
using System.Linq;


namespace OpenRm.Agent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool agentStarted = false;            // flag that indicates current status of agent. can be changed by pressing "Stop Agent" control
        private string serverIP;
        private int serverPort;
        private string logFilenamePattern;
        public static int ReceiveBufferSize = 64;      //recieve buffer size for tcp connection


        private NotifyIconWrapper _notifyIconComponent;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveHandler);
            //AppDomain.CurrentDomain.TypeResolve += new ResolveEventHandler(TypeResolveHandler);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _notifyIconComponent = new NotifyIconWrapper();
            _notifyIconComponent.StartAgentClick += StartAgent;
        }

        private void StartAgent(object sender, EventArgs e)
        {
            // Done because exception was thrown before Main. Solution found here:
            // http://www.codeproject.com/Questions/184743/AssemblyResolve-event-not-hit
            
            

            if (ReadConfigFile())
            {

                Logger.CreateLogFile("logs", logFilenamePattern);       // creates "logs" directory in binaries folder and set log filename
                Logger.WriteStr("Client started.");
                agentStarted = true;

                TCPclient client = new TCPclient(serverIP, serverPort, ReceiveBufferSize);

                Logger.WriteStr("Client terminated");
            }

        }


        // read configuration from config file
        private bool ReadConfigFile()
        {
            try
            {
                 
                serverIP = ConfigurationManager.AppSettings["ServerIP"];
                serverPort = Int32.Parse(ConfigurationManager.AppSettings["ServerPort"]);
                logFilenamePattern = ConfigurationManager.AppSettings["LogFilePattern"];
            }
            catch (Exception ex)
            {
                Logger.CriticalToEventLog("Error while reading config file. Error: " + ex.Message);
                return false;
            }

            return true;   
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


        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _notifyIconComponent.Dispose();
        }
    }
}

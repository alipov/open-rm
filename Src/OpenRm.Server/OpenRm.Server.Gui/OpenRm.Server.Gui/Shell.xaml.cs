using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows;
using System.Xml;
using OpenRm.Common.Entities;
using Woxalizer;

namespace OpenRm.Server.Gui
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : Window
    {
        public Shell()
        {
            InitializeComponent();

            var appDomain = AppDomain.CurrentDomain;
            appDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveHandler);

            // TODO: remove it once done testing
            TcpClient client = null;
            try
            {
                client = new TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));
            }
            catch (Exception e)
            {
                return;
            }
            client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050));

            //var emp = new Emp() { Age = 5, Name = "Alex" };
            var msg = new Message()
                          {
                              Command = new IpConfigData() {},
                              Id = 55

                          };
            var ns = client.GetStream();
            var writer = XmlWriter.Create(ns);
            using (var woxalizer = new WoxalizerUtil(TypeResolveHandler))
            {
                woxalizer.Save(msg, writer);
            }
        }

        static Assembly TypeResolveHandler(object sender, ResolveEventArgs args)
        {
            Assembly objExecutingAssemblies = Assembly.GetExecutingAssembly();
            //AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            //foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            //{

            //}
            return objExecutingAssemblies;
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
    }

    public class Emp
    {
        public string Name;
        public  int Age;
    }
}

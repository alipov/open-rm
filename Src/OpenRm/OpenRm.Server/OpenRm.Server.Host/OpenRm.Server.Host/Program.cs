namespace OpenRm.Server.Host
{
    class Program
    {
        static void Main()
        {
            // Done because exception was thrown before Main. Solution found here:
            // http://www.codeproject.com/Questions/184743/AssemblyResolve-event-not-hit
            TypeResolving.RegisterTypeResolving();

            //Main code
            var server = new Server();
            server.Run();
        }
    }
}

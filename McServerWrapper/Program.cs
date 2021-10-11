using System;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Win32.SafeHandles;

namespace McServerWrapper
{
    class Program
    {
        private const string ServerJar = "SERVERJAR";
        private const string ServerDir = "SERVERDIR";
        private const string BackupDir = "BACKUPDIR";

        static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("main");
            var serverDir = Environment.GetEnvironmentVariable(ServerDir) ??
                            throw new Exception("Server directory must be specified");
            var serverJar = Environment.GetEnvironmentVariable(ServerJar) ?? throw new Exception("Server jar must be specified");
            var backupDir = Environment.GetEnvironmentVariable(BackupDir) ??
                            throw new Exception("BackupDir must be specified");
            var wrapperRef = actorSystem.ActorOf(ServerWrapperActor.Props(new ServerWrapperActor.MineCraftStartInfo(serverJar, serverDir), backupDir));
            wrapperRef.Tell(new ServerWrapperActor.Start());
            actorSystem.WhenTerminated.Wait();
        }
    }
}
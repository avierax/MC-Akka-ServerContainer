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
        private const string NamedBackupDir = "NAMEDBACKUPDIR";

        static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("main");
            var serverDir = Environment.GetEnvironmentVariable(ServerDir) ??
                            throw new Exception("Server directory must be specified");
            var serverJar = Environment.GetEnvironmentVariable(ServerJar) ?? throw new Exception("Server jar must be specified");
            var backupDir = Environment.GetEnvironmentVariable(BackupDir) ??
                            throw new Exception("BackupDir must be specified");
            var namedBackupDir = Environment.GetEnvironmentVariable(NamedBackupDir) ??
                            throw new Exception("NamedBackupDir must be specified");
            var wrapperRef = actorSystem.ActorOf(ServerWrapperActor.Props(new ServerWrapperActor.MineCraftStartInfo(serverJar, serverDir), backupDir, namedBackupDir), "wrapper");
            var consoleActor = actorSystem.ActorOf(ConsoleActor.Props(wrapperRef), "consoleReader");
            consoleActor.Tell("start");
            wrapperRef.Tell(new ServerWrapperActor.Start());
            actorSystem.WhenTerminated.Wait();
        }
    }
}
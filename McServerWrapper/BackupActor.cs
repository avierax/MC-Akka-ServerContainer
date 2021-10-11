using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Util.Internal;

namespace McServerWrapper
{
    class BackupActor : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly string _backupDirectory;
        private readonly string _serverDirectory;
        private Process _process = null!;

        public static Props Props(string backupDirectory, string serverDirectory) =>
            Akka.Actor.Props.Create(() => new BackupActor(backupDirectory, serverDirectory));

        public BackupActor(string backupDirectory, string serverDirectory)
        {
            _backupDirectory = backupDirectory;
            _serverDirectory = serverDirectory;
            Become(Ready);
        }


        private void Ready()
        {
            Receive<DoBackup>(s =>
            {
                _log.Info($"Received '{s}'");
                var backupFilename = $"{DateTime.Now:yyyy-MM-ddTHH-mm-ss}.tar";
                var backupFullFilename = $"{_backupDirectory}/{backupFilename}";
                var processStartupInfo = new ProcessStartInfo
                {
                    FileName = "tar",
                    ArgumentList = {"cvf", backupFullFilename, _serverDirectory},
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                _log.Info($"executing {processStartupInfo.FileName} with arguments {processStartupInfo.ArgumentList}");
                _process = Process.Start(processStartupInfo) ?? throw new Exception("Could not start tar utility");
                _process.StandardOutput.ReadLineAsync().PipeTo(Self);
                _process.WaitForExitAsync().ContinueWith(task => new Finished(_process.ExitCode, backupFullFilename))
                    .PipeTo(Self);
                Become(Working);
            });
        }

        private record Finished(int ProcessExitCode, string BackupFullFilename);

        public record DoBackup;

        private void Working()
        {
            Receive<string>(s =>
            {
                if(s!=null)
                {
                    _log.Info($"[TAR] {s}");
                    _process.StandardOutput.ReadLineAsync().PipeTo(Self);
                }
            });
            Receive<Finished>(done =>
            {
                _log.Info($"Backup process has finished");
                var (exitCode, filename) = done;
                if (exitCode == 0)
                {
                    _log.Info("Success: tar process exit code was zero.");
                    Context.Parent.Tell(new ServerWrapperActor.BackupDone(filename));
                }
                else
                {
                    _log.Info($"Error: tar process exit code was {exitCode}");
                    var error = _process.StandardError.ReadToEnd();
                    Context.Parent.Tell(new ServerWrapperActor.BackupFailed(exitCode, error));
                }
                Become(Ready);
            });
        }
    }
}
using System;
using System.Diagnostics;
using Akka.Actor;
using Akka.Event;

namespace McServerWrapper
{
    class ServerWrapperActor : ReceiveActor, IWithTimers
    {
        private const string SaveToken = "#save";
        private readonly ILoggingAdapter _log = Context.GetLogger();

        #region Messages

        public record Stop();

        public record Start();

        public record BackupDone(string Filename);

        public record PerformBackup;        

        public record BackupFailed(int ExitCode, string? ErrorMsg);

        public record McCommand(string Command);

        #endregion

        private readonly MineCraftStartInfo _mineCraftStartInfo;
        private readonly string _namedBackupDir;
        private Process _process = null!;
        private IActorRef _backupActor = null!;
        private string? _alias;

        public record MineCraftStartInfo(string Jar, string ServerDir);


        public ServerWrapperActor(MineCraftStartInfo mineCraftStartInfo, string backupDir, string namedBackupDir)
        {
            _mineCraftStartInfo = mineCraftStartInfo;
            _namedBackupDir = namedBackupDir;
            Receive<Start>(x =>
            {
                _log.Info("Starting");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    ArgumentList = {"-Xmx5g", "-jar", "server.jar", "--nogui"},
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    WorkingDirectory = mineCraftStartInfo.ServerDir
                };
                _process = Process.Start(startInfo) ?? throw new Exception("Error while starting process");
                _backupActor = Context.ActorOf(
                    BackupActor.Props(namedBackupDir, backupDir, mineCraftStartInfo.ServerDir),
                    "backupWorker");
                Become(Started);
            });
        }

        private void Started()
        {
            _log.Info("Started");
            StdErrReadLineAsync();
            StdOutReadLineAsync();
            Receive<StderrLine>(read =>
            {
                var line = read.Line;
                if (line != null)
                {
                    _log.Info($"[Err] {line}");
                    StdErrReadLineAsync();
                }
                else
                {
                    _log.Info("stderr line was null, probably the child process exited");
                }
            });
            Receive<PerformBackup>(read =>
            {
                _log.Info("Performing backup");
                if (_alias != null)
                {
                    StartBackup($"{_alias}.tar");
                    _alias = null;
                }
                else
                {
                    StartBackup();
                }
            });
            Receive<StdoutLine>(read =>
            {
                var line = read.Line;
                if (line != null)
                {
                    _log.Info($"[Output] {line}");
                    if (line.Contains("[Server thread/INFO]") && line.Contains("Saved the game"))
                    {
                        _log.Info("Waiting 5 seconds before launching backup");
                        Timers.StartSingleTimer("backup", new PerformBackup(), TimeSpan.FromSeconds(5));
                    }
                    if (line.Contains("[Server thread/INFO]: Done"))
                    {
                        _log.Info("Server has started");
                        _log.Info("Disabling auto save");
                        Self.Tell(new McCommand("/save-off"));
                        Timers.StartPeriodicTimer("save-all", new McCommand("/save-all"), TimeSpan.FromMinutes(5),
                            TimeSpan.FromMinutes(5));
                    }

                    if (line.Contains(SaveToken))
                    {
                        var i = line.IndexOf(SaveToken, StringComparison.Ordinal);
                        i = line.IndexOf(' ', i);
                        _alias = line.Substring(i + 1);
                        Timers.Cancel("save-all");
                        Timers.StartPeriodicTimer("save-all", new McCommand("/save-all"), TimeSpan.FromMinutes(0),
                            TimeSpan.FromMinutes(5));
                    }

                    StdOutReadLineAsync();
                }
                else
                {
                    _log.Info("line from stdout is null. probably the process terminated");
                }
            });
            Receive<BackupDone>(done =>
            {
                _log.Info("Backup done");
                _process.StandardInput.WriteLine($"/say backup was done {done.Filename}");
            });
            Receive<BackupFailed>(failed => { _log.Info($"Backup failed {failed}"); });
            Receive<Stop>(stop => { _process.StandardInput.Close(); });
            Receive<McCommand>(command => { _process.StandardInput.WriteLine(command.Command); });
        }

        private void StartBackup(string? backupalias = null)
        {
            _backupActor.Tell(new BackupActor.DoBackup(backupalias));
            _log.Info("Starting backup");
        }

        private void StdErrReadLineAsync()
        {
            _process.StandardError.ReadLineAsync().ContinueWith(t => new StderrLine(t.Result)).PipeTo(Self);
        }

        private void StdOutReadLineAsync()
        {
            _process.StandardOutput.ReadLineAsync().ContinueWith(t => new StdoutLine(t.Result)).PipeTo(Self);
        }

        private record StdoutLine(string? Line);

        private record StderrLine(string? Line);

        public static Props Props(MineCraftStartInfo mineCraftStartInfo, string backupDir, string namedBackupDir) =>
            Akka.Actor.Props.Create(() => new ServerWrapperActor(mineCraftStartInfo, backupDir, namedBackupDir));

        public ITimerScheduler Timers { get; set; } = null!;
    }
}
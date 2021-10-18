using System;
using Akka.Actor;
using Akka.Event;

namespace McServerWrapper
{
    public class ConsoleActor : UntypedActor
    {
        private readonly IActorRef _continuation;
        private ILoggingAdapter _log = Context.GetLogger();

        public static Props Props(IActorRef continuation) =>
            Akka.Actor.Props.Create(() => new ConsoleActor(continuation));

        public ConsoleActor(IActorRef continuation)
        {
            _continuation = continuation;
        }

        protected override void OnReceive(object message)
        {
            if (message is ServerWrapperActor.McCommand)
            {
                _continuation.Tell(message); 
                Self.Tell("continue");
            }
            else
            {
                GetAndValidateInput();
            }
        }

        private void GetAndValidateInput()
        {
            var readLineAsync = Console.In.ReadLineAsync();
            readLineAsync.ContinueWith(t =>
            {
                var mcCommand = new ServerWrapperActor.McCommand(t.Result ?? throw new Exception("command is null"));
                return mcCommand;
            }).PipeTo(Self);
        }
    }
}
using System;
using Akka.Actor;

namespace McServerWrapper
{
    public class ConsoleActor : UntypedActor
    {
        private readonly IActorRef _continuation;

        public static Props Props(IActorRef continuation) =>
            Akka.Actor.Props.Create(() => new ConsoleActor(continuation));

        public ConsoleActor(IActorRef continuation)
        {
            _continuation = continuation;
        }

        protected override void OnReceive(object message)
        {
            GetAndValidateInput();
        }

        private void GetAndValidateInput()
        {
            var line = Console.ReadLine();
            _continuation.Tell(new ServerWrapperActor.McCommand(line ?? throw new Exception("command is null")));
        }
    }
}
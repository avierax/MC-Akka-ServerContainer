namespace McServerWrapper
{
    public class ActorMetadata
    {
        private string? _path;

        public ActorMetadata(string name, ActorMetadata? parent = null)
        {
            Name = name;
            Parent = parent;
        }

        private ActorMetadata? Parent { get; }
        private string Name { get; }

        public string Path => _path ??= $"{Parent?.Path}/${Name}";
    }
}
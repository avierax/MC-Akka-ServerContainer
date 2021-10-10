using System;

namespace SampleProcess
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter command:");
            var command = Console.ReadLine();
            Console.WriteLine($"Command is {command}");
        }
    }
}

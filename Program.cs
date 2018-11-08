using System;
using System.Threading;

namespace AnimalShogi
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Args size is error");
                return;
            }

            Server server = new Server(args[0]);
        }
    }
}

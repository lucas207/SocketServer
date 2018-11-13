using System;

namespace SocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncServer server = new AsyncServer(5656);
            server.StartListening();

        }
    }
}

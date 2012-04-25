using System;
using System.Net;
using System.Windows.Forms;

namespace Connect4Server
{
    class Program
    {

        public static void Main(string[] args)
        {
            int port = 0;
            int timeLimit = 0;
            if (args.Length != 2 || !Int32.TryParse(args[0], out port) || !Int32.TryParse(args[1], out timeLimit))
            {
                Console.WriteLine("usage: " + System.IO.Path.GetFileName(Application.ExecutablePath)
                    + " <portNumber> <timelimit>");
                return;
            }
            else if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                Console.WriteLine("invalid port number");
                return;
            }
            else if (timeLimit <= 0)
            {
                Console.WriteLine("time limit must be positive");
                return;
            }

            try
            {
                Connect4Service serv = new Connect4Service(port, timeLimit, Connect4Service.WhoGoesFirst.random);
                serv.NotificationEvent += (msg) =>
                {
                    Console.WriteLine(msg);
                };
                Console.ReadLine();
                serv.Shutdown();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Server couldn't start: " + e.Message);
                Console.ReadLine();
            }
        }
    }
}

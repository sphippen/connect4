using System;
using System.Windows.Forms;

namespace Connect4GUI
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Connect4 game = new Connect4())
            {
                game.Run();
            }
        }
    }
#endif
}


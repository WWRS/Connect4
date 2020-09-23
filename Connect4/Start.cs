using System;

namespace Connect4
{
    internal static class Start
    {
        public static void Main ()
        {
            ConnectFour game = new ConnectFour();
            while (game.Loop()) {}
        
            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }
    }
}

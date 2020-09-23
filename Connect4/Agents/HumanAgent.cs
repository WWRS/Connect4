using System;

namespace Connect4.Agents
{
    public class HumanAgent : IAgent
    {
        public byte GetMove(ConnectFour game)
        {
            //game.PrintBoard();
            
            while (true)
            {
                // whose turn
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("It is ");
                if (game.NextColor == CheckerColor.Red)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Red's");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("Yellow's");
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" turn.");
                
                // ask for column
                Console.Write("Column to place checker in? ");
                try
                {
                    byte x = (byte) (Convert.ToByte(Console.ReadLine()) - 1);
                    if (x < 7 && game.ValidMove(x))
                    {
                        return x;
                    }
                }
                catch
                {
                    // Do nothing, so loop
                }
            }
        }
    }
}

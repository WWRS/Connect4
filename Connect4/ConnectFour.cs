using System;
using Connect4.Agents;

namespace Connect4
{
    public enum CheckerColor
    {
        Empty,
        Red,
        Yellow
    }

    public enum MoveResult
    {
        Continue,
        Win,
        Tie,
        Invalid
    }

    public class ConnectFour
    {
        private readonly IAgent _redPlayer;
        private readonly IAgent _yellowPlayer;

        public CheckerColor NextColor { get; private set; } = CheckerColor.Red;

        private readonly CheckerColor[,] _board;

        public byte LastX { get; private set; } = 255;
        public byte LastY { get; private set; } = 255;

        public ConnectFour(bool makeAgents = true)
        {
            // Initializes to all CheckerColor.empty since this is the default
            _board = new CheckerColor[7, 6];
            
            if (makeAgents)
            {
                _redPlayer = new HumanAgent();
                _yellowPlayer = new MCTSAgent();
            }
        }

        // Return: should we loop again
        public bool Loop()
        {
            IAgent nextPlayer = (NextColor == CheckerColor.Red ? _redPlayer : _yellowPlayer);
            PrintBoard();
            bool success = DropChecker(nextPlayer.GetMove(this));
            
            if (!success)  // invalid move
            {
                NextColor = NextColor == CheckerColor.Red ? CheckerColor.Yellow : CheckerColor.Red;
                WriteWinner(false);
                return false;
            }

            if (TestTie())
            {
                WriteWinner(true);
                return false;
            }

            if (TestWin())
            {
                WriteWinner(false);
                return false;
            }

            NextColor = (NextColor == CheckerColor.Red ? CheckerColor.Yellow : CheckerColor.Red);
            return true;
        }

        public void PrintBoard()
        {
            Console.WriteLine();
            Console.WriteLine("  1  2  3  4  5  6  7");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("+---------------------+");
            for (int y = 5; y >= 0; y--)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("|");
                for (int x = 0; x < 7; x++)
                {
                    switch (_board[x, y])
                    {
                        case CheckerColor.Empty:
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write("( )");
                            break;
                        case CheckerColor.Red:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("(R)");
                            break;
                        case CheckerColor.Yellow:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("(Y)");
                            break;
                    }
                }

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("|");
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("+---------------------+");
            Console.WriteLine();
        }

        // Returns: if checker was dropped
        private bool DropChecker(byte x)
        {
            for (byte y = 0; y < 6; y++)
            {
                if (_board[x, y] == CheckerColor.Empty)
                {
                    _board[x, y] = NextColor;
                    LastX = x;
                    LastY = y;

                    return true;
                }
            }

            return false;
        }

        // Returns: if prev move won
        private bool TestWin()
        {
            // Test vertical
            if (LastY >= 3)
            {
                bool win = true;
                for (int i = 1; i < 4; i++)
                {
                    if (_board[LastX, LastY - i] != NextColor)
                    {
                        win = false;
                        break;
                    }
                }

                if (win)
                    return true;
            }

            // Test horizontal
            int connectedHoriz = 1;
            for (int i = 1; i < 4; i++)
            {
                if (LastX + i < 7 && _board[LastX + i, LastY] == NextColor)
                    connectedHoriz++;
                else
                    break;
            }

            for (int i = 1; i < 4; i++)
            {
                if (LastX - i >= 0 && _board[LastX - i, LastY] == NextColor)
                    connectedHoriz++;
                else
                    break;
            }

            if (connectedHoriz >= 4)
                return true;

            // Diagonal /
            int connectedDiag1 = 1;
            for (int i = 1; i < 4; i++)
            {
                if (LastX + i < 7 && LastY + i < 6 && _board[LastX + i, LastY + i] == NextColor)
                    connectedDiag1++;
                else
                    break;
            }

            for (int i = 1; i < 4; i++)
            {
                if (LastX - i >= 0 && LastY - i >= 0 && _board[LastX - i, LastY - i] == NextColor)
                    connectedDiag1++;
                else
                    break;
            }

            if (connectedDiag1 >= 4)
                return true;

            // Diagonal \
            int connectedDiag2 = 1;
            for (int i = 1; i < 4; i++)
            {
                if (LastX + i < 7 && LastY - i >= 0 && _board[LastX + i, LastY - i] == NextColor)
                    connectedDiag2++;
                else
                    break;
            }

            for (int i = 1; i < 4; i++)
            {
                if (LastX - i >= 0 && LastY + i < 6 && _board[LastX - i, LastY + i] == NextColor)
                    connectedDiag2++;
                else
                    break;
            }

            if (connectedDiag2 >= 4)
                return true;

            // None found
            return false;
        }

        private void WriteWinner(bool tie)
        {
            PrintBoard();

            if (tie)
                Console.WriteLine("It's a tie!");
            else
            {
                if (NextColor == CheckerColor.Red)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Red");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("Yellow");
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" wins!");
            }
        }

        // Returns: if prev move tied the game
        private bool TestTie()
        {
            for (int x = 0; x < 7; x++)
                if (_board[x, 5] == CheckerColor.Empty)
                    return false;
            return true;
        }

        // Returns: the result of the simulated move
        // Changes the state of the board!
        public MoveResult Simulate(byte col)
        {
            if (!DropChecker(col))
                return MoveResult.Invalid;

            if (TestWin())
                return MoveResult.Win;

            if (TestTie())
                return MoveResult.Tie;

            NextColor = (NextColor == CheckerColor.Red ? CheckerColor.Yellow : CheckerColor.Red);
            return MoveResult.Continue;
        }

        // Returns: reference to self, after resetting the board state
        public ConnectFour Clear()
        {
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    _board[i, j] = CheckerColor.Empty;
                }
            }

            LastX = 255;
            LastY = 255;

            return this;
        }

        // Returns: is the given move valid
        public bool ValidMove(byte move)
        {
            return _board[move, 5] == CheckerColor.Empty;
        }
    }
}

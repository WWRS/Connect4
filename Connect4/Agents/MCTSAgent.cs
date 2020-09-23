using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace Connect4.Agents
{
    public class MCTSAgent : IAgent
    {
        // Shared resources. Prevents Random syncing and saves on allocations
        public static readonly Random Rand = new Random();
        public static readonly byte[] Byte42 = new byte[42];
        public static readonly ConnectFour ConnectFour = new ConnectFour(false);

        private MCTSNode _root;
        private bool _running = false;

        public MCTSAgent()
        {
            _root = new MCTSNode(null, CheckerColor.Red);
        }

        public byte GetMove(ConnectFour game)
        {
            if (game.LastX != 255)
            {
                _root = _root.Node(game.LastX);
            }

            _running = true;
            Task.Run(() => { RunSearch(); });
            Thread.Sleep(1000);
            _running = false;

            byte move = _root.MostVisited();
            //Console.WriteLine("Next 5 plays: " + _root.Plays(5));

            _root = _root.Node(move);
            _root.RemoveParent();

            Console.WriteLine(move);
            
            return move;
        }

        private void RunSearch()
        {
            while (_running)
            {
                _root.Traverse();
            }
        }
    }

    internal class MCTSNode
    {
        private enum ColorResult
        {
            RedWins,
            YellowWins,
            Tie
        }
        
        // Node stuff
        private readonly MoveNode _moveList;
        private MCTSNode _parent;
        
        // MCTS stuff
        private int _visits = 0;
        private float _redScore = 0;
        private readonly MCTSNode[] _nodes = new MCTSNode[8];
        // MCTS exploration param
        private const float C = 1.141f;
        
        // Game stuff
        private readonly CheckerColor _nextMover;
        private CheckerColor PrevMover => _nextMover == CheckerColor.Red ? CheckerColor.Yellow : CheckerColor.Red;
        private CheckerColor NextNextMover => PrevMover;
        
        private readonly MoveResult _result = MoveResult.Continue;

        public MCTSNode(MoveNode moveList, CheckerColor nextMover, MCTSNode parent = null, byte move = 255)
        {
            _nextMover = nextMover;
            _parent = parent;

            // First move, so no prior
            if (move == 255)
            {
                return;
            }

            // Everything below is to set _result
            ConnectFour tester = MCTSAgent.ConnectFour.Clear();
            _moveList = new MoveNode(move, moveList);
                
            MoveNode curr = _moveList;
            byte[] moves = MCTSAgent.Byte42;
            byte moveLength = 0;
            for (; moveLength < 42; moveLength++)
            {
                moves[moveLength] = curr.Move;
                curr = curr.Next;
                if (curr == null)
                {
                    break;
                }
            }

            // moveLength is actually one short after the above
            for (int i = moveLength; i > 0; i--)
            {
                tester.Simulate(moves[i]);
            }
            _result = tester.Simulate(moves[0]);
        }

        public void Traverse()
        {
            if (_result != MoveResult.Continue)
            {
                ColorResult cr;
                switch (_result)
                {
                    case MoveResult.Win:
                        cr = (PrevMover == CheckerColor.Red ? ColorResult.RedWins : ColorResult.YellowWins);
                        break;
                    case MoveResult.Tie:
                        cr = ColorResult.Tie;
                        break;
                    default:  // loss or invalid
                        cr = (PrevMover == CheckerColor.Red ? ColorResult.YellowWins : ColorResult.RedWins);
                        break;
                }

                Backpropagate(cr);
                return;
            }

            // Choose
            byte[] choices = MCTSAgent.Byte42;
            byte choicesCount = 0;
            float bestUCT = 0;

            for (byte i = 0; i < 7; i++)
            {
                // Expand
                if (_nodes[i] == null)
                {
                    choicesCount = 0;
                    for (byte j = i; j < 7; j++)
                    {
                        if (_nodes[j] == null)
                        {
                            choices[choicesCount] = j;
                            choicesCount++;
                        }
                    }

                    byte choice = choices[MCTSAgent.Rand.Next(choicesCount)];
                    _nodes[choice] = new MCTSNode(_moveList, NextNextMover, this, choice);
                    _nodes[choice].Traverse();
                    return;
                }

                // Traverse if expanded
                float iScore = _nextMover == CheckerColor.Red
                    ? _nodes[i]._redScore
                    : _nodes[i]._visits - _nodes[i]._redScore;
                float UCT = iScore / _nodes[i]._visits + C * (float) Math.Sqrt(Math.Log(_visits) / _nodes[i]._visits);

                if (Math.Abs(UCT - bestUCT) < 0.0000001f)
                {
                    choices[choicesCount] = i;
                    choicesCount++;
                }
                else if (UCT > bestUCT)
                {
                    choices[0] = i;
                    choicesCount = 1;
                    bestUCT = UCT;
                }
            }

            // Traverse chosen
            byte choose = choices[MCTSAgent.Rand.Next(choicesCount)];
            _nodes[choose].Traverse();
        }

        public byte MostVisited()
        {
            byte move = 4;
            float mostVisits = 0;

            if (_nodes == null)
            {
                Console.WriteLine(_result);
                return 0;
            }

            for (byte i = 0; i < 7; i++)
            {
                //Console.WriteLine(_nodes[i]?._redScore + "/" + _nodes[i]?._visits);
                if (mostVisits < _nodes[i]?._visits)
                {
                    move = i;
                    mostVisits = _nodes[i]._visits;
                }
            }

            return move;
        }

        public MCTSNode Node(byte move)
        {
            if (_nodes?[move] == null)
            {
                Console.WriteLine("node not found");
                return new MCTSNode(_moveList, NextNextMover, null, move);
            }
            else
            {
                return _nodes[move];
            }
        }

        private void Backpropagate(ColorResult cr)
        {
            _visits++;
            if (cr == ColorResult.RedWins)
            {
                _redScore++;
            }
            else if (cr == ColorResult.Tie)
            {
                _redScore += 0.5f;
            }

            _parent?.Backpropagate(cr);
        }

        // For GC
        public void RemoveParent()
        {
            _parent = null;
        }

        public string PredictPlays(int depth)
        {
            if (depth == 0 || _nodes == null)
            {
                return "\n";
            }

            byte move = 255;
            int mostVisits = 0;
            for (byte i = 0; i < 7; i++)
            {
                if (mostVisits < _nodes[i]?._visits)
                {
                    move = i;
                    mostVisits = _nodes[i]._visits;
                }
            }

            return _nextMover + ":" + (move + 1) + " " + _nodes[move].PredictPlays(depth - 1);
        }

        internal class MoveNode
        {
            public readonly byte Move;
            public readonly MoveNode Next;

            public MoveNode(byte move, MoveNode next)
            {
                Move = move;
                Next = next;
            }
        }
    }
}

using Include;
using Include.Util;
using Priority_Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Tangible {
    enum ChessPiece : byte {
        None = 0,
        BPawn = 1,
        WPawn = 2,
        BRook = 3,
        WRook = 4,
        BKnight = 5,
        WKnight = 6,
        BBishop = 7,
        WBishop = 8,
        BKing = 9,
        WKing = 10,
        BQueen = 11,
        WQueen = 12
    }

    // this is a single piece move
    class Move {
        public ChessPiece Piece { get; set; }
        public (int, int) Start { get; set; }
        public (int, int) End { get; set; }
        public List<(int, int)> Path { get; set; } = new List<(int, int)>();
    }

    class PathNode : FastPriorityQueueNode {
        public (int, int) Position { get; set; }
        public (int, int)? BoardPos { get; set; } = null;
        public ChessPiece Piece { get; set; } = ChessPiece.None;
        public PathNode Parent { get; set; } = null;
        public List<PathNode> Adjacent { get; set; } = new List<PathNode>();

        public double GetDistance(PathNode node) {
            return Math.Sqrt(Math.Pow(node.Position.Item1 - Position.Item1, 2) + Math.Pow(node.Position.Item2 - Position.Item2, 2));
        }

        public bool IsAdjacent(PathNode node) {
            return Math.Abs(node.Position.Item1 - Position.Item1) + Math.Abs(node.Position.Item2 - Position.Item2) == 1;
        }

        public void AddIfAdjacent(PathNode node) {
            if (IsAdjacent(node)) {
                Adjacent.Add(node);
            }
        }
    }

    class StateDiff {
        public ChessPiece From { get; set; }
        public ChessPiece To { get; set; }
        public int Index { get; set; }

        // prioritize the piece that is there at the end
        public ChessPiece Piece { get { return From == ChessPiece.None ? To : From; } }

        // if the from is none, the piece has moved somewhere else
        public bool IsGone { get { return To == ChessPiece.None; } }

        // if the piece is move, then it spawned out of nowhere
        public bool IsMove { get { return From == ChessPiece.None; } }

        // if is take, then the piece was replaced by another piece
        public bool IsTake { get { return To != ChessPiece.None && From != ChessPiece.None; } }

        // 2 valid diffs added make a move, otherwise a null
        public static Move operator *(StateDiff a, StateDiff b) {

            if (a.Piece != b.Piece) {
                Log.E($"Move+ : incompatable operands");
                return null;
            }

            if (b == a) {
                Log.E($"Move+ : operands cannot be the same");
                return null;
            }

            if (!b.IsMove || !b.IsTake) {
                Log.E($"Move+ : second operand must be IsMove or IsTake");
                return null;
            }

            if (!a.IsGone) {
                Log.E($"Move+ : first operand must be IsGone");
                return null;
            }

            if (!BoardState.TryGetPathPositionFromBoardIndex(a.Index, out var start)) {
                Log.E($"Move+ : first operand's index invalid");
                return null;
            }

            if (!BoardState.TryGetPathPositionFromBoardIndex(b.Index, out var end)) {
                Log.E($"Move+ : second operand's index invalid");
                return null;
            }

            return new Move() { Start = start, End = end, Piece = a.Piece };
        }
    }

    // board state
    class BoardState {
        public static readonly int WIDTH = 8;
        public static readonly int HEIGHT = 8;
        public static readonly int SIZE = WIDTH * HEIGHT;

        public static readonly int PATHWIDTH = WIDTH * 2 + 1;
        public static readonly int PATHHEIGHT = HEIGHT * 2 + 1;
        public static readonly int PATHSIZE = PATHWIDTH * PATHHEIGHT;

        // coords are in path space
        public static List<(int, int)> DISCARDZONE = new List<(int, int)>() {
            (-1, 4),
            (-1, 5),
            (-1, 6),
            (-1, 7),
            (-1, 8),
            (-1, 9),
            (-1, 10),
            (-1, 11),
            (-1, 12),
            (-2, 4),
            (-2, 5),
            (-2, 6),
            (-2, 7),
            (-2, 8),
            (-2, 9),
            (-2, 10),
            (-2, 11),
            (-2, 12)
        };

        // using this as the real discard zone until the robot crashes itself
        public static List<(int, int)> SAFEDISCARD = new List<(int, int)>() {
            (-2, 5),
            (-2, 6),
            (-2, 7),
            (-2, 8),
            (-2, 9),
            (-2, 10),
            (-2, 11)
        };

        public static readonly int PRINTPADDING = 1;

        ChessPiece[] board = new ChessPiece[SIZE];
        List<PathNode> navMesh = new List<PathNode>();

        private static readonly Dictionary<ChessPiece, char> symbols = new Dictionary<ChessPiece, char>() {
            { ChessPiece.None    , ' ' },
            { ChessPiece.BPawn   , 'P' },
            { ChessPiece.WPawn   , 'p' },
            { ChessPiece.BRook   , 'R' },
            { ChessPiece.WRook   , 'r' },
            { ChessPiece.BKnight , 'N' },
            { ChessPiece.WKnight , 'n' },
            { ChessPiece.BBishop , 'B' },
            { ChessPiece.WBishop , 'b' },
            { ChessPiece.BKing   , 'K' },
            { ChessPiece.WKing   , 'k' },
            { ChessPiece.BQueen  , 'Q' },
            { ChessPiece.WQueen  , 'q' }
        };

        // resets the board state
        // 0 0 is the top left
        public void Reset() {
            for (int i = 0; i < SIZE; i++) {
                switch (i) {
                    case 0:
                        board[i] = ChessPiece.BRook;
                        break;
                    case 1:
                        board[i] = ChessPiece.BKnight;
                        break;
                    case 2:
                        board[i] = ChessPiece.BBishop;
                        break;
                    case 3:
                        board[i] = ChessPiece.BQueen;
                        break;
                    case 4:
                        board[i] = ChessPiece.BKing;
                        break;
                    case 5:
                        board[i] = ChessPiece.BBishop;
                        break;
                    case 6:
                        board[i] = ChessPiece.BKnight;
                        break;
                    case 7:
                        board[i] = ChessPiece.BRook;
                        break;
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                        board[i] = ChessPiece.BPawn;
                        break;
                    case 63:
                        board[i] = ChessPiece.WRook;
                        break;
                    case 62:
                        board[i] = ChessPiece.WKnight;
                        break;
                    case 61:
                        board[i] = ChessPiece.WBishop;
                        break;
                    case 60:
                        board[i] = ChessPiece.WKing;
                        break;
                    case 59:
                        board[i] = ChessPiece.WQueen;
                        break;
                    case 58:
                        board[i] = ChessPiece.WBishop;
                        break;
                    case 57:
                        board[i] = ChessPiece.WKnight;
                        break;
                    case 56:
                        board[i] = ChessPiece.WRook;
                        break;
                    case 55:
                    case 54:
                    case 53:
                    case 52:
                    case 51:
                    case 50:
                    case 49:
                    case 48:
                        board[i] = ChessPiece.WPawn;
                        break;
                    default:
                        board[i] = ChessPiece.None;
                        break;
                }
            }
        }

        // gets piece at location
        public ChessPiece GetPiece((int, int) position) {
            return board[position.Item1 + position.Item2 * WIDTH];
        }

        // updates the board state directly, no other processing
        public void ForceUpdate(ChessPiece[] absoluteStateOfTheBoard) {
            if (absoluteStateOfTheBoard.Length != board.Length) {
                Log.E($"ForceUpdate: Size mismatch src({absoluteStateOfTheBoard.Length}) : dst({board.Length}");
                return;
            }
            Array.Copy(absoluteStateOfTheBoard, board, SIZE);
        }

        public static bool TryGetPathPositionFromBoardIndex((int, int) boardPosition, out (int, int) pathPosition) {
            int index = boardPosition.Item1 * 2 + 1 + boardPosition.Item2 * PATHWIDTH;
            return TryGetPathPositionFromBoardIndex(index, out pathPosition);
        }

        // if out of range, will return false
        // otherwise, out is the translated position
        public static bool TryGetPathPositionFromBoardIndex(int index, out (int, int) position) {
            position = (0, 0);
            if (index >= 0 && index < SIZE) {
                position = (index % WIDTH, index / WIDTH);
                return true;
            } else {
                return false;
            }
        }

        public (int, int) GetClosestDiscardPoint((int, int) start) {
            (int, int) closest = (0, 0);
            double min = float.MaxValue;
            foreach (var point in SAFEDISCARD) {
                double hype = Math.Pow(point.Item1 - start.Item1, 2) + Math.Pow(point.Item2 - start.Item2, 2);
                if (hype < min) {
                    closest = point;
                    min = hype;
                }
            }
            return closest;
        }

        // gets the difference between the newState and current board state as a list of moves to be executed
        // this doesn't handle every single possible board state a to board state b, but shoudl handle all legal moves
        // and many illegal ones
        public List<Move> Diff(ChessPiece[] newState) {
            var moves = new List<Move>();
            var diffs = new List<StateDiff>();

            for (int i = 0; i < SIZE; i++) {
                if (newState[i] != board[i]) {
                    diffs.Add(new StateDiff() { From = board[i], To = newState[i], Index = i });
                }
            }

            var gonePieces = diffs.Where(x => x.IsGone).ToList(); // pieces that moved somewhere else, replaced with empty
            var movePieces = diffs.Where(x => x.IsMove).ToList(); // pieces that moved to a location from somewhere else, empty replaced by a piece
            var takePieces = diffs.Where(x => x.IsTake).ToList(); // pieces which have displaced other pieces, pieces replaced by another piece

            // remove the pieces that have been taken
            foreach (var take in takePieces) {
                if (TryGetPathPositionFromBoardIndex(take.Index, out var position)) {
                    moves.Add(new Move() { Piece = take.From, Start = position, End = GetClosestDiscardPoint(position) });
                } else {
                    Log.E($"Diff: take index{take.Index} is not valid");
                }
            }

            // move pieces to positions which took pieces
            foreach (var take in takePieces) {
                var origin = gonePieces.First(x => x.Piece == take.To);
                if (origin == null) {
                    TryGetPathPositionFromBoardIndex(take.Index, out var position);
                    Log.E($"Diff: could not find match for {take.Piece} moving to {position}");
                    continue;
                }
                gonePieces.Remove(origin);
                moves.Add(take * origin);
            }

            //move pieces that just moved
            foreach (var move in movePieces) {
                var origin = gonePieces.First(x => x.Piece == move.Piece);
                if (origin == null) {
                    TryGetPathPositionFromBoardIndex(move.Index, out var position);
                    Log.E($"Diff: could not find match for {move.Piece} moving to {position}");
                    continue;
                }
                gonePieces.Remove(origin);
                moves.Add(move * origin);
            }


            if (gonePieces.Count > 0) Log.E($"Diff: {gonePieces.Count} pieces remain unresolved");

            foreach (var gone in gonePieces) {
                Log.E($"Diff: {gone.Piece} at {gone.Index}");
            }

            //path find!

            return moves;
        }

        // finds path for move action
        public Move Resolve(Move move) {
            FastPriorityQueue<PathNode> queue = new FastPriorityQueue<PathNode>(PATHSIZE + DISCARDZONE.Count);
            var start = navMesh.First(x => x.BoardPos == move.Start);
            var end = navMesh.First(x => x.BoardPos == move.End);

            queue.Enqueue(start, (float)start.GetDistance(end));

            while (queue.Count != 0) {
                var node = queue.Dequeue();
                foreach (var adj in start.Adjacent) {
                    adj.Parent = start;
                    if (adj != end) {
                        if (adj.Piece == ChessPiece.None)
                            queue.Enqueue(adj, (float)adj.GetDistance(end));
                    } else {
                        break;
                    }
                }
                if (end.Parent != null) break;
            }

            List<PathNode> nodes = new List<PathNode>();
            while (end != start) {
                nodes.Add(end);
                end = end.Parent;
                if (end == null) return null;
            }

            for (int i = nodes.Count - 1; i >= 0; i++) {
                move.Path.Add(nodes[i].Position);
            }
            return move;
        }

        public void PrintState() {
            PrintState(board);
        }

        public static void PrintState(ChessPiece[] boardState) {
            if (boardState.Length != SIZE) {
                Log.E("PrintState: boardState length does not match");
                return;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < SIZE; i++) {
                sb.Append(symbols[boardState[i]]);
                // chuck a new line in there
                if (i % WIDTH == WIDTH - 1) {
                    sb.Append(Environment.NewLine);
                }
            }
            Log.I(sb.ToString());
        }

        public bool IsValidBoardPos((int, int) position) {
            return position.Item1 >= 0 && position.Item2 >= 0 && position.Item1 < WIDTH && position.Item2 < HEIGHT;
        }

        public bool IsValidPathPos((int, int) position) {
            if (DISCARDZONE.Contains(position)) return true;
            return position.Item1 >= 0 && position.Item2 >= 0 && position.Item1 < PATHWIDTH && position.Item2 < PATHHEIGHT;
        }

        private string Pad(object str, char pre = ' ', char post = ' ') {
            return Pad(str.ToString(), pre, post);
        }

        private string Pad(string str, char pre = ' ', char post = ' ') {
            return str.PadLeft(((PRINTPADDING - str.Length) / 2)
                            + str.Length, pre)
                   .PadRight(PRINTPADDING, post);
        }

        // just clears all pathfinding flags in the navmesh and updates to latest relevant model
        public void ResetNavMesh() {
            foreach (var node in navMesh) {
                if (node.BoardPos != null) {
                    node.Piece = GetPiece(((int, int))node.BoardPos);
                }
            }
        }

        // wipes and rebuilds navmesh, this calls ResetNavMesh on its own, no need to call it manually
        public void RebuildNavMesh() {
            navMesh = new List<PathNode>();
            for (int i = 0; i < PATHSIZE; i++) {
                var node = new PathNode() { Position = (i % PATHWIDTH, i / PATHWIDTH) };
                if ((i / PATHWIDTH) % 2 == 1 && (i % PATHWIDTH) % 2 == 1) {
                    node.BoardPos = (i % PATHWIDTH / 2, i / PATHWIDTH / 2);
                }
                navMesh.Add(node);
            }

            foreach (var point in DISCARDZONE) {
                navMesh.Add(new PathNode() { Position = point });
            }

            foreach (var point in navMesh) {
                foreach (var other in navMesh) {
                    point.AddIfAdjacent(other);
                }
            }

            ResetNavMesh();
        }

        // checks to see if all points are only 1 apart from eachother
        public bool PathValid(List<(int, int)> path) {
            if (path == null || path.Count == 0) {
                return true;
            }

            (int, int) from = (0, 0);
            for (int i = 0; i < path.Count; i++) {
                var point = path[i];
                if (!IsValidPathPos(point)) {
                    Log.E($"PathValid: point out of bounds at pos:{i} {{{point}}}");
                    return false;
                }

                if (i != 0) {
                    int diff = Math.Abs(point.Item1 - from.Item1) + Math.Abs(point.Item2 - from.Item2);
                    if (diff > 1 || diff < 1) {
                        Log.E($"PathValid: invalid stepsize at pos:{i}, {{{from}, {point}}}");
                        return false;
                    }
                }

                if (i != path.Count - 1) {
                    if (from == path[i + 1]) {
                        Log.E($"PathValid: uturn detected at pos:{i}, {{{from}, {point}, {path[i + 1]}}}");
                        return false;
                    }
                }
                from = point;
            }
            return true;
        }

        public void PrintPath(List<(int, int)> path) {
            if (!PathValid(path)) {
                Log.E("PrintPath: Path is invalid");
                return;
            }

            StringBuilder sb = new StringBuilder(Environment.NewLine);
            sb.Append("╔");
            for (int i = 0; i < PATHWIDTH * PRINTPADDING; i++) sb.Append("═");
            sb.Append("╗");
            sb.Append(Environment.NewLine);

            for (int i = 0; i < PATHSIZE; i++) {
                (int, int) boardPos = (-1, -1); // this is the 8 x 8 coordinate of the map
                (int, int) pathPos = (i % PATHWIDTH, i / PATHWIDTH);  // this is the 17 x 17 coordinate for where the piece can go

                if (i % PATHWIDTH == 0) sb.Append("║");

                if ((i / PATHWIDTH) % 2 == 1 && (i % PATHWIDTH) % 2 == 1) {
                    boardPos = (i % PATHWIDTH / 2, i / PATHWIDTH / 2);
                }

                if (IsValidBoardPos(boardPos) && GetPiece(boardPos) != ChessPiece.None) { // if it's a chess piece
                    sb.Append(Pad(symbols[GetPiece(boardPos)]));
                    //sb.Append(Pad(i));
                } else if (path?.Contains(pathPos) ?? false) { // if contains path value
                    char symbol = ' ';

                    if (path.Contains(pathPos)) {
                        int pos = path.IndexOf(pathPos);
                        if (pos == path.Count - 1) {
                            symbol = 'E';
                        } else if (pos == 0) {
                            symbol = 'S';
                        } else {
                            (int, int) from = path[pos - 1];
                            (int, int) cur = path[pos];
                            (int, int) to = path[pos + 1];
                            if (from.Item2 == to.Item2) { // horizontal, no change in y
                                symbol = '─';
                            } else if (from.Item1 == to.Item1) { // vertical, no change in x
                                symbol = '│';
                            } else { // oh god the corners
                                bool uturn = from.Item1 == to.Item1 && from.Item2 == to.Item2;
                                bool left = cur.Item1 - from.Item1 > 0 || to.Item1 - cur.Item1 < 0;
                                bool up = cur.Item2 - from.Item2 > 0 || to.Item2 - cur.Item2 < 0;

                                if (uturn) {
                                    // these aren't allowed btw
                                    symbol = '+';
                                } else if (left && up) {
                                    symbol = '┘';
                                } else if (left && !up) {
                                    symbol = '┐';
                                } else if (!left && up) {
                                    symbol = '└';
                                } else if (!left && !up) {
                                    symbol = '┌';
                                }
                            }
                        }
                    }

                    sb.Append(Pad(symbol));
                } else { // otherwise it's empty
                    sb.Append(Pad(" "));
                }

                // last char in the row
                if (i % PATHWIDTH == PATHWIDTH - 1) {
                    sb.Append("║");
                    sb.Append(Environment.NewLine);
                } else {
                    sb.Append("");
                }
            }

            sb.Append("╚");
            for (int i = 0; i < PATHWIDTH * PRINTPADDING; i++) sb.Append("═");
            sb.Append("╝");

            Log.I(sb.ToString());
        }
    }

    class Program {
        static bool run = false;

        static void Main(string[] args) {
            Log.SetFileName("tangible");
            Log.DoLogLevel(Log.LogLevel.info);
            Log.DoLogLevel(Log.LogLevel.verb);
            Log.DoLogLevel(Log.LogLevel.warn);
            Log.DoLogLevel(Log.LogLevel.errr);
            Log.DoLogLevel(Log.LogLevel.crit);

            // just blast the coordinates through UDP
            // listen for coordinates through UDP
            UDPSocket socket = new UDPSocket();
            socket.Begin("192.168.1.42", 13579);

            // so our thread runs
            run = true;

            // on piece move, scan for where pieces are
            socket.OnReceiveListener += Write;

            // scan update boardstate when we get a message
            Thread write = new Thread(new ThreadStart(DeQueue));
            write.Start();

            // scan update boardstate when we get a message
            Thread read = new Thread(new ThreadStart(Read));
            read.Start();

            //test the board state
            BoardState board = new BoardState();
            board.Reset();

            var pathTest = new List<(int, int)>() { (16, 15),
             (15, 15),
             (14, 15),
             (13, 15),
             (13, 14),
             (13, 13),
             (14, 13),
             (14, 12),
             (14, 11),
             (14, 10),
             (14, 9),
             (13, 9),
             (13, 10),
             (12, 10),
             (12, 11),
             (11, 11),
             (10, 11),
             (10, 12),
             (10, 13),
             (10, 14),
             (11, 14),
             (11, 15),
             (11, 16),
             (10, 16),
             (9, 16),
             (8, 16),
             (8, 15),
             (8, 14),
             (8, 13),
             (8, 12),
             (8, 11),
             (8, 10),
             (8, 9),
             (8, 8),
             (9, 8),
             (10, 8),
             (11, 8),
             (12, 8),
             (13, 8),
             (14, 8),
             };
            board.PrintPath(pathTest);

            // on receive coordinates, move piece
            Log.I("Tangible: Press any key to end");
            Console.ReadKey();

            run = false;
            read.Join();
            write.Join();
            socket.End();

            Log.I("Tangible: Done");
            Console.ReadKey();
        }

       static ConcurrentQueue<byte[]> dataQueue = new ConcurrentQueue<byte[]>();
        static void Write(IPEndPoint endpoint, byte[] data) { // write virtual board state to real board
            Log.D("Got new boardstate from device");
            dataQueue.Enqueue(data);
            
            // find difference between two board states
            // remove pieces first
            // while pieces arent where they should be
            // foreach piece
            // if spot is free, move piece to spot
            // wait for input
        }

        static void DeQueue() {
            while(dataQueue.TryDequeue(out var data)) {
                ChessPiece[] state = Array.ConvertAll(data, c => (ChessPiece)c);
                BoardState.PrintState(state);
            }
        }

        static void Read() { // reads real board state and reports to phone
            while (run) {
                //Log.D("Reading new boardstate from board");
                // run opencv
                // read map
                // send map
                Thread.Sleep(10);
            }
        }
    }
}

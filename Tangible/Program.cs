using Include;
using Include.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace Tangible {
    enum ChessPiece {
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

    class StateDiff {
        public ChessPiece From { get; set; }
        public ChessPiece To { get; set; }
        public int Index { get; set; }

        public bool IsGone { get { return To == ChessPiece.None; } }
        public bool IsAppear { get { return From == ChessPiece.None; } }

        public static Move operator +(StateDiff a, StateDiff b) {
            return new Move() { };
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

        public static readonly int PRINTPADDING = 1;

        ChessPiece[] board = new ChessPiece[SIZE];
        List<(int, int)> path = new List<(int, int)>();

        Dictionary<ChessPiece, char> symbols = new Dictionary<ChessPiece, char>() {
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

        // gets the difference between the 2 boards as a list of moves to be executed
        public List<Move> Diff(ChessPiece[] absoluteStateOfTheBoard) {
            var moves = new List<Move>();
            var diffs = new List<StateDiff>();

            for (int i = 0; i < SIZE; i++) {
                if (absoluteStateOfTheBoard[i] != board[i]) {
                    diffs.Add(new StateDiff() { From = board[i], To = absoluteStateOfTheBoard[i], Index = i });
                }
            }

            List<ChessPiece> dissappearedPieces = new List<ChessPiece>();
            List<ChessPiece> appearedPieces = new List<ChessPiece>();

            foreach (var diff in diffs) {
                if (diff.From == ChessPiece.None) {

                }

                if (diff.From == ChessPiece.None) {

                }
            }

            return moves;
        }

        public void PrintState() {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < SIZE; i++) {
                sb.Append(symbols[board[i]]);
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
                    char pre = 'x';
                    char post = 'y';

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

                    sb.Append(Pad(symbol, pre, post));
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
            socket.End();

            Log.I("Tangible: Done");
            Console.ReadKey();
        }

        static void Write(IPEndPoint endpoint, byte[] data) { // write virtual board state to real board
            Log.D("Got new boardstate from device");
            // find difference between two board states
            // remove pieces first
            // while pieces arent where they should be
            // foreach piece
            // if spot is free, move piece to spot
            // wait for input
            Thread.Sleep(10);
        }

        static void Read() { // reads real board state and reports to phone
            while (run) {
                Log.D("Reading new boardstate from board");
                // run opencv
                // read map
                // send map
                Thread.Sleep(10);
            }
        }
    }
}

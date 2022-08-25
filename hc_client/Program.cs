using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using HexC;
// using Newtonsoft.Json;
using System.IO;

namespace HexCClient
{


    class hcclient
    {
        public class Spot
        {
            public int Q { get; set; }
            public int R { get; set; }
            public string Color { get; set; }
            public string Piece { get; set; }

        }

        public class FromString
        {
            public static HexC.PiecesEnum PieceFromString(string piece)
            {
                switch (piece)
                {
                    case "King": return PiecesEnum.King;
                    case "Queen": return PiecesEnum.Queen;
                    case "Castle": return PiecesEnum.Castle;
                    case "Elephant": return PiecesEnum.Elephant;
                    case "Pawn": return PiecesEnum.Pawn;
                }
                System.Diagnostics.Debug.Assert(false);
                return PiecesEnum.King;
            }

            public static HexC.ColorsEnum ColorFromString(string color)
            {
                switch (color)
                {
                    case "Black": return ColorsEnum.Black;
                    case "Tan": return ColorsEnum.Tan;
                    case "White": return ColorsEnum.White;
                }
                System.Diagnostics.Debug.Assert(false);
                return ColorsEnum.Black;
            }
        }

        protected static BoardLocation ShiftedSpot(BoardLocation from, int q, int r)
        {
            if (null == from)
                from = new BoardLocation(0, 0);
            BoardLocation delta = new BoardLocation(q, r);
            BoardLocation newSpot = new BoardLocation(from, delta);
            if (newSpot.IsValidLocation())
                return newSpot;
            return from;
        }



        public class PrettyJsonPiece
        {
            public string Color { get; set; }
            public string Piece { get; set; }
        }

        public class PrettyJsonPlacedPiece : PrettyJsonPiece
        {
            public int Q { get; set; }
            public int R { get; set; }
        }

        public class PrettyJsonBoard : List<object>
        {
            public PrettyJsonBoard(List<PlacedPiece> lpPlaced, PieceList plSidelined)
            {
                foreach (PlacedPiece pp in lpPlaced)
                {
                    PrettyJsonPlacedPiece pjp = new PrettyJsonPlacedPiece();
                    pjp.Q = pp.Location.Q;
                    pjp.R = pp.Location.R;
                    pjp.Color = pp.Color.ToString();
                    pjp.Piece = pp.PieceType.ToString();
                    this.Add(pjp);
                }
                /*
                foreach (var p in plSidelined)
                {
                    PrettyJsonPlacedPiece pjp = new PrettyJsonPlacedPiece();
                    pjp.Color = p.Color.ToString();
                    pjp.Piece = p.PieceType.ToString();
                    pjp.Q = 99;
                    pjp.R = 99;

                    this.Add(pjp);
                }
                */
            }
        }

        protected static string m_whoseTurnItIs; // kooky quotes and stuff, just informational
        protected static bool m_showDebug = false;

        async static Task Main(string[] args)
        {
            BoardLocation cursor = new BoardLocation(0, 0);
            BoardLocation selected = null;

            DateTime lastServerRefresh = DateTime.UtcNow;
            DateTime lastKeyStrike = DateTime.UtcNow;

            Board b = null;
            Board turnStartBoard = null;

            using var client = new HttpClient();

            // if there is a 4th parameter, it's a local filename of a game we *create* on the server
            // ONLY SUCCESSFUL IF IT DOESN'T ALREADY EXIST

            if (args.GetLength(0) > 3)
            {
                string contents = File.ReadAllText(args[3]);
                //                string json = JsonConvert.SerializeObject(contents, Formatting.Indented);
                var httpContent = new StringContent(contents);
                var createScenarioResult = await client.PostAsync($"http://{args[0]}/Board/CreateScenario?gameId={args[1]}&whoseTurn={args[2]}", httpContent);
            }

            while (true)
            {
                // Would you please tell me whose turn it is? Maybe later underline the trio above.
                var whoseTurn = await client.GetStringAsync($"http://{args[0]}/WhoseTurn?gameId={args[1]}");
                lastServerRefresh = DateTime.UtcNow;

                bool fRedraw = false;

                // if a turn has changed, nuke any local changes and show new board.
                if (m_whoseTurnItIs != whoseTurn)
                {
                    fRedraw = true;
                    m_whoseTurnItIs = whoseTurn;

                    var jsonContent = await client.GetStringAsync($"http://{args[0]}/Board?gameId={args[1]}&color={args[2]}");

                    var pieces = System.Text.Json.JsonSerializer.Deserialize<List<Spot>>(jsonContent);

                    // with these pieces, render the board cleanly
                    b = new Board();
                    foreach (Spot s in pieces)
                    {
                        //                        if (s.Q == 99)
                        //                            b.Add(new Piece(FromString.PieceFromString(s.Piece), FromString.ColorFromString(s.Color)));
                        //                        else
                        b.Add(new PlacedPiece(FromString.PieceFromString(s.Piece), FromString.ColorFromString(s.Color), s.Q, s.R));
                    }

                    turnStartBoard = new Board(b); // clone this
                    lastServerRefresh = DateTime.UtcNow;
                }

                // loop as a user interface by showing the board, and commands for the newbies:
                while (true)
                {
                    if (fRedraw)
                    {
                        fRedraw = false;
                        Console.Clear();

                        // I need a clue for colors
                        SetPieceColor(ColorsEnum.Black); Console.Write("Black ");
                        SetPieceColor(ColorsEnum.Tan); Console.Write("Tan ");
                        SetPieceColor(ColorsEnum.White); Console.WriteLine("White");

                        Console.WriteLine(m_whoseTurnItIs.Replace("\"", "") + "'s turn.");
                        Console.WriteLine();

                        // Show captured pieces across the top, if any

                        if (b.SidelinedPieces.Count > 0)
                        {
                            ColorsEnum col = b.SidelinedPieces[0].Color;
                            for (int iPieceSlot = 0; iPieceSlot < b.SidelinedPieces.Count; iPieceSlot++)
                            {
                                SetPieceColor(b.SidelinedPieces[iPieceSlot].Color);
                                if (b.SidelinedPieces[iPieceSlot].Color != col)
                                {
                                    col = b.SidelinedPieces[iPieceSlot].Color;
                                    Console.WriteLine();
                                }
                                Console.Write(b.SidelinedPieces[iPieceSlot].ToChar());
                                Console.BackgroundColor = ConsoleColor.Black;
                            }
                        }
                        Console.WriteLine();
                        Console.WriteLine();

                        ShowTextBoard(b, cursor); // cursor could be null

                        Console.WriteLine();
                        Console.WriteLine(
                            "134679 : Move Cursor\r\n" + 
                            "5 : Select\r\n" + 
                            "0 : Reset to turn start\r\n" + 
                            "+ : Finish turn\r\n" + 
                            "- : Back one move (if possible)\r\n" + 
                            "/ : Show/hide debug details");

                        if (m_showDebug)
                        {
                            Console.WriteLine();
                            Console.WriteLine($"{args[0]} new-gamename {m_whoseTurnItIs.Replace("\"", "")} filename-of-the-following");
                            Console.WriteLine();
                            Console.Write("[");
                            bool fFirst = true;
                            foreach (var piece in b.PlacedPieces)
                            {
                                if (fFirst)
                                {
                                    fFirst = false;
                                }
                                else
                                    Console.WriteLine(",");
                                Console.Write("{");
                                Console.Write("\"Piece\":\"{0}\",\"Color\":\"{1}\",\"Q\":{2},\"R\":{3}",
                                    piece.PieceType.ToString(), piece.Color.ToString(), piece.Location.Q.ToString(), piece.Location.R.ToString());
                                Console.Write("}");

                            }
                            Console.Write("]");
                        }
                    }

                RollAgain:
                    // if no keys pressed, wait, and possibly refresh.
                    if (false == Console.KeyAvailable)
                    {
                        // if user is actively using interface, then never refresh.
                        TimeSpan sinceLastKeyStrike = DateTime.UtcNow - lastKeyStrike;
                        if (Convert.ToInt32(sinceLastKeyStrike.TotalSeconds) > 2)
                        {
                            // if haven't refreshed in five secs, go ahead.
                            TimeSpan diffy = DateTime.UtcNow - lastServerRefresh;
                            if (Convert.ToInt32(diffy.TotalSeconds) > 7)
                            {
                                break; ; // refreshes
                            }
                            System.Threading.Thread.Sleep(200);
                        }
                    }
                    if (false == Console.KeyAvailable)
                        goto RollAgain;

                    ConsoleKeyInfo cki;
                    cki = Console.ReadKey();
                    lastKeyStrike = DateTime.UtcNow;
                    fRedraw = true;

                    switch (cki.KeyChar.ToString().ToLower()[0])
                    {
                        case '+':
                            {
                                var whoseTurnBefore = await client.GetStringAsync($"http://{args[0]}/WhoseTurn?gameId={args[1]}");

                                // Listen up: If your turn STARTED with a piece of your color in the portal,
                                // and it's still there, then i will take it out for you.
                                HexC.ColorsEnum whose = FromString.ColorFromString(whoseTurnBefore.Replace("\"", ""));
                                var centerPiece = turnStartBoard.AnyoneThere(new BoardLocation(0, 0));
                                if (null != centerPiece)
                                    if (centerPiece.Color == whose)
                                        b.Remove(centerPiece);

                                PrettyJsonBoard pjb = new PrettyJsonBoard(b.PlacedPieces, b.SidelinedPieces);
                                //                                HttpContent content = new StringContent(JsonConvert.SerializeObject(pjb), System.Text.Encoding.UTF8, "application/json");
                                string content = JsonSerializer.Serialize(pjb);
                                HttpContent hc = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
                                await client.PostAsync($"http://{args[0]}/Moves?gameId={args[1]}", hc);
                                // ok, the event occurred... now ask whose turn it is! That's how we know if the turn was accepted!
                                m_whoseTurnItIs = await client.GetStringAsync($"http://{args[0]}/WhoseTurn?gameId={args[1]}");
                                if (whoseTurnBefore == m_whoseTurnItIs)
                                    goto case '0'; // failed. just reset the turn.
                                turnStartBoard = new Board(b);  // clone it! cuz it's a new turn!
                                break;
                            }
                        case '-':
                            Console.WriteLine("Permanently step back one move? +/-");
                            cki = Console.ReadKey();
                            if (cki.KeyChar.ToString().ToLower() == "+")
                            {
                                HttpContent content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
                                await client.PostAsync($"http://{args[0]}/Board/RollBackOne?gameId={args[1]}", content);
                            }
                            break;

                        case '0': b = new Board(turnStartBoard); break;
                        case '1': cursor = ShiftedSpot(cursor, -1, 1); break;
                        case '3': cursor = ShiftedSpot(cursor, 0, 1); break;
                        case '4': cursor = ShiftedSpot(cursor, -1, 0); break;
                        case '6': cursor = ShiftedSpot(cursor, 1, 0); break;
                        case '7': cursor = ShiftedSpot(cursor, 0, -1); break;
                        case '9': cursor = ShiftedSpot(cursor, 1, -1); break;
                        case '/': m_showDebug = (m_showDebug ? false : true); break;
                        case '5':
                            if (null == selected)
                            {
                                // none previously selected. just remember this spot as the selection.
                                if (null != b.AnyoneThere(cursor))
                                    selected = cursor;
                                break; // just easier to read?
                            }
                            else
                            {
                                PlacedPiece pieceAtOrigin = b.AnyoneThere(selected);
                                System.Diagnostics.Debug.Assert(pieceAtOrigin != null);

                                // if the piece in the center is my piece, i'll remove it later
                                bool fMoveOrLose = false;
                                PlacedPiece portalPiece = b.AnyoneThere(new BoardLocation(0, 0));
                                if (null != portalPiece)
                                {
                                    if (pieceAtOrigin.Color == portalPiece.Color)
                                        fMoveOrLose = true;
                                }

                                // is anyone at our destination spot?
                                PlacedPiece pieceAtDest = b.AnyoneThere(cursor);
                                if (null != pieceAtDest)
                                {
                                    // I can drop my piece on opponent, or Queen-King if valid. But postponing Queen-King magic for now.
                                    if (pieceAtDest.Color == pieceAtOrigin.Color)
                                        continue;

                                    b.Remove(pieceAtDest);
                                    b.SidelinedPieces.Add(pieceAtDest);
                                }

                                // If there's a piece at the destination, then
                                // is the portal empty? if so,
                                // do i have any sidelined pieces of this type to relocate to the portal?
                                if (null != pieceAtDest)
                                    if (null == b.AnyoneThere(new BoardLocation(0, 0)))
                                        if (b.SidelinedPieces.ContainsThePiece(pieceAtDest.PieceType, pieceAtOrigin.Color))
                                            b.Add(new PlacedPiece(pieceAtDest.PieceType, pieceAtOrigin.Color, 0, 0));

                                // NO MATTER WHAT, if we are moving a piece while our own color is in the portal, then it's gone.
                                if (fMoveOrLose)
                                    b.Remove(portalPiece);

                                // remove the original piece.
                                // and wtf is cursor?
                                b.Remove(pieceAtOrigin);
                                if (false == cursor.IsPortal)
                                {
                                    PlacedPiece ppNew = new PlacedPiece(pieceAtOrigin, cursor);
                                    b.Add(ppNew);
                                }

                                selected = null;
                            }
                            break;
                    }
                }
            }
        }


        protected static void SetPieceColor(ColorsEnum color)
        {
            switch (color)
            {
                case ColorsEnum.Black: Console.ForegroundColor = ConsoleColor.Cyan; break;
                case ColorsEnum.White: Console.ForegroundColor = ConsoleColor.White; break;
                case ColorsEnum.Tan: Console.ForegroundColor = ConsoleColor.Red; break;
            }
        }


        public static void ShowTextBoard(Board b, BoardLocation singleSpot = null, BoardLocationList highlights = null)
        {
            // Spit sequentially
            // the lines grow, then shrink
            // 6, then 7, then 8, then 9, up to 11, then back down.
            // FIRST:  how many in this line.
            // SECOND: first of the two coordinates, that increment across the line.
            // THIRD:  the other coordinate, that does not change across this line.
            int[,] Lines =  { { 6,  0, -5 },
                              { 7, -1, -4 },
                              { 8, -2, -3 },
                              { 9, -3, -2 },
                              {10, -4, -1 },
                              {11, -5,  0 },
                              {10, -5,  1 },
                              { 9, -5,  2 },
                              { 8, -5,  3 },
                              { 7, -5,  4 },
                              { 6, -5,  5 } };

            for (int iLine = 0; iLine < Lines.GetLength(0); iLine++)
            {
                Console.ResetColor();
                // First ident this many spaces: 11 minus how-many-this-line
                Console.Write("             ".Substring(0, 11 - Lines[iLine, 0]));

                // now increment through the line
                for (int iPos = 0; iPos < Lines[iLine, 0]; iPos++)
                {
                    BoardLocation spot = new BoardLocation(Lines[iLine, 1] + iPos, Lines[iLine, 2]);

                    if (highlights != null)
                    {
                        if (highlights.ContainsTheLocation(spot))
                        {
                            Console.BackgroundColor = ConsoleColor.DarkYellow;
                        }
                    }

                    if (spot.IsPortal)
                        Console.BackgroundColor = ConsoleColor.DarkCyan;

                    if (singleSpot != null)
                    {
                        if (BoardLocation.IsSameLocation(singleSpot, spot))
                            Console.BackgroundColor = ConsoleColor.DarkGreen;
                    }

                    PlacedPiece p = b.AnyoneThere(spot);
                    if (null == p)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        //Console.Write(spot.IsPortal ? "O" : "·");
                        Console.Write("·");
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ResetColor();
                        Console.Write(" ");
                    }
                    else
                    {
                        SetPieceColor(p.Color);
                        Console.Write(p.ToChar());
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.Write(" ");
                    }
                }
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}
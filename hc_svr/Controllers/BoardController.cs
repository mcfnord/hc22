using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json.Serialization;
using HexC;
using System.Net;
using Swashbuckle.Swagger.Annotations;

namespace hcsv2020.Controllers
{


    class VisualBoardStore
    {
        protected static HexC.ColorsEnum ColorEnumFromString(string color)
        {
            HexC.ColorsEnum col = HexC.ColorsEnum.White;
            switch (color)
            {
                case "black": col = HexC.ColorsEnum.Black; break;
                case "tan": col = HexC.ColorsEnum.Tan; break;
            }
            return col;
        }

        public static Board? GameBoard(string gameId)
        {
            if (false == m_allBoards.ContainsKey(gameId))
                return null;

            return m_allBoards[gameId];
        }

        public static bool GameOver(string gameId)
        {
            return (m_gamesOver.Contains(gameId) ? true : false);
        }

        public static bool ContainsGame(string gameId) { return m_allBoards.ContainsKey(gameId); } // m_allPieces.ContainsKey(id); }
        // public static Dictionary<string, string> GameBoard(string id) { if (m_allPieces.ContainsKey(id)) return m_allPieces[id]; return null; }
        //        public static Dictionary<string, string> TeamHues(string id, string color) { return m_allHues[id][ColorEnumFromString(color)]; }
        /*
        public static void AddBoard(string id, Dictionary<string, string> board)
        {
            m_allPieces.Add(id, board);
            
        m_allHues.Add(id, new Dictionary<HexC.ColorsEnum, Dictionary<string, string>>());
            m_allHues[id].Add(HexC.ColorsEnum.White, new Dictionary<string, string>());
            // what is this about???   m_allHues[id][HexC.ColorsEnum.White].Add("n0_n0", "9,9,9,1.0");
            m_allHues[id].Add(HexC.ColorsEnum.Black, new Dictionary<string, string>());
            m_allHues[id].Add(HexC.ColorsEnum.Tan, new Dictionary<string, string>());
        }
        */

        // There's just one board per game, but each game has different hue maps for each player.
        //        protected static Dictionary<string, Dictionary<string, string>> m_allPieces = new Dictionary<string, Dictionary<string, string>>();

        /*
        public static void UNIT_TEST_BACKDOOR_allBoardsYourTurn(string gameId, Board b, ColorsEnum col)
        {
            m_allBoards[gameId] = b;
            m_yourTurn[gameId] = col;
        }
        */

        protected static Dictionary<string, Board> m_allBoards = new Dictionary<string, Board>();
        protected static Dictionary<string, HexC.ColorsEnum> m_yourTurn = new Dictionary<string, HexC.ColorsEnum>();
        protected static List<string> m_gamesOver = new List<string>();

        protected static Dictionary<string, Board> m_allBoardsOnLastTurnEnd = new Dictionary<string, Board>();
        protected static Dictionary<string, HexC.ColorsEnum> m_yourTurnOnLastTurnEnd = new Dictionary<string, HexC.ColorsEnum>();


        public static HexC.ColorsEnum WhoseTurn(string gameId)
        {
            Debug.Assert(ContainsGame(gameId));
            return (HexC.ColorsEnum)m_yourTurn[gameId];
        }


        public static void RollbackOneMove(string gameId)
        {
            // Fastest implementation is just changing to rollback data
            // this will change the whole state to the previous state,
            // by starting with previous state default (no change)
            // and by overwriting that [1] state.

            m_allBoards[gameId] = m_allBoardsOnLastTurnEnd[gameId];
            m_yourTurn[gameId] = m_yourTurnOnLastTurnEnd[gameId];

            // We know this game is not over.
            if (m_gamesOver.Contains(gameId))
                m_gamesOver.Remove(gameId);
        }


        public static void ReportSuccessfulTurn(string gameId, HexC.ColorsEnum color, Board b)
        {
            // Save the current state in the lastMove objects
            // and update game state
            m_allBoardsOnLastTurnEnd[gameId] = m_allBoards[gameId];
            m_yourTurnOnLastTurnEnd[gameId] = m_yourTurn[gameId];

            // now set new game state
            m_allBoards[gameId] = b;
            switch (color)
            {
                case HexC.ColorsEnum.White:
                    m_yourTurn[gameId] = HexC.ColorsEnum.Tan;
                    break;

                case HexC.ColorsEnum.Tan:
                    m_yourTurn[gameId] = HexC.ColorsEnum.Black;
                    break;

                case HexC.ColorsEnum.Black:
                    m_yourTurn[gameId] = HexC.ColorsEnum.White;
                    break;

                default: Debug.Assert(false); break;
            }

            // If we detect that the new successful turn has placed the next player in a state where they cannot make any move,
            // then we set m_yourTurn[gameId] to null
            foreach (var piece in b.PlacedPiecesThisColor((ColorsEnum)m_yourTurn[gameId]))
            {
                var options = b.WhatCanICauseWithDoo(piece);
                if (options.Count > 0)
                    return;
            }

            // it's still someone's turn....
            // so i don't do this...
            // m_yourTurn[gameId] = null; // no options for any pieces = game over. so no next turn!
            // but instead,
            // i do this:
            m_gamesOver.Add(gameId);
            //m_gameLoser.Add(m_yourTurn[gameId]) ;

            // The player who can't move out of check is in checkmate. That player just lost.
            // To determine who won, we need to determine which other player put the player into checkmate FIRST.
            // Just back up one turn and determine if this same player is in checkmate again there.
            // if so, their neighbor to the left has won. if not, then their player to the right has created the  checkmate state.

            /*
            if Checkmated(m_yourTurn[gameId]))
            {
                m_yourTurn-m_yourTurn loses. who wins?
                    if Checkmated(boardJustBeforeThisBoard, m_yourTurn[gameId])
                        PlayerToLeftWOn
                       else
                    PlayerToRightWon
            */
            // was the player in a checkmate state at the start of the turn of the player to the right?
            foreach (var piece in m_allBoardsOnLastTurnEnd[gameId].PlacedPiecesThisColor(m_yourTurn[gameId]))
            {
                var options = b.WhatCanICauseWithDoo(piece);
                if (options.Count > 0)
                {
                    // Loser could have moved out of check when player-to-right's turn started.
                    Console.WriteLine("{0} won.", m_yourTurnOnLastTurnEnd[gameId]);
                    return;
                }

            }
            Console.WriteLine("The player who isn't {0} or {1} won.", m_yourTurn[gameId], m_yourTurnOnLastTurnEnd[gameId]);
        }

        public static void MakeCertainGameExists(string gameId)
        {
            var board = VisualBoardStore.GameBoard(gameId);
            if (null != board)
                return;
            HexC.Board b = new HexC.Board();

            b.Add(new PlacedPiece(PiecesEnum.Castle, ColorsEnum.Black, -1, -4));
            b.Add(new PlacedPiece(PiecesEnum.Castle, ColorsEnum.Black, -4, -1));
            b.Add(new PlacedPiece(PiecesEnum.Elephant, ColorsEnum.Black, -1, -3));
            b.Add(new PlacedPiece(PiecesEnum.Elephant, ColorsEnum.Black, -2, -2));
            b.Add(new PlacedPiece(PiecesEnum.Elephant, ColorsEnum.Black, -3, -1));
            b.Add(new PlacedPiece(PiecesEnum.Pawn, ColorsEnum.Black, -1, -2));
            b.Add(new PlacedPiece(PiecesEnum.Pawn, ColorsEnum.Black, -1, -1));
            b.Add(new PlacedPiece(PiecesEnum.Pawn, ColorsEnum.Black, -2, -1));
            b.Add(new PlacedPiece(PiecesEnum.Queen, ColorsEnum.Black, -3, -2));
            b.Add(new PlacedPiece(PiecesEnum.King, ColorsEnum.Black, -2, -3));

            b.Add(new PlacedPiece(PiecesEnum.Castle, ColorsEnum.Tan, -4, 5));
            b.Add(new PlacedPiece(PiecesEnum.Castle, ColorsEnum.Tan, -1, 5));
            b.Add(new PlacedPiece(PiecesEnum.Elephant, ColorsEnum.Tan, -3, 4));
            b.Add(new PlacedPiece(PiecesEnum.Elephant, ColorsEnum.Tan, -2, 4));
            b.Add(new PlacedPiece(PiecesEnum.Elephant, ColorsEnum.Tan, -1, 4));
            b.Add(new PlacedPiece(PiecesEnum.Pawn, ColorsEnum.Tan, -2, 3));
            b.Add(new PlacedPiece(PiecesEnum.Pawn, ColorsEnum.Tan, -1, 3));
            b.Add(new PlacedPiece(PiecesEnum.Pawn, ColorsEnum.Tan, -1, 2));
            b.Add(new PlacedPiece(PiecesEnum.King, ColorsEnum.Tan, -3, 5));
            b.Add(new PlacedPiece(PiecesEnum.Queen, ColorsEnum.Tan, -2, 5));

            b.Add(new PlacedPiece(PiecesEnum.Castle, ColorsEnum.White, 5, -4));
            b.Add(new PlacedPiece(PiecesEnum.Castle, ColorsEnum.White, 5, -1));
            b.Add(new PlacedPiece(PiecesEnum.Elephant, ColorsEnum.White, 4, -3));
            b.Add(new PlacedPiece(PiecesEnum.Elephant, ColorsEnum.White, 4, -2));
            b.Add(new PlacedPiece(PiecesEnum.Elephant, ColorsEnum.White, 4, -1));
            b.Add(new PlacedPiece(PiecesEnum.Pawn, ColorsEnum.White, 3, -2));
            b.Add(new PlacedPiece(PiecesEnum.Pawn, ColorsEnum.White, 3, -1));
            b.Add(new PlacedPiece(PiecesEnum.Pawn, ColorsEnum.White, 2, -1));
            b.Add(new PlacedPiece(PiecesEnum.King, ColorsEnum.White, 5, -3));
            b.Add(new PlacedPiece(PiecesEnum.Queen, ColorsEnum.White, 5, -2));
            m_allBoards.Add(gameId, b);
            m_allBoardsOnLastTurnEnd.Add(gameId, b);
            m_yourTurn.Add(gameId, ColorsEnum.Black); // black goes first  
            m_yourTurnOnLastTurnEnd.Add(gameId, ColorsEnum.Black); // black went first in the very first game, and in the game before that
        }


        public static void CreateGameScenario(string gameId, Board b, HexC.ColorsEnum col)
        {
            Debug.Assert(gameId.Length > 0);
            Debug.Assert(false == m_allBoards.ContainsKey(gameId));

            m_allBoards[gameId] = b;
            m_yourTurn[gameId] = col;
        }
    }

    /*
    public class GameHelper
    {
        public static void MakeCertainGameExists(string gameId)
        {
            if (VisualBoardStore.ContainsGame(gameId))
                return;

            // it exists but there aren't any pieces on the board today.
            m_yourTurn[gameId] = HexC.ColorsEnum.Black;

            Debug.Assert(false); // doesn't exist huh.


        }
    }
    */

    [Route("")]
    [ApiController]
    public class BoardController : ControllerBase
    {
        public class Spot
        {
            public int Q { get; set; }
            public int R { get; set; }
            public string Color { get; set; }
            public string Piece { get; set; }
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
                foreach (var p in plSidelined)
                {
                    PrettyJsonPlacedPiece pjp = new PrettyJsonPlacedPiece();
                    pjp.Color = p.Color.ToString();
                    pjp.Piece = p.PieceType.ToString();
                    pjp.Q = 99;
                    pjp.R = 99;

                    this.Add(pjp);
                }
            }
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
                return ColorsEnum.Black;  // why pick a failure color? an why pick black? 
            }
        }

        protected static bool BoardsMatch(Board b1, Board b2)
        {
            // Are there any Consequential differences?
            foreach (var pp in b1.PlacedPieces)
            {
                var samePP = b2.AnyoneThere(pp.Location);
                if (null == samePP) return false;
                if (samePP.PieceType != pp.PieceType) return false;
                if (samePP.Color != pp.Color) return false;
            }

            foreach (var pp in b2.PlacedPieces)
            {
                var samePP = b1.AnyoneThere(pp.Location);
                if (null == samePP) return false;
                if (samePP.PieceType != pp.PieceType) return false;
                if (samePP.Color != pp.Color) return false;
            }

            foreach (var p in b1.SidelinedPieces)
                if (false == b2.SidelinedPieces.ContainsThePiece(p.PieceType, p.Color))
                    return false;

            foreach (var p in b2.SidelinedPieces)
                if (false == b1.SidelinedPieces.ContainsThePiece(p.PieceType, p.Color))
                    return false;

            return true;
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

        public static void ShowTextBoard(Board b, BoardLocation? singleSpot = null, BoardLocationList? highlights = null)
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

                    if (singleSpot != null)
                    {
                        if (BoardLocation.IsSameLocation(singleSpot, spot))
                            Console.BackgroundColor = ConsoleColor.DarkRed;
                    }

                    PlacedPiece p = b.AnyoneThere(spot);
                    if (null == p)
                    {
                        if (spot.IsPortal)
                            Console.BackgroundColor = ConsoleColor.DarkGray;

                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write(spot.IsPortal ? "X" : "·");
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
            }

        }



        protected static bool CanFindOptionResultingInNewBoard(HexC.ColorsEnum col, Board bFrom, Board bTo)
        {

            Console.WriteLine();
            Console.WriteLine("Target Board:");
            ShowTextBoard(bTo);

            foreach (var piece in bFrom.PlacedPiecesThisColor(col))
            {
                var options = bFrom.WhatCanICauseWithDoo(piece);

                foreach (var option in options)
                {
                    Board bThisBoard = new Board(bFrom); // clone the original

                    foreach (var anEvent in option)
                    {
                        switch (anEvent.EventType)
                        {
                            case EventTypeEnum.Add:
                                bThisBoard.Add(anEvent.Regarding);
                                break;

                            case EventTypeEnum.Remove:
                                bThisBoard.Remove(anEvent.Regarding);
                                break;

                        }
                    }

                    // What are we lookin at?
                    ShowTextBoard(bThisBoard);

                    // Does this new board match the desired outcome board?
                    if (BoardsMatch(bThisBoard, bTo))
                    {
                        // FOR DEBUGGING PURPOSES, RE-CALL THE WHAT'S=POSSIBLE FUNCTION THAT HAS RESULTED
                        // IN THE CONCLUSION THIS MOVE IS VALID.
                        //var debugOptions = bFrom.WhatCanICauseWithDoo(piece);
                        // END DEBUGGING DIAGNOSTIC

                        return true;
                    }
                    // Console.WriteLine("Boards do NOT match:");
                }
            }
            return false;
        }


        protected static string StringFromColorEnum(ColorsEnum col)
        {
            switch (col)
            {
                case ColorsEnum.Black: return "Black";
                case ColorsEnum.Tan: return "Tan";
                case ColorsEnum.White: return "White";
                default:
                    Debug.Assert(false);
                    return null;
            }
        }



        /// <summary>
        /// ////////////////////////////////////////////
        /// ////////////////////////////////////////////
        /// ////////////////////////////////////////////
        /// 
        /// NON-ENDPOINTS ABOVE, ENDPOINTS BELOW
        /// 
        /// ////////////////////////////////////////////
        /// ////////////////////////////////////////////
        /// ////////////////////////////////////////////
        /// </summary>

        // a single mutex serializes endpoint calls
        private static System.Threading.Mutex m_serializerMutex = new System.Threading.Mutex();


        /*  THIS WON'T COMPILE IN UPGRADE TO HC22 SO TEMPORARILY PULLING IT OFFLINE.
        [HttpPost]
        public IActionResult CreateScenario([FromQuery] string gameId, [FromQuery] string whoseTurn)
        {
            m_serializerMutex.WaitOne();
            try
            {
                if (null == gameId)
                    return BadRequest();

                // a scenario creates a gameId that doesn't exist, with a board setup and whoseTurn specified by json in the body
                // visualBoardStore has no way to create 
                bool fSuccess = false;
                if (false == VisualBoardStore.ContainsGame(gameId))
                {
                    // feed new game to VisualBoardStore

                    System.IO.Stream req = Request.Body;
                    //     req.Seek(0, System.IO.SeekOrigin.Begin);
                    string json = new System.IO.StreamReader(req).ReadToEnd();
                    var pieces = System.Text.Json.JsonSerializer.Deserialize<List<Spot>>(json);

                    HexC.Board board = new HexC.Board();
                    foreach (Spot spot in pieces)
                    {
                        var piece = new PlacedPiece(FromString.PieceFromString(spot.Piece), FromString.ColorFromString(spot.Color), spot.Q, spot.R);
                        if (null == board.AnyoneThere(piece.Location)) // don't tase me bro
                            board.Add(piece);
                        else
                            Debug.Assert(false);// why did this client shove two pieces in one spot at me?
                    }

                    VisualBoardStore.CreateGameScenario(gameId, board, FromString.ColorFromString(whoseTurn));
                    fSuccess = true;
                }

                return Ok(System.Text.Json.JsonSerializer.Serialize(fSuccess));
            }
            finally { m_serializerMutex.ReleaseMutex(); }
        }
        */



        // Receive moves that constitute a player's turn, but only accept them if they're confirmed to be a valid outcome.
        // if it is valid, also determine if it places any other player into checkmate.

        [Route("Moves")]
        [HttpPost]
//        [HttpPost("{gameId:guid}", Name = nameof(BoardController))]
        //        [HttpPost(Name = "Moves")]
        //        [HttpPost("moveit/forever")]
        //      [SwaggerOperation("GetCustomer")]
        //    [SwaggerResponse((int)HttpStatusCode.OK)]
        //  [SwaggerResponse((int)HttpStatusCode.NotFound)]

        public IActionResult Moves([FromQuery] string gameId)
        {
            m_serializerMutex.WaitOne();
            try
            {
                if (null == gameId)
                    return BadRequest();
                VisualBoardStore.MakeCertainGameExists(gameId);

//                System.IO.Stream req = Request.Body;
                //     req.Seek(0, System.IO.SeekOrigin.Begin);
                string json = new System.IO.StreamReader(Request.Body).ReadToEnd();
                var pieces = System.Text.Json.JsonSerializer.Deserialize<List<Spot>>(json);

                HexC.Board bProposed = new HexC.Board();
                foreach (Spot spot in pieces)
                {
                    if (spot.Q == 99)
                    {
                        // my client shoves sideline details at me in Q,R = 99, but i infer sideline now.
                        // bProposed.Add(new Piece(FromString.PieceFromString(spot.Piece), FromString.ColorFromString(spot.Color)));
                    }
                    else
                    {
                        var piece = new PlacedPiece(FromString.PieceFromString(spot.Piece), FromString.ColorFromString(spot.Color), spot.Q, spot.R);
                        if (null == bProposed.AnyoneThere(piece.Location)) // don't tase me bro
                            bProposed.Add(piece);
                        else
                            Debug.Assert(false);// why did this client shove two pieces in one spot at me?
                    }
                }

                // Determine if this proposed board is among the outcomes considered valid for this player on the current board
                HexC.ColorsEnum col = VisualBoardStore.WhoseTurn(gameId);
                bool fSuccess = CanFindOptionResultingInNewBoard(col, VisualBoardStore.GameBoard(gameId), bProposed);
                if (fSuccess)
                {
                    // eventually i'll need to track the last turn to roll back, perhaps multiple rolls back, but for now just take it.
                    VisualBoardStore.ReportSuccessfulTurn(gameId, col, bProposed);
                }
                return Ok(System.Text.Json.JsonSerializer.Serialize(fSuccess));
                // i guess as far as i'm concerned it's the next player's turn. We all want a visual indicator of whose turn it is. May as well get one.
            }
            finally { m_serializerMutex.ReleaseMutex(); }
        }


        //        [HttpPost("{gameId:guid}", Name = nameof(BoardController))]
        [Route("RollBackOne")]
        [HttpPost]
//        [Route("api/RollinBaby/whatever")]
//        [HttpPost(Name = "RollBackOne")]
        public IActionResult RollBackOne([FromQuery] string gameId)
        {
            m_serializerMutex.WaitOne();
            try
            {
                if (null == gameId)
                    return BadRequest();
                VisualBoardStore.MakeCertainGameExists(gameId);
                VisualBoardStore.RollbackOneMove(gameId); // also assures this game isn't over

                return Ok();
            }
            finally { m_serializerMutex.ReleaseMutex(); }
        }


        //        [HttpGet]
        [Route("WhoseTurn")]
        [HttpGet] //(Name = "WhoseTurn")]
        public IActionResult WhoseTurn([FromQuery] string gameId)
        {
            m_serializerMutex.WaitOne();
            try
            {
                if (null == gameId)
                    return BadRequest();
                VisualBoardStore.MakeCertainGameExists(gameId);

                HexC.ColorsEnum colorsEnum = VisualBoardStore.WhoseTurn(gameId);
                string col = StringFromColorEnum(colorsEnum);
                if (VisualBoardStore.GameOver(gameId))
                    col = "LOST: " + col;
                var jsonString = System.Text.Json.JsonSerializer.Serialize(col);
                return Ok(jsonString);
            }
            finally { m_serializerMutex.ReleaseMutex(); }
        }


        [Route("Board")]
        [HttpGet]
        public IActionResult Board([FromQuery] string gameId, [FromQuery] string color) // IS color USED? NO?
        {
            m_serializerMutex.WaitOne();
            try
            {
                if (null == gameId || color == null)
                    return BadRequest();
                VisualBoardStore.MakeCertainGameExists(gameId);

                Board b = VisualBoardStore.GameBoard(gameId);

                PrettyJsonBoard pjb = new PrettyJsonBoard(b.PlacedPieces, b.SidelinedPieces);

                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var jsonString = System.Text.Json.JsonSerializer.Serialize(pjb, options);

                Console.WriteLine(jsonString);

                return Ok(jsonString);
            }
            finally { m_serializerMutex.ReleaseMutex(); }
        }

        /* revive this when I'm using CORS again

        [HttpOptions]
        public IActionResult Board()
        {
            Response.Headers.Add("Allow", "OPTIONS,GET,PUT,POST,PATCH");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,OPTIONS");
            Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type,Authorization,Content-Length,X-Requested-With,Accept");
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Content-Length", "0");

            return Ok();
        }
    */

    }
}


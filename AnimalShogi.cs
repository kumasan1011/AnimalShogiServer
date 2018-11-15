using System;
using System.Linq;

/*
  0  1  2  3
  4  5  6  7
  8  9  10 11
  12 13 14 15
  16 17 18 19
  20 21 22 23
*/

namespace AnimalShogi
{
    public enum Color {
        BLACK, WHITE, COLOR_NB, NULL,
    };

    public enum Square {
        SQ_01, SQ_02, SQ_03, SQ_04,
        SQ_05, SQ_06, SQ_07, SQ_08,
        SQ_09, SQ_10, SQ_11, SQ_12,
        SQ_13, SQ_14, SQ_15, SQ_16,
        SQ_17, SQ_18, SQ_19, SQ_20,
        SQ_21, SQ_22, SQ_23, SQ_24,

        SQ_NB,
    };

    public static class Piece
    {
        public const int Empty = 0;
        public const int Wall = 16; // out of board
		public const int PromoteBit = 4;
		public const int WhiteBit = 8;
		public const int BP = 1, WP = BP + WhiteBit; // pawn
        public const int BB = 2, WB = BB + WhiteBit; // bishop
		public const int BR = 3, WR = BR + WhiteBit; // rook
		public const int BK = 4, WK = BK + WhiteBit; // king
        public const int BPP = BP + PromoteBit, WPP = BPP + WhiteBit; // propawn

        public static readonly int[][] Inc = new int[][] 
		{
            null,
            new[]{-4},                             // Bpawn
            new[]{-5, -3, +3, +5},                 // Bbishop
			new[]{-4, -1, +1, +4},                 // Brook
            new[]{-5, -4, -3, -1, +1, +3, +4, +5}, // Bking
            new[]{-5, -4, -3, -1, +1, +4 },        // Bpropawn
            null,
            null,
            null,
            new[]{+4},                             // Bpawn
            new[]{-5, -3, +3, +5},                 // Bbishop
			new[]{-4, -1, +1, +4},                 // Brook
            new[]{-5, -4, -3, -1, +1, +3, +4, +5}, // Bking
            new[]{-4, -1, +1, +3, +4, +5},         // Bpropawn
        };

        public static readonly int[] StartPos = new int[]
        {
            Piece.Wall, Piece.Wall, Piece.Wall, Piece.Wall,
            Piece.WR,   Piece.WK,   Piece.WB,   Piece.Wall,
            Piece.Empty,Piece.WP,   Piece.Empty,Piece.Wall,
            Piece.Empty,Piece.BP,   Piece.Empty,Piece.Wall,
            Piece.BR,   Piece.BK,   Piece.BB,   Piece.Wall,
            Piece.Wall, Piece.Wall, Piece.Wall, Piece.Wall,
        };

        public static int Abs(int piece) {
            return piece & (~WhiteBit);
        }

        public static bool CanPromote(int piece, Square to) {

            if (Abs(piece) !=  BP) 
              return false;

            if (  (IsBlack(piece) && (Square.SQ_04 < to && to < Square.SQ_08))
               || (IsWhite(piece) && (Square.SQ_16 < to && to < Square.SQ_20)))
              return true; 
            
            return false;
        }

        public static bool IsBlack(int piece) {
            return (piece & WhiteBit) == 0x00;
        }

        public static bool IsWhite(int piece) {
            return (piece & WhiteBit) != 0x00;
        }

        public static Color ColorIs(int piece) {
            return IsBlack(piece) ? Color.BLACK : Color.WHITE;
        }

        public static string[] PieceChar = new string[] 
        {
            "   ",                             // EMPTY
            " H ", " Z ", " K ", " L ", " N ", // BLACK
            "ERR", "ERR", "ERR",               // UNUSED
            " h ", " z ", " k ", " l ", " n ", // WHITE
            "ERR", "ERR",                      // UNUSED
            "WLL",
        };
    }

    // move
    // xxxx xxx1 1111 : from
    // xx11 111x xxxx : to
    // x1xx xxxx xxxx : drop
    // 1xxx xxxx xxxx : promote
    public class Move
    {
        public int MakeSquare(int file, int rank) {
            return 4 * rank + file;
        }

        public int File(Square sq) {
            return (int)sq % 4;
        }

        public int Rank(Square sq) {
            return (int)sq / 4;
        }

        const string PieceChar = " hzk";
        const string FileChar  = "123";
        const string RankChar  = " abcd";
        public Move(string sfen) {

            Console.WriteLine(sfen);

            int tFile = FileChar.IndexOf(sfen.Substring(2, 1));
            int tRank = RankChar.IndexOf(sfen.Substring(3, 1));
            bool drop = sfen.Substring(1, 1) == "*";

            if (drop) {
                int fKind = PieceChar.IndexOf(sfen.Substring(0, 1));
                move = fKind + (MakeSquare(tFile, tRank) << 5) + (1 << 10); 
            }
            else {
                int fFile = FileChar.IndexOf(sfen.Substring(0, 1));
                int fRank = RankChar.IndexOf(sfen.Substring(1, 1));
                move = MakeSquare(fFile, fRank) + (MakeSquare(tFile, tRank) << 5); 
                if (sfen.Substring(4, 1) == "+")
                    move += (1 << 11);
            }
        }

        public Move(Square from, Square to, bool promote, bool drop) {

            move = (int)from + ((int)to << 5);

            if (drop) {
                move += (1 << 10); 
            }
            else if (promote)
                move += (1 << 11);
        }

        public string ToSfen() {
            
            string str = String.Empty;

            if (Drop()) {
                str += PieceChar[(int)From()] + "*";
            }
            else {
                str += FileChar[(int)File(From())];
                str += RankChar[(int)Rank(From())];
            }

            str += FileChar[(int)File(To())];
            str += RankChar[(int)Rank(To())]; 

            if (Promote())
              str += "+";

            return str;
        }

        public Square From() {
            return (Square)(move & 0b11111);
        }

        public Square To() {
            return (Square)((move >> 5) & 0b11111);
        }

        public bool Drop() {
            return ((move >> 10) & 0b1) == 1;
        }

        public bool Promote() {
            return ((move >> 11) & 0b1) == 1;
        }

        private int move;
    }

    public class Position
    {
        public Position() {

            stand[Black] = new int[Piece.BR + 1];
			stand[White] = new int[Piece.BR + 1];

            // 初期局面を作っておく
            for (int i = 0; i <= Piece.BR; ++i)
              stand[Black][i] = stand[White][i] = 0;

            for (int i = 0; i < SquareSize; ++i)
              square[i] = Piece.StartPos[i];

            sideToMove = Color.BLACK;
            kingPos[Black] = (int)Square.SQ_22;
            kingPos[White] = (int)Square.SQ_06;
        }

        public void PrintPosition() {

            string str = String.Empty;

            Console.WriteLine("+---+---+---+");
            str = "|";
            for (int i = (int)Square.SQ_05; i < (int)Square.SQ_21; ++i)
            {
                // 壁なら出力して次の行へ
                if (square[i] == Piece.Wall)
                {
                    Console.WriteLine(str);
                    if (i != (int)Square.SQ_20)
                      str = "|";
                    continue;
                }
                  
                str += Piece.PieceChar[square[i]];
                str += "|";
            }
            Console.WriteLine("+---+---+---+");
        }
        public bool IsDrop(Square from) {
            return from < Square.SQ_05 ? true : false;
        }

        public bool IsLegalMove(Move m)
        {
            Square from = m.From();
            Square to = m.To(); 
            bool promote = m.Promote();

            // out of range
            if (   from < Square.SQ_01
                || from > Square.SQ_19
                || to   < Square.SQ_01
                || to   > Square.SQ_19)
                return false;

            if (IsDrop(from))
            {
                if (promote)
                    return false;
                if (square[(int)to] != Piece.Empty)
                    return false;
            }
            else {
                int piece = square[(int)from];

                // 同じ場所には移動できない
                if (from == to)
                    return false;

                // 駒が存在しない、または壁
                if (   piece == Piece.Empty
                    || piece == Piece.Wall)
                    return false;

                // 動かすのが自分の駒でない
                if (   (sideToMove == Color.BLACK && Piece.IsWhite(piece))
                    || (sideToMove == Color.WHITE && Piece.IsBlack(piece)))
                    return false;
            
                int cap = square[(int)to];

                // 取るのが相手の駒でない
                if ( cap != Piece.Empty  
                  && ((sideToMove == Color.WHITE && Piece.IsWhite(cap))
                  || (sideToMove == Color.BLACK && Piece.IsBlack(cap))))
                    return false;

                int inc = to - from;

                // 動けない方向に動いている
                if (!Piece.Inc[piece].Contains(inc))
                    return false;

                if (promote && !Piece.CanPromote(piece, to))
                  return false; 
            }
            
            return true;
        }
    
        public bool DoMove(Move m) 
        {
            Square from = m.From();
            Square to = m.To(); 
            bool promote = m.Promote();

            int fKind =  IsDrop(from) ? sideToMove == Color.BLACK ? (int)from : (int)from + Piece.WhiteBit 
                                      : square[(int)from];
            int tKind = fKind + (promote ? Piece.PromoteBit : 0);
            int capture = square[(int)to];

            square[(int)to] = tKind;
            square[(int)from] = Piece.Empty;

            // 手番変更
            sideToMove = (sideToMove == Color.BLACK) ? Color.WHITE : Color.BLACK;

            // トライ勝ち
            if (sideToMove == Color.BLACK)
            {
                if (Square.SQ_04 < to && to < Square.SQ_08)
                  return true;
            }
            else {
                if (Square.SQ_16 < to && to < Square.SQ_20)
                  return true;
            }

            return (capture == Piece.BK || capture == Piece.WK) ? true : false;
        }

        public void UndoMove(Move m)
        {
            Square from = m.From();
            Square to = m.To(); 
            bool promote = m.Promote();
            int fKind =  IsDrop(from) ? sideToMove == Color.BLACK ? (int)from : (int)from + Piece.WhiteBit 
                                      : square[(int)from];
            int tKind = fKind + (promote ? Piece.PromoteBit : 0);
            int capture = square[(int)to];

            square[(int)to] = tKind;
            square[(int)from] = Piece.Empty;

            // 手番変更
            sideToMove = (sideToMove == Color.BLACK) ? Color.WHITE : Color.BLACK;
        }

        public int Stand(int color, int absKind)
		{
			return stand[color][absKind];
		}
		public int KingPos(int color)
		{
			return kingPos[color];
		}

        public int SquareIs(Square sq)
        {
            return square[(int)sq];
        }

        public Color SideToMove()
        {
            return sideToMove;
        }

        public const int SquareSize = 24;
        const int Black = (int)Color.BLACK, White = (int)Color.WHITE; // alias
        
        private int[] square = new int[SquareSize];
		private int[] kingPos = new int[(int)Color.COLOR_NB];
		private int[][] stand = new int[(int)Color.COLOR_NB][];
        private Color sideToMove;
    } 
}

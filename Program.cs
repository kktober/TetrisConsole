using System;
using System.Collections.Generic;
using System.Threading;

namespace TetrisConsole;

internal static class Program
{
    private const int BoardWidth = 10;
    private const int BoardHeight = 20;
    private static readonly Random Random = new();

    private static readonly bool[][,] Shapes =
    {
        new bool[,] { { true, true, true, true } },                         // I
        new bool[,] { { true, true }, { true, true } },                     // O
        new bool[,] { { false, true, false }, { true, true, true } },       // T
        new bool[,] { { true, false, false }, { true, true, true } },       // J
        new bool[,] { { false, false, true }, { true, true, true } },       // L
        new bool[,] { { false, true, true }, { true, true, false } },      // S
        new bool[,] { { true, true, false }, { false, true, true } }       // Z
    };

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;

        bool playAgain;
        do
        {
            RunGame();
            Console.WriteLine();
            Console.Write("Начать заново? (Y/N): ");
            playAgain = ReadRestartChoice();
        } while (playAgain);

        Console.CursorVisible = true;
        Console.WriteLine("Спасибо за игру!");
    }

    private static bool ReadRestartChoice()
    {
        while (true)
        {
            ConsoleKey key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Y || key == ConsoleKey.Enter) return true;
            if (key == ConsoleKey.N || key == ConsoleKey.Escape) return false;
        }
    }

    private static void RunGame()
    {
        int[,] board = new int[BoardHeight, BoardWidth];
        int score = 0;
        int lines = 0;
        int level = 1;
        bool gameOver = false;

        Piece current = CreatePiece();
        DateTime lastFall = DateTime.UtcNow;
        int fallInterval = 650;

        Console.Clear();
        while (!gameOver)
        {
            while (Console.KeyAvailable)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        TryMove(current, board, -1, 0);
                        break;
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        TryMove(current, board, 1, 0);
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        if (TryMove(current, board, 0, 1)) score += 1;
                        else LockAndSpawn();
                        lastFall = DateTime.UtcNow;
                        break;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                    case ConsoleKey.Spacebar:
                        TryRotate(current, board);
                        break;
                    case ConsoleKey.Escape:
                        gameOver = true;
                        break;
                }
            }

            if (gameOver) break;

            if ((DateTime.UtcNow - lastFall).TotalMilliseconds >= fallInterval)
            {
                if (!TryMove(current, board, 0, 1))
                    LockAndSpawn();

                lastFall = DateTime.UtcNow;
            }

            Draw(board, current, score, lines, level);
            Thread.Sleep(25);

            void LockAndSpawn()
            {
                PlacePiece(current, board);
                int cleared = ClearCompletedLines(board);
                if (cleared > 0)
                {
                    lines += cleared;
                    score += cleared switch { 1 => 100, 2 => 300, 3 => 500, _ => 800 };
                    level = lines / 10 + 1;
                    fallInterval = Math.Max(100, 650 - (level - 1) * 55);
                }

                current = CreatePiece();
                if (!CanPlace(current, board, current.X, current.Y))
                    gameOver = true;
            }
        }

        Draw(board, current, score, lines, level);
        Console.SetCursorPosition(0, BoardHeight + 4);
        Console.WriteLine("ИГРА ОКОНЧЕНА!");
        Console.WriteLine($"Итоговый счёт: {score}. Заполнено линий: {lines}.");
    }

    private static Piece CreatePiece()
    {
        bool[,] shape = (bool[,])Shapes[Random.Next(Shapes.Length)].Clone();
        return new Piece(shape, BoardWidth / 2 - shape.GetLength(1) / 2, 0);
    }

    private static bool TryMove(Piece piece, int[,] board, int dx, int dy)
    {
        int newX = piece.X + dx;
        int newY = piece.Y + dy;
        if (!CanPlace(piece, board, newX, newY)) return false;
        piece.X = newX;
        piece.Y = newY;
        return true;
    }

    private static bool CanPlace(Piece piece, int[,] board, int x, int y)
    {
        for (int r = 0; r < piece.Shape.GetLength(0); r++)
        for (int c = 0; c < piece.Shape.GetLength(1); c++)
        {
            if (!piece.Shape[r, c]) continue;
            int boardX = x + c;
            int boardY = y + r;

            if (boardX < 0 || boardX >= BoardWidth || boardY >= BoardHeight)
                return false;

            if (boardY >= 0 && board[boardY, boardX] == 1)
                return false;
        }
        return true;
    }

    private static void TryRotate(Piece piece, int[,] board)
    {
        int rows = piece.Shape.GetLength(0);
        int cols = piece.Shape.GetLength(1);
        bool[,] rotated = new bool[cols, rows];

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            rotated[c, rows - 1 - r] = piece.Shape[r, c];

        var candidate = new Piece(rotated, piece.X, piece.Y);
        if (CanPlace(candidate, board, candidate.X, candidate.Y))
            piece.Shape = rotated;
    }

    private static void PlacePiece(Piece piece, int[,] board)
    {
        for (int r = 0; r < piece.Shape.GetLength(0); r++)
        for (int c = 0; c < piece.Shape.GetLength(1); c++)
        {
            if (!piece.Shape[r, c]) continue;
            int y = piece.Y + r;
            int x = piece.X + c;
            if (y >= 0 && y < BoardHeight && x >= 0 && x < BoardWidth)
                board[y, x] = 1;
        }
    }

    private static int ClearCompletedLines(int[,] board)
    {
        int cleared = 0;
        for (int row = BoardHeight - 1; row >= 0; row--)
        {
            bool full = true;
            for (int col = 0; col < BoardWidth; col++)
                if (board[row, col] == 0) { full = false; break; }

            if (!full) continue;

            cleared++;
            for (int y = row; y > 0; y--)
            for (int x = 0; x < BoardWidth; x++)
                board[y, x] = board[y - 1, x];

            for (int x = 0; x < BoardWidth; x++)
                board[0, x] = 0;

            row++;
        }
        return cleared;
    }

    private static void Draw(int[,] board, Piece piece, int score, int lines, int level)
    {
        char[,] view = new char[BoardHeight, BoardWidth];
        for (int y = 0; y < BoardHeight; y++)
        for (int x = 0; x < BoardWidth; x++)
            view[y, x] = board[y, x] == 1 ? '█' : ' ';

        for (int r = 0; r < piece.Shape.GetLength(0); r++)
        for (int c = 0; c < piece.Shape.GetLength(1); c++)
        {
            int x = piece.X + c;
            int y = piece.Y + r;
            if (piece.Shape[r, c] && y >= 0 && y < BoardHeight && x >= 0 && x < BoardWidth)
                view[y, x] = '▓';
        }

        Console.SetCursorPosition(0, 0);
        Console.WriteLine("┌" + new string('─', BoardWidth * 2) + "┐");
        for (int y = 0; y < BoardHeight; y++)
        {
            Console.Write("│");
            for (int x = 0; x < BoardWidth; x++)
                Console.Write(view[y, x] == ' ' ? "  " : $"{view[y, x]} ");
            Console.WriteLine("│");
        }
        Console.WriteLine("└" + new string('─', BoardWidth * 2) + "┘");
        Console.WriteLine($"Счёт: {score}   Линии: {lines}   Уровень: {level}       ");
        Console.WriteLine("Управление: ←/A влево, →/D вправо, ↓/S вниз, ↑/W/Пробел — поворот, Esc — выход");
    }

    private sealed class Piece
    {
        public bool[,] Shape { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public Piece(bool[,] shape, int x, int y)
        {
            Shape = shape;
            X = x;
            Y = y;
        }
    }
}

using System;
using AdventureGame.Core;

namespace AdventureGame.ConsoleUI
{
    class Program
    {
        static void Main()
        {
            var game = new GameEngine(21, 21);

            while (!game.IsGameOver)
            {
                Console.Clear();
                Draw(game);

                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.W) game.Move(0, -1);
                if (key.Key == ConsoleKey.S) game.Move(0, 1);
                if (key.Key == ConsoleKey.A) game.Move(-1, 0);
                if (key.Key == ConsoleKey.D) game.Move(1, 0);
            }

            Console.Clear();
            Draw(game);

            Console.WriteLine();
            Console.WriteLine(game.HasWon ? "YOU WIN!" : "GAME OVER!");
            Console.ReadKey();
        }

        static void Draw(GameEngine game)
        {
            Console.WriteLine($"HP: {game.Player.Health}");
            Console.WriteLine(game.LastMessage);
            Console.WriteLine();

            for (int y = 0; y < game.Maze.Height; y++)
            {
                for (int x = 0; x < game.Maze.Width; x++)
                {
                    if (x == game.PlayerX && y == game.PlayerY)
                    {
                        Console.Write("@ ");
                        continue;
                    }

                    var tile = game.Maze.Tiles[y, x];

                    char symbol = tile.Type switch
                    {
                        TileType.Wall => '#',
                        TileType.Empty => '.',
                        TileType.Monster => 'M',
                        TileType.Weapon => 'W',
                        TileType.Potion => 'P',
                        TileType.Exit => 'E',
                        _ => '.'
                    };

                    Console.Write(symbol + " ");
                }
                Console.WriteLine();
            }
        }
    }
}

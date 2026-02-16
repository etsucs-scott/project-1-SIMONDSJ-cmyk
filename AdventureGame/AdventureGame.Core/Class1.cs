using System;
using System.Collections.Generic;
using System.Linq;

namespace AdventureGame.Core
{
    // =========================
    // ICharacter
    // =========================
    public interface ICharacter
    {
        int Health { get; }
        bool IsAlive { get; }
        void Attack(ICharacter target);
        void TakeDamage(int amount);
    }

    // =========================
    // Player
    // =========================
    public class Player : ICharacter
    {
        private const int BaseDamage = 10;
        private const int MaxHealth = 150;

        public int Health { get; private set; } = 100;
        public bool IsAlive => Health > 0;

        public List<Weapon> Inventory { get; } = new();

        public void Attack(ICharacter target)
        {
            int bestWeapon = Inventory.Any()
                ? Inventory.Max(w => w.AttackModifier)
                : 0;

            int damage = BaseDamage + bestWeapon;
            target.TakeDamage(damage);
        }

        public void TakeDamage(int amount)
        {
            Health -= amount;
            if (Health < 0) Health = 0;
        }

        public void Heal(int amount)
        {
            Health += amount;
            if (Health > MaxHealth)
                Health = MaxHealth;
        }

        public void AddItem(Item item)
        {
            if (item is Weapon weapon)
                Inventory.Add(weapon);
            else if (item is Potion potion)
                potion.Apply(this);
        }
    }

    // =========================
    // Monster
    // =========================
    public class Monster : ICharacter
    {
        private const int BaseDamage = 10;

        public int Health { get; private set; }
        public bool IsAlive => Health > 0;

        public Monster(Random random)
        {
            Health = random.Next(30, 51); // 30–50 HP
        }

        public void Attack(ICharacter target)
        {
            target.TakeDamage(BaseDamage);
        }

        public void TakeDamage(int amount)
        {
            Health -= amount;
            if (Health < 0) Health = 0;
        }
    }

    // =========================
    // Item Base Class
    // =========================
    public abstract class Item
    {
        public string Name { get; }
        public string PickupMessage { get; }

        protected Item(string name, string message)
        {
            Name = name;
            PickupMessage = message;
        }
    }

    // =========================
    // Weapon
    // =========================
    public class Weapon : Item
    {
        public int AttackModifier { get; }

        public Weapon(string name, int modifier)
            : base(name, $"Picked up {name} (+{modifier} attack)")
        {
            AttackModifier = modifier;
        }
    }

    // =========================
    // Potion
    // =========================
    public class Potion : Item
    {
        private const int HealAmount = 20;

        public Potion()
            : base("Health Potion", "Drank potion (+20 HP)")
        {
        }

        public void Apply(Player player)
        {
            player.Heal(HealAmount);
        }
    }

    // =========================
    // Tile + Maze
    // =========================
    public enum TileType
    {
        Wall,
        Empty,
        Monster,
        Weapon,
        Potion,
        Exit
    }

    public class Tile
    {
        public TileType Type { get; set; }
        public Monster? Monster { get; set; }
        public Item? Item { get; set; }
    }

    public class Maze
    {
        public int Width { get; }
        public int Height { get; }
        public Tile[,] Tiles { get; }

        private readonly Random _random = new();

        public Maze(int width, int height)
        {
            Width = width < 10 ? 10 : width;
            Height = height < 10 ? 10 : height;

            if (Width % 2 == 0) Width++;
            if (Height % 2 == 0) Height++;

            Tiles = new Tile[Height, Width];

            Initialize();
            Generate(1, 1);
            PlaceExit();
            Populate();
        }

        private void Initialize()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Tiles[y, x] = new Tile { Type = TileType.Wall };
        }

        private void Generate(int x, int y)
        {
            Tiles[y, x].Type = TileType.Empty;

            int[] dirs = { 0, 1, 2, 3 };
            Shuffle(dirs);

            foreach (int d in dirs)
            {
                int dx = 0, dy = 0;

                if (d == 0) dy = -2;
                if (d == 1) dx = 2;
                if (d == 2) dy = 2;
                if (d == 3) dx = -2;

                int nx = x + dx;
                int ny = y + dy;

                if (InBounds(nx, ny) && Tiles[ny, nx].Type == TileType.Wall)
                {
                    Tiles[y + dy / 2, x + dx / 2].Type = TileType.Empty;
                    Generate(nx, ny);
                }
            }
        }

        private bool InBounds(int x, int y)
            => x > 0 && x < Width - 1 && y > 0 && y < Height - 1;

        private void Shuffle(int[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                int j = _random.Next(i, arr.Length);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }

        private void PlaceExit()
        {
            Tiles[Height - 2, Width - 2].Type = TileType.Exit;
        }

        private void Populate()
        {
            for (int y = 1; y < Height - 1; y++)
                for (int x = 1; x < Width - 1; x++)
                {
                    if (Tiles[y, x].Type == TileType.Empty)
                    {
                        int roll = _random.Next(100);

                        if (roll < 10)
                        {
                            Tiles[y, x].Type = TileType.Monster;
                            Tiles[y, x].Monster = new Monster(_random);
                        }
                        else if (roll < 15)
                        {
                            if (_random.Next(2) == 0)
                            {
                                Tiles[y, x].Type = TileType.Weapon;
                                Tiles[y, x].Item = new Weapon("Sword", 5);
                            }
                            else
                            {
                                Tiles[y, x].Type = TileType.Potion;
                                Tiles[y, x].Item = new Potion();
                            }
                        }
                    }
                }
        }
    }

    // =========================
    // Game Engine
    // =========================
    public class GameEngine
    {
        public Player Player { get; } = new();
        public Maze Maze { get; }

        public int PlayerX { get; private set; } = 1;
        public int PlayerY { get; private set; } = 1;

        public string LastMessage { get; private set; } = "";

        public bool IsGameOver { get; private set; }
        public bool HasWon { get; private set; }

        public GameEngine(int width, int height)
        {
            Maze = new Maze(width, height);
        }

        public void Move(int dx, int dy)
        {
            int nx = PlayerX + dx;
            int ny = PlayerY + dy;

            if (nx < 0 || ny < 0 || nx >= Maze.Width || ny >= Maze.Height)
            {
                LastMessage = "Cannot move outside maze!";
                return;
            }

            var tile = Maze.Tiles[ny, nx];

            if (tile.Type == TileType.Wall)
            {
                LastMessage = "You hit a wall!";
                return;
            }

            PlayerX = nx;
            PlayerY = ny;

            HandleTile(tile);
        }

        private void HandleTile(Tile tile)
        {
            if (tile.Type == TileType.Monster && tile.Monster != null)
            {
                Battle(tile.Monster);

                if (!Player.IsAlive)
                {
                    IsGameOver = true;
                    LastMessage = "You were slain!";
                    return;
                }

                tile.Type = TileType.Empty;
                tile.Monster = null;
                LastMessage = "Monster defeated!";
            }
            else if (tile.Item != null)
            {
                Player.AddItem(tile.Item);
                LastMessage = tile.Item.PickupMessage;

                tile.Type = TileType.Empty;
                tile.Item = null;
            }
            else if (tile.Type == TileType.Exit)
            {
                HasWon = true;
                IsGameOver = true;
                LastMessage = "You reached the exit!";
            }
            else
            {
                LastMessage = "";
            }
        }

        private void Battle(Monster monster)
        {
            while (Player.IsAlive && monster.IsAlive)
            {
                Player.Attack(monster);
                if (monster.IsAlive)
                    monster.Attack(Player);
            }
        }
    }
}

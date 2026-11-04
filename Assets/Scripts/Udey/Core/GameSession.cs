using System;

namespace Udey.Core
{
    public enum GameMode { Story, Endless }

    public enum Difficulty { Easy, Medium, Hard }

    /// <summary>
    /// What the player picked in the menus, carried into the gameplay scene.
    ///
    /// Static on purpose — statics survive a scene load, so there's no DontDestroyOnLoad
    /// object to manage and nothing to wire up in the inspector. They reset when you stop
    /// play mode, which is what you want.
    ///
    /// The menus write to this; WaveManager and friends read from it.
    /// </summary>
    public static class GameSession
    {
        public static GameMode Mode { get; private set; } = GameMode.Story;
        public static Difficulty Difficulty { get; private set; } = Difficulty.Medium;

        /// <summary>Fires whenever Mode or Difficulty changes.</summary>
        public static event Action OnChanged;

        public static void StartStory()
        {
            Mode = GameMode.Story;
            OnChanged?.Invoke();
        }

        public static void StartEndless(Difficulty difficulty)
        {
            Mode = GameMode.Endless;
            Difficulty = difficulty;
            OnChanged?.Invoke();
        }

        /// <summary>For hooking straight to a Button: 0 = Easy, 1 = Medium, 2 = Hard.</summary>
        public static void StartEndless(int difficultyIndex)
        {
            StartEndless(IndexToDifficulty(difficultyIndex));
        }

        public static Difficulty IndexToDifficulty(int index)
        {
            switch (index)
            {
                case 0: return Difficulty.Easy;
                case 2: return Difficulty.Hard;
                default: return Difficulty.Medium;
            }
        }

        // ---------------------------------------------------------------- tuning hooks
        // Read these from gameplay systems so difficulty actually does something.
        // Story mode always plays at the Medium numbers.

        /// <summary>Scales how many enemies a wave spawns.</summary>
        public static float EnemyCountMultiplier
        {
            get
            {
                if (Mode == GameMode.Story) return 1f;
                switch (Difficulty)
                {
                    case Difficulty.Easy: return 0.7f;
                    case Difficulty.Hard: return 1.4f;
                    default: return 1f;
                }
            }
        }

        /// <summary>Scales damage the player takes.</summary>
        public static float EnemyDamageMultiplier
        {
            get
            {
                if (Mode == GameMode.Story) return 1f;
                switch (Difficulty)
                {
                    case Difficulty.Easy: return 0.75f;
                    case Difficulty.Hard: return 1.3f;
                    default: return 1f;
                }
            }
        }

        /// <summary>Scales ammo drop chance — generous on Easy, stingy on Hard.</summary>
        public static float AmmoDropMultiplier
        {
            get
            {
                if (Mode == GameMode.Story) return 1f;
                switch (Difficulty)
                {
                    case Difficulty.Easy: return 1.35f;
                    case Difficulty.Hard: return 0.8f;
                    default: return 1f;
                }
            }
        }

        /// <summary>Back to defaults. Call it from the main menu if you want a clean slate.</summary>
        public static void Reset()
        {
            Mode = GameMode.Story;
            Difficulty = Difficulty.Medium;
            OnChanged?.Invoke();
        }
    }
}

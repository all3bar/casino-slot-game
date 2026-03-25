using System;
using System.Collections.Generic;

namespace CasinoSlot.Input
{
    /// <summary>
    /// USB keyboard input provider.
    /// Maps <see cref="System.ConsoleKey"/> values to <see cref="SlotAction"/> values.
    ///
    /// Default bindings (all configurable):
    /// <list type="bullet">
    ///   <item><description>Space / Enter         → Spin</description></item>
    ///   <item><description>M                     → MaxBet</description></item>
    ///   <item><description>UpArrow               → BetIncrease</description></item>
    ///   <item><description>DownArrow             → BetDecrease</description></item>
    ///   <item><description>RightArrow            → LinesIncrease</description></item>
    ///   <item><description>LeftArrow             → LinesDecrease</description></item>
    ///   <item><description>C                     → Cashout</description></item>
    ///   <item><description>Escape / A            → AcknowledgeResult</description></item>
    ///   <item><description>F10                   → OperatorMenu</description></item>
    ///   <item><description>R                     → RepeatBet</description></item>
    /// </list>
    /// </summary>
    public class KeyboardInputProvider : IInputProvider
    {
        private readonly Dictionary<ConsoleKey, SlotAction> _bindings;

        // Key-state tracking to emit only on key-down transitions
        private readonly HashSet<ConsoleKey> _heldKeys = new HashSet<ConsoleKey>();

        // Injected key-available / key-read delegates (allow unit testing without a real console)
        private readonly Func<bool> _keyAvailable;
        private readonly Func<ConsoleKeyInfo> _readKey;

        /// <inheritdoc/>
        public string Name => "Keyboard";

        /// <inheritdoc/>
        public bool IsConnected => true; // USB keyboard always considered connected

        /// <summary>
        /// Creates a <see cref="KeyboardInputProvider"/> with the default key bindings.
        /// </summary>
        /// <param name="keyAvailable">
        /// Optional delegate that returns true when a key is waiting to be read.
        /// Defaults to <see cref="Console.KeyAvailable"/>.
        /// </param>
        /// <param name="readKey">
        /// Optional delegate that reads (intercepts) one key.
        /// Defaults to <c>Console.ReadKey(intercept: true)</c>.
        /// </param>
        public KeyboardInputProvider(
            Func<bool>? keyAvailable = null,
            Func<ConsoleKeyInfo>? readKey = null)
        {
            _keyAvailable = keyAvailable ?? (() => Console.KeyAvailable);
            _readKey = readKey ?? (() => Console.ReadKey(intercept: true));
            _bindings = BuildDefaultBindings();
        }

        /// <inheritdoc/>
        public IReadOnlyList<SlotAction> Poll()
        {
            var actions = new List<SlotAction>();

            // Drain all available key events
            while (_keyAvailable())
            {
                ConsoleKeyInfo info = _readKey();
                ConsoleKey key = info.Key;

                if (!_heldKeys.Contains(key))
                {
                    _heldKeys.Add(key);
                    if (_bindings.TryGetValue(key, out SlotAction action) && action != SlotAction.None)
                        actions.Add(action);
                }
            }

            return actions;
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, SlotAction> GetBindings()
        {
            var result = new Dictionary<string, SlotAction>();
            foreach (var kv in _bindings)
                result[kv.Key.ToString()] = kv.Value;
            return result;
        }

        /// <inheritdoc/>
        public bool SetBinding(string inputKey, SlotAction action)
        {
            if (Enum.TryParse<ConsoleKey>(inputKey, ignoreCase: true, out ConsoleKey key))
            {
                _bindings[key] = action;
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public bool RemoveBinding(string inputKey)
        {
            if (Enum.TryParse<ConsoleKey>(inputKey, ignoreCase: true, out ConsoleKey key))
                return _bindings.Remove(key);
            return false;
        }

        /// <summary>
        /// Must be called when a key is released to allow re-triggering.
        /// In Unity, this is done in the Update loop; in tests, call it manually.
        /// </summary>
        public void NotifyKeyReleased(ConsoleKey key)
        {
            _heldKeys.Remove(key);
        }

        /// <summary>Clears all held-key state (e.g. when focus is lost).</summary>
        public void ClearHeldKeys() => _heldKeys.Clear();

        // ---------------------------------------------------------------
        private static Dictionary<ConsoleKey, SlotAction> BuildDefaultBindings()
        {
            return new Dictionary<ConsoleKey, SlotAction>
            {
                // Primary spin buttons
                { ConsoleKey.Spacebar,    SlotAction.Spin             },
                { ConsoleKey.Enter,       SlotAction.Spin             },

                // Bet controls
                { ConsoleKey.UpArrow,     SlotAction.BetIncrease      },
                { ConsoleKey.DownArrow,   SlotAction.BetDecrease      },
                { ConsoleKey.M,           SlotAction.MaxBet           },

                // Lines controls
                { ConsoleKey.RightArrow,  SlotAction.LinesIncrease    },
                { ConsoleKey.LeftArrow,   SlotAction.LinesDecrease    },

                // Cashout
                { ConsoleKey.C,           SlotAction.Cashout          },

                // Acknowledge / collect
                { ConsoleKey.Escape,      SlotAction.AcknowledgeResult},
                { ConsoleKey.A,           SlotAction.AcknowledgeResult},

                // Repeat bet
                { ConsoleKey.R,           SlotAction.RepeatBet        },

                // Operator menu (requires PIN after opening)
                { ConsoleKey.F10,         SlotAction.OperatorMenu     },
            };
        }
    }
}

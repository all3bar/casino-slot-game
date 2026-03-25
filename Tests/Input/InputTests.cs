using System;
using System.Collections.Generic;
using Xunit;
using CasinoSlot.Input;

namespace CasinoSlot.Tests.Input
{
    /// <summary>
    /// Unit tests for the input abstraction layer (keyboard, joystick, InputManager).
    /// </summary>
    public class InputTests
    {
        // ================================================================
        // SlotAction enum sanity
        // ================================================================

        [Fact]
        public void SlotAction_None_IsZero()
        {
            Assert.Equal(0, (int)SlotAction.None);
        }

        [Fact]
        public void SlotAction_HasExpectedValues()
        {
            Assert.Equal(1,  (int)SlotAction.Spin);
            Assert.Equal(2,  (int)SlotAction.MaxBet);
            Assert.Equal(3,  (int)SlotAction.BetIncrease);
            Assert.Equal(4,  (int)SlotAction.BetDecrease);
            Assert.Equal(5,  (int)SlotAction.LinesIncrease);
            Assert.Equal(6,  (int)SlotAction.LinesDecrease);
            Assert.Equal(7,  (int)SlotAction.Cashout);
            Assert.Equal(8,  (int)SlotAction.AcknowledgeResult);
            Assert.Equal(9,  (int)SlotAction.OperatorMenu);
            Assert.Equal(10, (int)SlotAction.RepeatBet);
        }

        // ================================================================
        // KeyboardInputProvider tests
        // ================================================================

        [Fact]
        public void Keyboard_Name_IsKeyboard()
        {
            var kb = CreateKeyboard();
            Assert.Equal("Keyboard", kb.Name);
        }

        [Fact]
        public void Keyboard_IsAlwaysConnected()
        {
            var kb = CreateKeyboard();
            Assert.True(kb.IsConnected);
        }

        [Fact]
        public void Keyboard_DefaultBindings_ContainSpin()
        {
            var kb = CreateKeyboard();
            var bindings = kb.GetBindings();
            Assert.Contains("Spacebar", bindings.Keys);
            Assert.Equal(SlotAction.Spin, bindings["Spacebar"]);
        }

        [Fact]
        public void Keyboard_DefaultBindings_AllPrimaryActionsPresent()
        {
            var kb = CreateKeyboard();
            var bindings = kb.GetBindings();
            var actions = new HashSet<SlotAction>(bindings.Values);
            Assert.Contains(SlotAction.Spin,              actions);
            Assert.Contains(SlotAction.MaxBet,            actions);
            Assert.Contains(SlotAction.BetIncrease,       actions);
            Assert.Contains(SlotAction.BetDecrease,       actions);
            Assert.Contains(SlotAction.LinesIncrease,     actions);
            Assert.Contains(SlotAction.LinesDecrease,     actions);
            Assert.Contains(SlotAction.Cashout,           actions);
            Assert.Contains(SlotAction.AcknowledgeResult, actions);
            Assert.Contains(SlotAction.OperatorMenu,      actions);
        }

        [Fact]
        public void Keyboard_Poll_ReturnsSpinOnSpacebar()
        {
            var keys = new Queue<ConsoleKeyInfo>();
            keys.Enqueue(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));

            var kb = CreateKeyboard(keys);
            var actions = kb.Poll();

            Assert.Single(actions);
            Assert.Equal(SlotAction.Spin, actions[0]);
        }

        [Fact]
        public void Keyboard_Poll_EmptyWhenNoKeys()
        {
            var kb = CreateKeyboard(new Queue<ConsoleKeyInfo>());
            var actions = kb.Poll();
            Assert.Empty(actions);
        }

        [Fact]
        public void Keyboard_Poll_SameKeyHeldDoesNotRepeat()
        {
            var keys = new Queue<ConsoleKeyInfo>();
            keys.Enqueue(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));
            keys.Enqueue(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));

            var kb = CreateKeyboard(keys);
            var first  = kb.Poll();
            var second = kb.Poll();

            Assert.Single(first);
            Assert.Empty(second); // held key should not re-trigger
        }

        [Fact]
        public void Keyboard_Poll_RepeatsAfterRelease()
        {
            var keys = new Queue<ConsoleKeyInfo>();
            keys.Enqueue(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));

            var kb = CreateKeyboard(keys);
            kb.Poll(); // first press

            kb.NotifyKeyReleased(ConsoleKey.Spacebar);

            keys.Enqueue(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));
            var second = kb.Poll();
            Assert.Single(second);
        }

        [Fact]
        public void Keyboard_Poll_CashoutOnC()
        {
            var keys = new Queue<ConsoleKeyInfo>();
            keys.Enqueue(new ConsoleKeyInfo('c', ConsoleKey.C, false, false, false));

            var kb = CreateKeyboard(keys);
            var actions = kb.Poll();

            Assert.Single(actions);
            Assert.Equal(SlotAction.Cashout, actions[0]);
        }

        [Fact]
        public void Keyboard_SetBinding_UpdatesMap()
        {
            var kb = CreateKeyboard();
            bool ok = kb.SetBinding("F1", SlotAction.Cashout);
            Assert.True(ok);
            var bindings = kb.GetBindings();
            Assert.Equal(SlotAction.Cashout, bindings["F1"]);
        }

        [Fact]
        public void Keyboard_SetBinding_InvalidKey_ReturnsFalse()
        {
            var kb = CreateKeyboard();
            bool ok = kb.SetBinding("NOTAKEY", SlotAction.Spin);
            Assert.False(ok);
        }

        [Fact]
        public void Keyboard_RemoveBinding_Succeeds()
        {
            var kb = CreateKeyboard();
            bool removed = kb.RemoveBinding("Spacebar");
            Assert.True(removed);
            var bindings = kb.GetBindings();
            Assert.DoesNotContain("Spacebar", bindings.Keys);
        }

        [Fact]
        public void Keyboard_RemoveBinding_NonExistent_ReturnsFalse()
        {
            var kb = CreateKeyboard();
            bool removed = kb.RemoveBinding("F9"); // not in default bindings
            Assert.False(removed);
        }

        [Fact]
        public void Keyboard_ClearHeldKeys_AllowsRetrigger()
        {
            var keys = new Queue<ConsoleKeyInfo>();
            keys.Enqueue(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));
            var kb = CreateKeyboard(keys);
            kb.Poll();

            kb.ClearHeldKeys();

            keys.Enqueue(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));
            var second = kb.Poll();
            Assert.Single(second);
        }

        // ================================================================
        // JoystickInputProvider tests
        // ================================================================

        [Fact]
        public void Joystick_Name_ContainsDeviceName()
        {
            var joy = new JoystickInputProvider("My Panel");
            Assert.Contains("My Panel", joy.Name);
        }

        [Fact]
        public void Joystick_IsConnected_ByDefault()
        {
            var joy = new JoystickInputProvider();
            Assert.True(joy.IsConnected);
        }

        [Fact]
        public void Joystick_SetConnected_False_Disconnects()
        {
            var joy = new JoystickInputProvider();
            joy.SetConnected(false);
            Assert.False(joy.IsConnected);
        }

        [Fact]
        public void Joystick_DefaultBindings_Button0IsSpin()
        {
            var joy = new JoystickInputProvider();
            var bindings = joy.GetBindings();
            Assert.Equal(SlotAction.Spin, bindings["0"]);
        }

        [Fact]
        public void Joystick_DefaultBindings_AllPrimaryActionsPresent()
        {
            var joy = new JoystickInputProvider();
            var bindings = joy.GetBindings();
            var actions = new HashSet<SlotAction>(bindings.Values);
            Assert.Contains(SlotAction.Spin,              actions);
            Assert.Contains(SlotAction.MaxBet,            actions);
            Assert.Contains(SlotAction.BetIncrease,       actions);
            Assert.Contains(SlotAction.BetDecrease,       actions);
            Assert.Contains(SlotAction.LinesIncrease,     actions);
            Assert.Contains(SlotAction.LinesDecrease,     actions);
            Assert.Contains(SlotAction.Cashout,           actions);
            Assert.Contains(SlotAction.AcknowledgeResult, actions);
            Assert.Contains(SlotAction.OperatorMenu,      actions);
        }

        [Fact]
        public void Joystick_NotifyButtonState_ButtonDown_ReturnsSpin()
        {
            var joy = new JoystickInputProvider();
            SlotAction action = joy.NotifyButtonState(0, true);
            Assert.Equal(SlotAction.Spin, action);
        }

        [Fact]
        public void Joystick_NotifyButtonState_SameButtonHeld_NoRepeat()
        {
            var joy = new JoystickInputProvider();
            joy.NotifyButtonState(0, true);
            SlotAction second = joy.NotifyButtonState(0, true);
            Assert.Equal(SlotAction.None, second);
        }

        [Fact]
        public void Joystick_NotifyButtonState_ButtonUp_ClearsHeld()
        {
            var joy = new JoystickInputProvider();
            joy.NotifyButtonState(0, true);
            joy.NotifyButtonState(0, false); // release

            SlotAction third = joy.NotifyButtonState(0, true); // re-press
            Assert.Equal(SlotAction.Spin, third);
        }

        [Fact]
        public void Joystick_NotifyButtonState_WhileDisconnected_ReturnsNone()
        {
            var joy = new JoystickInputProvider();
            joy.SetConnected(false);
            SlotAction action = joy.NotifyButtonState(0, true);
            Assert.Equal(SlotAction.None, action);
        }

        [Fact]
        public void Joystick_NotifyButtonState_CashoutButton6()
        {
            var joy = new JoystickInputProvider();
            SlotAction action = joy.NotifyButtonState(6, true);
            Assert.Equal(SlotAction.Cashout, action);
        }

        [Fact]
        public void Joystick_IsButtonHeld_TrueAfterPress()
        {
            var joy = new JoystickInputProvider();
            joy.NotifyButtonState(2, true);
            Assert.True(joy.IsButtonHeld(2));
        }

        [Fact]
        public void Joystick_IsButtonHeld_FalseAfterRelease()
        {
            var joy = new JoystickInputProvider();
            joy.NotifyButtonState(2, true);
            joy.NotifyButtonState(2, false);
            Assert.False(joy.IsButtonHeld(2));
        }

        [Fact]
        public void Joystick_SetBinding_ValidIndex()
        {
            var joy = new JoystickInputProvider();
            bool ok = joy.SetBinding("10", SlotAction.RepeatBet);
            Assert.True(ok);
            var bindings = joy.GetBindings();
            Assert.Equal(SlotAction.RepeatBet, bindings["10"]);
        }

        [Fact]
        public void Joystick_SetBinding_InvalidKey_ReturnsFalse()
        {
            var joy = new JoystickInputProvider();
            bool ok = joy.SetBinding("notanumber", SlotAction.Spin);
            Assert.False(ok);
        }

        [Fact]
        public void Joystick_SetBinding_NegativeIndex_ReturnsFalse()
        {
            var joy = new JoystickInputProvider();
            bool ok = joy.SetBinding("-1", SlotAction.Spin);
            Assert.False(ok);
        }

        [Fact]
        public void Joystick_RemoveBinding_Succeeds()
        {
            var joy = new JoystickInputProvider();
            bool ok = joy.RemoveBinding("0");
            Assert.True(ok);
            var bindings = joy.GetBindings();
            Assert.DoesNotContain("0", bindings.Keys);
        }

        [Fact]
        public void Joystick_ClearHeldButtons_AllowsRetrigger()
        {
            var joy = new JoystickInputProvider();
            joy.NotifyButtonState(0, true);
            joy.ClearHeldButtons();
            SlotAction second = joy.NotifyButtonState(0, true);
            Assert.Equal(SlotAction.Spin, second);
        }

        [Fact]
        public void Joystick_Poll_ReturnsEmptyList()
        {
            var joy = new JoystickInputProvider();
            Assert.Empty(joy.Poll());
        }

        [Fact]
        public void Joystick_DisconnectClearsHeldButtons()
        {
            var joy = new JoystickInputProvider();
            joy.NotifyButtonState(0, true);
            Assert.True(joy.IsButtonHeld(0));
            joy.SetConnected(false);
            Assert.False(joy.IsButtonHeld(0));
        }

        // ================================================================
        // InputManager tests
        // ================================================================

        [Fact]
        public void InputManager_RegisterProvider_AddsToList()
        {
            var mgr = new InputManager();
            var joy = new JoystickInputProvider();
            mgr.RegisterProvider(joy);
            Assert.Contains(joy, mgr.Providers);
        }

        [Fact]
        public void InputManager_RegisterProvider_Null_Throws()
        {
            var mgr = new InputManager();
            Assert.Throws<ArgumentNullException>(() => mgr.RegisterProvider(null!));
        }

        [Fact]
        public void InputManager_RegisterSameProvider_NoDuplicates()
        {
            var mgr = new InputManager();
            var joy = new JoystickInputProvider();
            mgr.RegisterProvider(joy);
            mgr.RegisterProvider(joy);
            Assert.Single(mgr.Providers);
        }

        [Fact]
        public void InputManager_UnregisterProvider_RemovesFromList()
        {
            var mgr = new InputManager();
            var joy = new JoystickInputProvider();
            mgr.RegisterProvider(joy);
            mgr.UnregisterProvider(joy);
            Assert.Empty(mgr.Providers);
        }

        [Fact]
        public void InputManager_IsEnabled_TrueByDefault()
        {
            var mgr = new InputManager();
            Assert.True(mgr.IsEnabled);
        }

        [Fact]
        public void InputManager_SetEnabled_False_SuppressesActions()
        {
            var mgr = new InputManager();
            mgr.SetEnabled(false);

            var triggered = new List<SlotAction>();
            mgr.ActionTriggered += (a, _) => triggered.Add(a);

            mgr.DispatchAction(SlotAction.Spin);
            Assert.Empty(triggered);
        }

        [Fact]
        public void InputManager_DispatchAction_FiresEvent()
        {
            var mgr = new InputManager();
            SlotAction? received = null;
            mgr.ActionTriggered += (a, _) => received = a;

            mgr.DispatchAction(SlotAction.Cashout);
            Assert.Equal(SlotAction.Cashout, received);
        }

        [Fact]
        public void InputManager_DispatchAction_None_DoesNotFireEvent()
        {
            var mgr = new InputManager();
            int count = 0;
            mgr.ActionTriggered += (_, _) => count++;

            mgr.DispatchAction(SlotAction.None);
            Assert.Equal(0, count);
        }

        [Fact]
        public void InputManager_Update_SkipsDisconnectedProviders()
        {
            var mgr = new InputManager();
            var joy = new JoystickInputProvider("test", connected: false);
            mgr.RegisterProvider(joy);

            // Should not throw even though provider is disconnected
            mgr.Update();
        }

        [Fact]
        public void InputManager_MultipleProviders_BothPolled()
        {
            var mgr = new InputManager();
            var triggered = new List<SlotAction>();
            mgr.ActionTriggered += (a, _) => triggered.Add(a);

            // Two providers that each dispatch one action
            mgr.DispatchAction(SlotAction.Spin);
            mgr.DispatchAction(SlotAction.Cashout);

            Assert.Equal(2, triggered.Count);
            Assert.Contains(SlotAction.Spin,    triggered);
            Assert.Contains(SlotAction.Cashout, triggered);
        }

        [Fact]
        public void InputManager_DispatchAction_WithSource_IncludesSource()
        {
            var mgr = new InputManager();
            IInputProvider? source = null;
            mgr.ActionTriggered += (_, s) => source = s;

            var joy = new JoystickInputProvider("Test");
            mgr.DispatchAction(SlotAction.Spin, joy);

            Assert.Equal(joy, source);
        }

        // ================================================================
        // Helpers
        // ================================================================

        private static KeyboardInputProvider CreateKeyboard(Queue<ConsoleKeyInfo>? keys = null)
        {
            keys ??= new Queue<ConsoleKeyInfo>();
            return new KeyboardInputProvider(
                keyAvailable: () => keys.Count > 0,
                readKey: () => keys.Dequeue()
            );
        }
    }
}

using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace CommandLocker
{
    public class MainWindowViewModel
    {
        public ReactiveCommand DefaultLockDemoCommand { get; }
        public ReactiveCommand TriggerLockDemoCommand { get; }
        public ReactiveCommand LockCommand { get; }
        public ReactiveCommand UnlockCommand { get; }
        public ReactiveCommand LockTriggerCommand { get; }
        public ReactiveCommand UnlockTriggerCommand { get; }
        public ReactiveCommand UnlockForceCommand { get; }
        public AsyncReactiveCommand WaitUnlockTriggerDemoCommand { get; }

        public ReactiveProperty<string> Log { get; } = new ReactiveProperty<string>();

        private CommandLocker<int> locker = new CommandLocker<int>();

        public MainWindowViewModel()
        {
            DefaultLockDemoCommand = new ReactiveCommand(locker.CanExecute);
            TriggerLockDemoCommand = new ReactiveCommand(locker.CanExecuteWithTrigger);
            LockCommand = new ReactiveCommand();
            UnlockCommand = new ReactiveCommand();
            LockTriggerCommand = new ReactiveCommand();
            UnlockTriggerCommand = new ReactiveCommand();
            UnlockForceCommand = new ReactiveCommand();
            WaitUnlockTriggerDemoCommand = new AsyncReactiveCommand();

            var key = Guid.Empty;
            LockCommand.Subscribe(() =>
            {
                var locked = locker.Lock();
                if (locked.result) { key = locked.key; }
            });
            UnlockCommand.Subscribe(() =>
            {
                if (locker.Release(key))
                {
                    key = Guid.Empty;
                }
            });

            var triggerKey = Guid.Empty;
            var count = 0;
            LockTriggerCommand.Subscribe(() =>
            {
                var locked = locker.Lock(3);
                if (locked.result) { triggerKey = locked.key; }
            });
            UnlockTriggerCommand.Subscribe(() =>
            {
                if (triggerKey != Guid.Empty) { count++; }

                var result = locker.TryRelease(count);
                if (result)
                {
                    triggerKey = Guid.Empty;
                    count = 0;
                }
            });
            UnlockForceCommand.Subscribe(() =>
            {
                locker.ReleaseAllForce();
                key = Guid.Empty;
                triggerKey = Guid.Empty;
                count = 0;
            });
            WaitUnlockTriggerDemoCommand.Subscribe(async () =>
            {
                var locked = locker.Lock(3);
                if (locked.result)
                {
                    triggerKey = locked.key;
                    Log.Value = "Locked.";
                    await locker.WaitReleaseAsync();
                    Log.Value = "Unlocked.";
                }
            });
        }
    }
}

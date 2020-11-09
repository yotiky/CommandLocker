using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLocker
{
    public class CommandLocker<T> where T : IEquatable<T>
    {
        public ReactiveProperty<bool> CanExecute { get; }
        private ReactiveProperty<bool> canExcuteWithTrigger { get; }
        public ReactiveProperty<bool> CanExecuteWithTrigger { get; }

        private Guid lockKey;
        private object lockObject = new object();

        private Guid lockTriggerKey;
        private T releaseTrigger;

        public CommandLocker()
        {
            CanExecute = new ReactiveProperty<bool>(true);
            canExcuteWithTrigger = new ReactiveProperty<bool>(true);
            CanExecuteWithTrigger = CanExecute
                .CombineLatest(canExcuteWithTrigger, (a, b) => a && b)
                .ToReactiveProperty();
        }

        public (bool result, Guid key) Lock()
        {
            if (lockKey != Guid.Empty) { return (false, Guid.Empty); }

            lock (lockObject)
            {
                CanExecute.Value = false;
                lockKey = Guid.NewGuid();
            }
            return (true, lockKey);
        }
        public bool Release(Guid key)
        {
            if (lockKey != key) { return false; }

            lock (lockObject)
            {
                CanExecute.Value = true;
                lockKey = Guid.Empty;
            }
            return true;
        }

        public (bool, Guid) Lock(T trigger)
        {
            if (lockTriggerKey != Guid.Empty) { return (false, Guid.Empty); }

            lock (lockObject)
            {
                canExcuteWithTrigger.Value = false;
                lockTriggerKey = Guid.NewGuid();
                releaseTrigger = trigger;
            }
            return (true, lockTriggerKey);
        }
        public bool TryRelease(T value, Guid? key = null)
        {
            if (lockTriggerKey == Guid.Empty) { return false; }
            if (!releaseTrigger.Equals(value)) { return false; }
            if (key != null && lockTriggerKey != key) { return false; }

            lock (lockObject)
            {
                canExcuteWithTrigger.Value = true;
                lockTriggerKey = Guid.Empty;
            }
            return true;
        }

        public async Task WaitReleaseAsync()
        {
            if (canExcuteWithTrigger.Value) { return; }

            var result = false;
            while (!result)
            {
                result = await canExcuteWithTrigger;
            }

        }
        public void ReleaseForce()
        {
            lock (lockObject)
            {
                CanExecute.Value = true;
                canExcuteWithTrigger.Value = true;
                lockKey = Guid.Empty;
                lockTriggerKey = Guid.Empty;
            }
        }
    }
}

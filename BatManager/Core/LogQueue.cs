using System;
using System.Collections.Generic;
using System.Threading;

namespace BatManager.Core {
    public class LogQueue<T> {
        private Queue<T> _internalQueue;
        
        public int Capacity { get; }
        public int Count => _internalQueue.Count;
        
        public int Pushes { get; private set; }
        public int Pops { get; private set; }
        
        public LogQueue(int capacity) {
            Capacity = capacity;
            _internalQueue = new Queue<T>();
            if (capacity <= 0) throw new ArgumentException(nameof(capacity));
        }

        public void Push(T value) {
            while (_internalQueue.Count >= Capacity) {
                Dequeue();
            }
 
            Enqueue(value);
        }

        public T Pop() {
            if (_internalQueue.Count == 0)
                throw new InvalidOperationException("Queue is empty");
            return Dequeue();
        }

        public void ClearStats() {  }
        
        public void Lock() { Monitor.Enter(this); }
        
        public void Unlock() { Monitor.Exit(this); }
        
        private void Enqueue(T t) {
            _internalQueue.Enqueue(t);
        }
        
        private T Dequeue() {
            return _internalQueue.Dequeue();
        }
    }
}
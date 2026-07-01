using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Conkist.GDK.Services.Conkist
{
    /// <summary>
    /// Thread-safe event buffer for queuing telemetry events before batch transmission.
    /// Events are enqueued from gameplay code and drained by the flush loop in ConkistBackendService.
    /// </summary>
    public class ConkistTelemetryBuffer
    {
        /// <summary>
        /// Maximum number of events to include in a single batch send.
        /// </summary>
        public int MaxBatchSize { get; set; } = 10;

        /// <summary>
        /// Maximum number of events to hold in memory before dropping the oldest.
        /// Prevents unbounded memory growth when offline.
        /// </summary>
        public int MaxCapacity { get; set; } = 1000;

        private readonly ConcurrentQueue<TelemetryEvent> _queue = new ConcurrentQueue<TelemetryEvent>();
        private int _count;

        /// <summary>
        /// Current number of events in the buffer.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Returns true if the buffer has no events.
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Enqueues a telemetry event. If the buffer is at max capacity,
        /// the oldest event is dropped to make room.
        /// </summary>
        public void Enqueue(TelemetryEvent evt)
        {
            // Drop oldest if at capacity
            while (_count >= MaxCapacity)
            {
                if (_queue.TryDequeue(out _))
                {
                    System.Threading.Interlocked.Decrement(ref _count);
                }
                else break;
            }

            _queue.Enqueue(evt);
            System.Threading.Interlocked.Increment(ref _count);
        }

        /// <summary>
        /// Drains up to MaxBatchSize events from the queue for batch transmission.
        /// </summary>
        /// <returns>A list of events to send, up to MaxBatchSize.</returns>
        public List<TelemetryEvent> Drain()
        {
            var batch = new List<TelemetryEvent>();
            int drained = 0;

            while (drained < MaxBatchSize && _queue.TryDequeue(out var evt))
            {
                batch.Add(evt);
                System.Threading.Interlocked.Decrement(ref _count);
                drained++;
            }

            return batch;
        }

        /// <summary>
        /// Re-enqueues a batch of events at the front of the queue.
        /// Used when a telemetry send fails and events need to be retried.
        /// Note: ConcurrentQueue doesn't support prepend, so events are re-enqueued 
        /// at the end. Order within a retry batch is preserved but they will be 
        /// sent after any events enqueued during the retry delay.
        /// </summary>
        public void ReEnqueue(List<TelemetryEvent> events)
        {
            foreach (var evt in events)
            {
                _queue.Enqueue(evt);
                System.Threading.Interlocked.Increment(ref _count);
            }
        }

        /// <summary>
        /// Clears all events from the buffer.
        /// </summary>
        public void Clear()
        {
            while (_queue.TryDequeue(out _))
            {
                System.Threading.Interlocked.Decrement(ref _count);
            }
        }
    }
}

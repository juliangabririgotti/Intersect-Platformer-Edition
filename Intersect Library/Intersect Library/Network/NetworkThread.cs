﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Intersect.Logging;
using Intersect.Threading;

namespace Intersect.Network
{
    public sealed class NetworkThread
    {
        private bool mStarted;
        private readonly IThreadYield mThreadYield;
        private readonly PacketDispatcher mDispatcher;

        public string Name { get; }

        public Thread CurrentThread { get; }
        public PacketQueue Queue { get; }
        public IList<IConnection> Connections { get; }

        public bool IsRunning { get; private set; }
        
        public NetworkThread(PacketDispatcher dispatcher, IThreadYield yield, string name = null)
        {
            mThreadYield = yield ?? new ThreadYieldNet35();
            Name = name ?? "Network Worker Thread";
            CurrentThread = new Thread(Loop);
            Queue = new PacketQueue();
            mDispatcher = dispatcher;
            Connections = new List<IConnection>();
        }

        public void Start()
        {
            lock (this)
            {
                if (mStarted) return;
                mStarted = true;
                IsRunning = true;
            }

            CurrentThread.Start();
        }

        public void Stop()
        {
            lock (this)
            {
                IsRunning = false;
            }

            Queue?.Interrupt();
        }

        private void Loop()
        {
            var sw = new Stopwatch();
            var last = 0L;
            sw.Start();
            while (IsRunning)
            {
                // ReSharper disable once PossibleNullReferenceException
                if (!Queue.TryNext(out IPacket packet)) continue;

                //Log.Debug($"Dispatching packet '{packet.GetType().Name}' (size={(packet as BinaryPacket)?.Buffer?.Length() ?? -1}).");
                if (!(mDispatcher?.Dispatch(packet) ?? false))
                {
                    Log.Warn($"Failed to dispatch packet '{packet}'.");
                }

#if DIAGNOSTIC
                if (last + (1 * TimeSpan.TicksPerSecond) < sw.ElapsedTicks)
                {
                    last = sw.ElapsedTicks;
                    Console.Title = $"Queue size: {Queue.Size}";
                }
#endif

                packet.Dispose();

                mThreadYield?.Yield();
            }
            sw.Stop();

            Log.Debug($"Exiting network thread ({Name}).");
        }
    }
}
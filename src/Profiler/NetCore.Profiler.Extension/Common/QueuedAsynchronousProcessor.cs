/*
 * Copyright 2017 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * 	http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NetCore.Profiler.Extension.Common
{
    public class QueuedAsynchronousProcessor<TElement> : IDisposable
    {
        public QueuedAsynchronousProcessor(Action<TElement> elementProcessor)
        {
            _queue = new ConcurrentQueue<TElement>();
            _elementProcessor = elementProcessor;
            _stopEvent = new ManualResetEvent(false);
            _wakeUpEvent = new AutoResetEvent(false);
            _task = new Task(Process);
            _task.Start();
        }

        public void Dispose()
        {
            Task task = Interlocked.Exchange(ref _task, null);
            if (task != null)
            {
                _stopEvent.Set();
            }
        }

        public int GetCount()
        {
            return _queue.Count;
        }

        public void Enqueue(TElement element)
        {
            _queue.Enqueue(element);
            _wakeUpEvent.Set();
        }

        private void Process()
        {
            try
            {
                Loop();
                _stopEvent.Dispose();
                _wakeUpEvent.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format($"Error in {GetType().Name}. {ex.Message}"));
            }
        }

        private void Loop()
        {
            WaitHandle[] handles = new[] { _stopEvent, _wakeUpEvent };
            while (true)
            {
                int waitResult = WaitHandle.WaitAny(handles);
                if (waitResult == 0) // stop set
                {
                    return;
                }
                // wake-up
                TElement @event;
                while (_queue.TryDequeue(out @event))
                {
                    if (_stopEvent.WaitOne(0))
                    {
                        return;
                    }
                    _elementProcessor(@event);
                }
            }
        }

        private ConcurrentQueue<TElement> _queue;

        private Action<TElement> _elementProcessor;

        private EventWaitHandle _stopEvent;

        private EventWaitHandle _wakeUpEvent;

        private Task _task;
    }
}

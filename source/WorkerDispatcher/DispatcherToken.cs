﻿//#define TRACE_STOP
using System;
using System.Threading;
using System.Threading.Tasks;
using WorkerDispatcher.Workers;

namespace WorkerDispatcher
{
    public class DispatcherToken : IDispatcherToken
    {
        private readonly IQueueWorker _queueWorker;
        private readonly ICounterBlocked _processCount;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ActionDispatcherSettings _actionDispatcherSettings;
        private readonly ICounterBlocked _chainCounter;
        private readonly IWorkerHandler _workerHandler;

        internal DispatcherToken(ICounterBlocked counterProcess,
            IQueueWorker queueWorker,
            ActionDispatcherSettings actionDispatcherSettings,
            CancellationTokenSource cancellationTokenSource,
            ICounterBlocked chainCounterBlocked,
            IWorkerHandler workerHandler)
        {
            _processCount = counterProcess;

            _chainCounter = chainCounterBlocked;
            _workerHandler = workerHandler;
            _queueWorker = queueWorker;

            _cancellationTokenSource = cancellationTokenSource;

            _actionDispatcherSettings = actionDispatcherSettings;
        }

        public int ProcessCount => _processCount.Count;

        public int ProcessLimit => _actionDispatcherSettings.PrefetchCount;

        public int QueueProcessCount => _queueWorker.Count;

        public IDispatcherPlugin Plugin
        {
            get
            {
                return new DefaultDispathcerPlugin(this, _workerHandler);
            }
        }

        public void Post(IActionInvoker actionInvoker)
        {
            if (actionInvoker == null)
            {
                throw new ArgumentNullException(nameof(actionInvoker));
            }

            _queueWorker.Post(actionInvoker);
        }

        public void Post(Func<CancellationToken, Task> fn)
        {
            if (fn == null)
            {
                throw new ArgumentNullException(nameof(fn));
            }

            _queueWorker.Post(new InternalWorkerFunc(fn));
        }

        public void Post<TData>(IActionInvoker<TData> actionInvoker, TData data)
        {
            if (actionInvoker == null)
            {
                throw new ArgumentNullException(nameof(actionInvoker));
            }

            _queueWorker.Post(new InternalWorkerValue<TData>(actionInvoker, data));
        }

        public void Post<TData>(IActionInvoker<TData> actionInvoker, TData data, TimeSpan lifetime)
        {
            if (actionInvoker == null)
            {
                throw new ArgumentNullException(nameof(actionInvoker));
            }

            _queueWorker.Post(new InternalWorkerValueLifetime<TData>(actionInvoker, data, lifetime));
        }

        public IWorkerChain Chain()
        {
            return new DefaultWorkerChain(_queueWorker, _chainCounter);
        }

        public async Task Stop(int timeoutSeconds = 60)
        {
            await Task.Yield();

            WaitCompleted(timeoutSeconds);
        }

        public void WaitCompleted(int timeoutSeconds = 60)
        {
            var chainSec = new TimeSpan(0, 0, timeoutSeconds);
            var chainLimitTime = new TimeSpan(DateTime.Now.Add(chainSec).Ticks);

            _chainCounter.Wait((int)chainSec.TotalMilliseconds);

            _queueWorker.Complete();

            _cancellationTokenSource.Cancel();

            var chainCurrentTime = new TimeSpan(DateTime.Now.Ticks);
            var chainDelta = chainLimitTime - chainCurrentTime;

            var timeout = new TimeSpan(0, 0, chainDelta <= TimeSpan.Zero ? 1 : (int)chainDelta.TotalMilliseconds);

            var limitTime = new TimeSpan(DateTime.Now.Add(timeout).Ticks);

            _queueWorker.WaitCompleted((int)timeout.TotalMilliseconds);

#if TRACE_STOP
			Trace.WriteLine("queue completed");
#endif
            var currentTime = new TimeSpan(DateTime.Now.Ticks);
            var delta = limitTime - currentTime;

            if (delta > TimeSpan.Zero)
            {
#if TRACE_STOP
				Trace.WriteLine($"stop process with {(int)delta.TotalMilliseconds}, sec: {(int)delta.TotalSeconds}");
#endif
                _processCount.Wait((int)delta.TotalMilliseconds);
#if TRACE_STOP
				Trace.WriteLine("process completed");
#endif
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cancellationTokenSource.Cancel();

                    _queueWorker.Dispose();

                    _cancellationTokenSource.Dispose();

                    _processCount.Dispose();

                    _chainCounter.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

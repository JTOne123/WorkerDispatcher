﻿using System.Threading;
using System.Threading.Tasks;

namespace WorkerDispatcher.Workers
{
	internal class InternalWorkerValue<TData> : IActionInvoker, IActionInvokerData
    {
		private readonly TData _data;
		private readonly IActionInvoker<TData> _actionInvoker;

        public object Data { get; }

		public InternalWorkerValue(IActionInvoker<TData> actionInvoker, TData data)
		{
			_data = data;
			_actionInvoker = actionInvoker;
            Data = data;
		}

		public virtual async Task<object> Invoke(CancellationToken token)
		{
			return await _actionInvoker.Invoke(_data, token);
		}

        public override string ToString()
        {
            return _actionInvoker.ToString();
        }
    }

    internal interface IActionInvokerData
    {
        object Data { get; }
    }
}

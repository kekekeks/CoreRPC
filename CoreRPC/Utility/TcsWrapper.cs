using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncRpc.Utility
{
	internal interface ITaskCompletionSource
	{
		Task Task { get; }
		void SetResultOrCastException(object obj);
		void SetException(Exception e);
	}

	internal class TcsWrapper<T> : TaskCompletionSource<T>, ITaskCompletionSource
	{
		Task ITaskCompletionSource.Task
		{
			get { return Task; }
		}

		void ITaskCompletionSource.SetResultOrCastException(object obj)
		{
			T data;
			try
			{
				data = (T) obj;
			}
			catch (Exception e)
			{
				SetException(e);
				return;
			}
			SetResult(data);
		}
	}
}

using System;
using System.Collections.Generic;

namespace AsyncRpc.Utility
{
	public class ConcurentBulkReadOptimizedCache<TKey, TValue>
	{
		readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue> ();
		readonly Func<TKey, TValue> _getter;
		public ConcurentBulkReadOptimizedCache (Func<TKey, TValue> getter)
		{
			_getter = getter;
		}

		Dictionary<TKey, TValue> GetSnapshot()
		{
			lock (_dictionary)
				return new Dictionary<TKey, TValue> (_dictionary);
		}

		Dictionary<TKey, TValue> GetUpdatedSnapshot (TKey missing)
		{
			lock (_dictionary)
			{
				if (!_dictionary.ContainsKey (missing))
					_dictionary[missing] = _getter (missing);
				return new Dictionary<TKey, TValue> (_dictionary);
			}
		}

		public class CacheContext
		{
			readonly ConcurentBulkReadOptimizedCache<TKey, TValue> _parent;
			Dictionary<TKey, TValue> _snapshot;

			public CacheContext (ConcurentBulkReadOptimizedCache<TKey, TValue> parent)
			{
				_parent = parent;
				_snapshot = _parent.GetSnapshot ();
			}

			public TValue this[TKey key]
			{
				get
				{
					TValue rv;
					if (_snapshot.TryGetValue (key, out rv))
						return rv;
					_snapshot = _parent.GetUpdatedSnapshot (key);
					return _snapshot[key];
				}
			}
		}

		public CacheContext GetContext ()
		{
			return new CacheContext (this);
		}

	}
}

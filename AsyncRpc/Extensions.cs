using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncRpc
{
	internal static class Extensions
	{
		public static async Task<byte[]> ReadExactlyAsync(this Stream stream, int size)
		{
			var ms = new MemoryStream(size);
			var buffer = new byte[8192];
			while (size > 0)
			{
				var read = await stream.ReadAsync(buffer, 0, Math.Min(size, buffer.Length));
				if (read == 0)
					throw new EndOfStreamException();
				ms.Write(buffer, 0, read);
				size -= read;
			}
			return ms.ToArray();
		}


	}
}

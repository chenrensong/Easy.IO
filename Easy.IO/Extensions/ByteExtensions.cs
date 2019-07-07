using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Easy.IO
{
    public static class ByteExtensions
    {
        public static byte[] Copy(this byte[] bytes)
        {
            byte[] newBytes = new byte[bytes.Length];
            bytes.CopyTo(newBytes, 0);
            return newBytes;
        }

        public static byte[] Copy(this byte[] bytes, int offset, int length)
        {
            byte[] newBytes = new byte[length];
            Array.Copy(bytes, offset, newBytes, 0, length);
            return newBytes;
        }


        /// <summary>Writes a 32-bit integer in a compressed format.</summary>
        /// <param name="value">The 32-bit integer to be written.</param>
        public static void Write7BitEncodedInt(this Stream stream, int value)
        {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            uint v = (uint)value;   // support negative numbers
            while (v >= 0x80)
            {
                stream.WriteByte((byte)(v | 0x80));
                v >>= 7;
            }
            stream.WriteByte((byte)v);
        }

        public static int Read(this Stream stream, Span<byte> destination)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(destination.Length);
            try
            {
                int numRead = stream.Read(buffer, 0, destination.Length);
                if ((uint)numRead > destination.Length)
                {
                    throw new IOException("Stream was too long.");
                }
                new Span<byte>(buffer, 0, numRead).CopyTo(destination);
                return numRead;
            }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
        }

        public static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> destination, CancellationToken cancellationToken = default)
        {

            if (MemoryMarshal.TryGetArray(destination, out ArraySegment<byte> array))
            {
                return new ValueTask<int>(stream.ReadAsync(array.Array, array.Offset, array.Count, cancellationToken));
            }
            else
            {
                if (stream is MemoryStream ms)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
                    }

                    try
                    {
                        // ReadAsync(Memory<byte>,...) needs to delegate to an existing virtual to do the work, in case an existing derived type
                        // has changed or augmented the logic associated with reads.  If the Memory wraps an array, we could delegate to
                        // ReadAsync(byte[], ...), but that would defeat part of the purpose, as ReadAsync(byte[], ...) often needs to allocate
                        // a Task<int> for the return value, so we want to delegate to one of the synchronous methods.  We could always
                        // delegate to the Read(Span<byte>) method, and that's the most efficient solution when dealing with a concrete
                        // MemoryStream, but if we're dealing with a type derived from MemoryStream, Read(Span<byte>) will end up delegating
                        // to Read(byte[], ...), which requires it to get a byte[] from ArrayPool and copy the data.  So, we special-case the
                        // very common case of the Memory<byte> wrapping an array: if it does, we delegate to Read(byte[], ...) with it,
                        // as that will be efficient in both cases, and we fall back to Read(Span<byte>) if the Memory<byte> wrapped something
                        // else; if this is a concrete MemoryStream, that'll be efficient, and only in the case where the Memory<byte> wrapped
                        // something other than an array and this is a MemoryStream-derived type that doesn't override Read(Span<byte>) will
                        // it then fall back to doing the ArrayPool/copy behavior.
                        return new ValueTask<int>(
                            MemoryMarshal.TryGetArray(destination, out ArraySegment<byte> destinationArray) ?
                                ms.Read(destinationArray.Array, destinationArray.Offset, destinationArray.Count) :
                                ms.Read(destination.Span));
                    }
                    // TODO
                    //catch (OperationCanceledException oce)
                    //{
                    //  return new ValueTask<int>(Task.FromCancellation<int>(oce));
                    //}
                    catch (Exception exception)
                    {
                        return new ValueTask<int>(Task.FromException<int>(exception));
                    }
                }
                else
                {
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(destination.Length);
                    return FinishReadAsync(stream.ReadAsync(buffer, 0, destination.Length, cancellationToken), buffer, destination);

                    async ValueTask<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
                    {
                        try
                        {
                            int result = await readTask.ConfigureAwait(false);
                            new Span<byte>(localBuffer, 0, result).CopyTo(localDestination.Span);
                            return result;
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(localBuffer);
                        }
                    }
                }
            }
        }

        public static void Write(this Stream stream, ReadOnlySpan<byte> source)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(source.Length);
            try
            {
                source.CopyTo(buffer);
                stream.Write(buffer, 0, source.Length);
            }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
        }

        public static Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(source, out ArraySegment<byte> array))
            {
                return stream.WriteAsync(array.Array, array.Offset, array.Count, cancellationToken);
            }
            else
            {
                if (stream is MemoryStream ms)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Task.FromCanceled(cancellationToken);
                    }

                    try
                    {
                        // See corresponding comment in ReadAsync for why we don't just always use Write(ReadOnlySpan<byte>).
                        // Unlike ReadAsync, we could delegate to WriteAsync(byte[], ...) here, but we don't for consistency.
                        if (MemoryMarshal.TryGetArray(source, out ArraySegment<byte> sourceArray))
                        {
                            ms.Write(sourceArray.Array, sourceArray.Offset, sourceArray.Count);
                        }
                        else
                        {
                            ms.Write(source.Span);
                        }
                        return Task.CompletedTask;
                    }
                    // TODO
                    //catch (OperationCanceledException oce)
                    //{
                    //  return Task.FromCancellation<VoidTaskResult>(oce);
                    //}
                    catch (Exception exception)
                    {
                        return Task.FromException(exception);
                    }
                }
                else
                {
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(source.Length);
                    source.Span.CopyTo(buffer);
                    return FinishWriteAsync(stream.WriteAsync(buffer, 0, source.Length, cancellationToken), buffer);

                    async Task FinishWriteAsync(Task writeTask, byte[] localBuffer)
                    {
                        try
                        {
                            await writeTask.ConfigureAwait(false);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(localBuffer);
                        }
                    }
                }
            }
        }


    }
}

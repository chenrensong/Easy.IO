﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    public class SegmentPool
    {
        private static object obj = new object();
        /** The maximum number Of bytes to pool. */
        // TODO: Is 64 KiB a good maximum size? Do we ever have that many idle segments?
        const long MAX_SIZE = 64 * 1024; // 64 KiB.

        /** Singly-linked list Of segments. */
        static Segment next;

        /** Total bytes in this pool. */
        static long byteCount;

        private SegmentPool()
        {
        }

        public static Segment Take()
        {
            lock (obj)
            {
                if (next != null)
                {
                    var result = next;
                    next = result.next;
                    result.next = null;
                    byteCount -= Segment.SIZE;
                    return result;
                }
            }
            return new Segment(); // Pool is empty. Don't zero-fill while holding a lock.
        }

        public static void Recycle(Segment segment)
        {
            if (segment.next != null || segment.prev != null)
            {
                throw new IllegalArgumentException();
            }
            if (segment.shared)
            {
                return; // This segment cannot be recycled.
            }
            lock (obj)
            {
                if (byteCount + Segment.SIZE > MAX_SIZE)
                {
                    return; // Pool is full.
                }
                byteCount += Segment.SIZE;
                segment.next = next;
                segment.pos = segment.limit = 0;
                next = segment;
            }
        }
    }
}
//using Easy.IO;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using System.Threading;

//namespace Easy.IO
//{
//    public class AsyncTimeout : Timeout
//    {
//        /**
//     * Don't write more than 64 KiB Of data at a time, give or take a segment. Otherwise slow
//     * connections may suffer timeouts even when they're making (slow) progress. Without this, writing
//     * a single 1 MiB buffer may never succeed on a sufficiently slow connection.
//     */
//        private static int TIMEOUT_WRITE_SIZE = 64 * 1024;

//        /** Duration for the watchdog thread to be idle before it shuts itself down. */
//        private static long IDLE_TIMEOUT_MILLIS = TimeUnit.SECONDS.ToMillis(60);
//        private static long IDLE_TIMEOUT_NANOS = TimeUnit.MILLISECONDS.ToNanos(IDLE_TIMEOUT_MILLIS);

//        /**
//         * The watchdog thread processes a linked list Of pending timeouts, sorted in the order to be
//         * triggered. This class synchronizes on AsyncTimeout.class. This lock guards the queue.
//         *
//         * <p>Head's 'next' points to the first element Of the linked list. The first element is the next
//         * node to time out, or null if the queue is empty. The head is null until the watchdog thread is
//         * started and also after being idle for {@link #IDLE_TIMEOUT_MILLIS}.
//         */
//        static AsyncTimeout head;

//        /** True if this node is currently in the queue. */
//        private bool inQueue;

//        /** The next node in the linked list. */
//        private AsyncTimeout next;

//        /** If scheduled, this is the time that the watchdog should time this out. */
//        private long timeoutAt;

//        public void enter()
//        {
//            if (inQueue) throw new IllegalStateException("Unbalanced enter/exit");
//            long timeoutNanos = base.TimeoutNanos;
//            bool hasDeadline = base.HasDeadline;
//            if (timeoutNanos == 0 && !hasDeadline)
//            {
//                return; // No timeout and no deadline? Don't bother with the queue.
//            }
//            inQueue = true;
//            scheduleTimeout(this, timeoutNanos, hasDeadline);
//        }

//        private static void scheduleTimeout(
//            AsyncTimeout node, long timeoutNanos, bool hasDeadline)
//        {
//            // Start the watchdog thread and create the head node when the first timeout is scheduled.
//            if (head == null)
//            {
//                head = new AsyncTimeout();
//                //new Watchdog().start();
//            }

//            long now = System.nanoTime();
//            if (timeoutNanos != 0 && hasDeadline)
//            {
//                // Compute the earliest event; either timeout or deadline. Because nanoTime can wrap around,
//                // Math.min() is undefined for absolute values, but meaningful for relative ones.
//                node.timeoutAt = now + Math.Min(timeoutNanos, node.DeadlineNanoTime - now);
//            }
//            else if (timeoutNanos != 0)
//            {
//                node.timeoutAt = now + timeoutNanos;
//            }
//            else if (hasDeadline)
//            {
//                node.timeoutAt = node.DeadlineNanoTime;
//            }
//            else
//            {
//                throw new AssertionException();
//            }

//            // Insert the node in sorted order.
//            long remainingNanos = node.remainingNanos(now);
//            for (AsyncTimeout prev = head; true; prev = prev.next)
//            {
//                if (prev.next == null || remainingNanos < prev.next.remainingNanos(now))
//                {
//                    node.next = prev.next;
//                    prev.next = node;
//                    if (prev == head)
//                    {
//                        //lock.notify();
//                        //AsyncTimeout.class.notify(); // Wake up the watchdog when inserting at the front.
//                    }
//                    break;
//                }
//            }
//        }

//        /** Returns true if the timeout occurred. */
//        public bool exit()
//        {
//            if (!inQueue) return false;
//            inQueue = false;
//            return cancelScheduledTimeout(this);
//        }

//        /** Returns true if the timeout occurred. */
//        private static bool cancelScheduledTimeout(AsyncTimeout node)
//        {
//            // Remove the node from the linked list.
//            for (AsyncTimeout prev = head; prev != null; prev = prev.next)
//            {
//                if (prev.next == node)
//                {
//                    prev.next = node.next;
//                    node.next = null;
//                    return false;
//                }
//            }

//            // The node wasn't found in the linked list: it must have timed out!
//            return true;
//        }

//        /**
//         * Returns the amount Of time left until the time out. This will be negative if the timeout has
//         * elapsed and the timeout should occur immediately.
//         */
//        private long remainingNanos(long now)
//        {
//            return timeoutAt - now;
//        }

//        /**
//         * Invoked by the watchdog thread when the time between calls to {@link #enter()} and {@link
//         * #exit()} has exceeded the timeout.
//         */
//        protected void timedOut()
//        {
//        }

//        /**
//         * Returns a new sink that delegates to {@code sink}, using this to implement timeouts. This works
//         * best if {@link #timedOut} is overridden to interrupt {@code sink}'s current operation.
//         */
//        public Sink sink(Sink sink)
//        {
//            return new Sink()
//            {
//             public void write(Buffer source, long byteCount)
//            {
//                checkOffsetAndCount(source.size, 0, byteCount);

//                while (byteCount > 0L)
//                {
//                    // Count how many bytes to write. This loop guarantees we split on a segment boundary.
//                    long toWrite = 0L;
//                    for (Segment s = source.head; toWrite < TIMEOUT_WRITE_SIZE; s = s.next)
//                    {
//                        int segmentSize = s.limit - s.pos;
//                        toWrite += segmentSize;
//                        if (toWrite >= byteCount)
//                        {
//                            toWrite = byteCount;
//                            break;
//                        }
//                    }

//                    // Emit one write. Only this section is subject to the timeout.
//                    bool throwOnTimeout = false;
//                    enter();
//                    try
//                    {
//                        sink.write(source, toWrite);
//                        byteCount -= toWrite;
//                        throwOnTimeout = true;
//                    }
//                    catch (IOException e)
//                    {
//                        throw exit(e);
//                    }
//                    finally
//                    {
//                        exit(throwOnTimeout);
//                    }
//                }
//            }

//            public void flush()  {
//                bool throwOnTimeout = false;
//                enter();
//                try
//                {
//                    sink.flush();
//                    throwOnTimeout = true;
//                }
//                catch (IOException e)
//                {
//                    throw exit(e);
//                }
//                finally
//                {
//                    exit(throwOnTimeout);
//                }
//            }

//            public void close()  {
//                bool throwOnTimeout = false;
//                enter();
//                try
//                {
//                    sink.close();
//                    throwOnTimeout = true;
//                }
//                catch (IOException e)
//                {
//                    throw exit(e);
//                }
//                finally
//                {
//                    exit(throwOnTimeout);
//                }
//            }

//            public Timeout timeout()
//            {
//                return AsyncTimeout.this;
//            }

//            public string toString()
//            {
//                return "AsyncTimeout.sink(" + sink + ")";
//            }
//        };
//    }

//    /**
//     * Returns a new source that delegates to {@code source}, using this to implement timeouts. This
//     * works best if {@link #timedOut} is overridden to interrupt {@code sink}'s current operation.
//     */
//    public Source source(Source source)
//    {
//        return new Source()
//        {
//       public long read(Buffer sink, long byteCount) {
//            bool throwOnTimeout = false;
//            enter();
//            try
//            {
//                long result = source.read(sink, byteCount);
//                throwOnTimeout = true;
//                return result;
//            }
//            catch (IOException e)
//            {
//                throw exit(e);
//            }
//            finally
//            {
//                exit(throwOnTimeout);
//            }
//        }

//        public void close() {
//            bool throwOnTimeout = false;
//            try
//            {
//                source.close();
//                throwOnTimeout = true;
//            }
//            catch (IOException e)
//            {
//                throw exit(e);
//            }
//            finally
//            {
//                exit(throwOnTimeout);
//            }
//        }

//        public Timeout timeout()
//        {
//            return AsyncTimeout.this;
//        }

//        public string toString()
//        {
//            return "AsyncTimeout.source(" + source + ")";
//        }
//    };
//}

///**
// * Throws an IOException if {@code throwOnTimeout} is {@code true} and a timeout occurred. See
// * {@link #newTimeoutException(java.io.IOException)} for the type Of exception thrown.
// */
//void exit(bool throwOnTimeout)
//{
//    bool timedOut = exit();
//    if (timedOut && throwOnTimeout) throw newTimeoutException(null);
//}

///**
// * Returns either {@code cause} or an IOException that's caused by {@code cause} if a timeout
// * occurred. See {@link #newTimeoutException(java.io.IOException)} for the type Of exception
// * returned.
// */
//IOException exit(IOException cause)
//{
//    if (!exit()) return cause;
//    return newTimeoutException(cause);
//}

///**
// * Returns an {@link IOException} to represent a timeout. By default this method returns {@link
// * java.io.InterruptedIOException}. If {@code cause} is non-null it is set as the cause Of the
// * returned exception.
// */
//protected IOException newTimeoutException(IOException cause)
//{
//    InterruptedIOException e = new InterruptedIOException("timeout");
//    if (cause != null)
//    {
//        e.initCause(cause);
//    }
//    return e;
//}

//private static class Watchdog : Thread
//{
//    Watchdog()
//    {
//        super("Okio Watchdog");
//        setDaemon(true);
//    }

//    public void run()
//    {
//        while (true)
//        {
//            try
//            {
//                AsyncTimeout timedOut;
//                synchronized(AsyncTimeout.class) {
//            timedOut = awaitTimeout();

//            // Didn't find a node to interrupt. Try again.
//            if (timedOut == null) continue;

//            // The queue is completely empty. Let this thread exit and let another watchdog thread
//            // get created on the next call to scheduleTimeout().
//            if (timedOut == head) {
//              head = null;
//              return;
//            }
//          }

//          // Close the timed out node.
//          timedOut.timedOut();
//        } catch (InterruptedException ignored) {
//        }
//      }
//    }
//  }

//  /**
//   * Removes and returns the node at the head Of the list, waiting for it to time out if necessary.
//   * This returns {@link #head} if there was no node at the head Of the list when starting, and
//   * there continues to be no node after waiting {@code IDLE_TIMEOUT_NANOS}. It returns null if a
//   * new node was inserted while waiting. Otherwise this returns the node being waited on that has
//   * been removed.
//   */
//  static AsyncTimeout awaitTimeout()
//{
//    // Get the next eligible node.
//    AsyncTimeout node = head.next;

//    // The queue is empty. Wait until either something is enqueued or the idle timeout elapses.
//    if (node == null)
//    {
//        long startNanos = System.nanoTime();
//        AsyncTimeout.class.wait(IDLE_TIMEOUT_MILLIS);
//      return head.next == null && (System.nanoTime() - startNanos) >= IDLE_TIMEOUT_NANOS
//          ? head  // The idle timeout elapsed.
//          : null; // The situation has changed.
//    }

//    long waitNanos = node.remainingNanos(System.nanoTime());

//    // The head Of the queue hasn't timed out yet. Await that.
//    if (waitNanos > 0) {
//      // Waiting is made complicated by the fact that we work in nanoseconds,
//      // but the API wants (millis, nanos) in two arguments.
//      long waitMillis = waitNanos / 1000000L;
//waitNanos -= (waitMillis* 1000000L);
//      AsyncTimeout.class.wait(waitMillis, (int) waitNanos);
//      return null;
//    }

//    // The head Of the queue has timed out. Remove it.
//    head.next = node.next;
//    node.next = null;
//    return node;
//  }
//    }
//}

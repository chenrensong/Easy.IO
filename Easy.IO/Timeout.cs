using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Easy.IO
{


    public class Timeout
    {

        /**
         * True if {@code deadlineNanoTime} is defined. There is no equivalent to null
         * or 0 for {@link System#nanoTime}.
         */
        private bool hasDeadline;
        private long deadlineNanoTime;
        private long timeoutNanos;
        public static Timeout NONE = new NoneTimeout();

        public Timeout()
        {
        }

        /**
         * Wait at most {@code timeout} time before aborting an operation. Using a
         * per-operation timeout means that as long as forward progress is being made,
         * no sequence Of operations will fail.
         *
         * <p>If {@code timeout == 0}, operations will run indefinitely. (Operating
         * system timeouts may still apply.)
         */
        public virtual Timeout SetTimeout(long timeout, TimeUnit unit)
        {
            if (timeout < 0) throw new IllegalArgumentException("timeout < 0: " + timeout);
            if (unit == default) throw new IllegalArgumentException("unit == null");
            this.timeoutNanos = unit.ToNanos(timeout);
            return this;
        }

        /** Returns the timeout in nanoseconds, or {@code 0} for no timeout. */
        public long TimeoutNanos
        {
            get
            {
                return timeoutNanos;
            }
        }

        /** Returns true if a deadline is enabled. */
        public bool HasDeadline
        {
            get
            {
                return hasDeadline;
            }
        }


        public long DeadlineNanoTime()
        {
            if (!hasDeadline) throw new IllegalStateException("No deadline");
            return deadlineNanoTime;
        }

        public virtual Timeout DeadlineNanoTime(long deadlineNanoTime)
        {
            this.hasDeadline = true;
            this.deadlineNanoTime = deadlineNanoTime;
            return this;
        }



        /** Set a deadline Of now plus {@code duration} time. */
        public Timeout Deadline(long duration, TimeUnit unit)
        {
            if (duration <= 0) throw new IllegalArgumentException("duration <= 0: " + duration);
            if (unit == default) throw new IllegalArgumentException("unit == null");
            var value = System.NanoTime() + unit.ToNanos(duration);
            deadlineNanoTime = value;
            return this;
        }

        /** Clears the timeout. Operating system timeouts may still apply. */
        public Timeout ClearTimeout()
        {
            this.timeoutNanos = 0;
            return this;
        }

        /** Clears the deadline. */
        public Timeout ClearDeadline()
        {
            this.hasDeadline = false;
            return this;
        }

        public static void EnterUninterruptibly(object mon, out bool wasInterrupted)
        {
            wasInterrupted = false;
            while (true)
            {
                try
                {
                    Monitor.Enter(mon);
                    return;
                }
                catch (ThreadInterruptedException e)
                {
                    wasInterrupted = true;
                }
            }
        }


        /**
         * Throws an {@link InterruptedIOException} if the deadline has been reached or if the current
         * thread has been interrupted. This method doesn't detect timeouts; that should be implemented to
         * asynchronously abort an in-progress operation.
         */
        public virtual void ThrowIfReached()
        {
            try
            {
                Thread.CurrentThread.Interrupt(); // Retain interrupted status.
            }
            catch (Exception ex)
            {
                throw new InterruptedIOException("interrupted");
            }

            if (hasDeadline && deadlineNanoTime - System.NanoTime() <= 0)
            {
                throw new InterruptedIOException("deadline reached");
            }
        }

        /**
         * Waits on {@code monitor} until it is notified. Throws {@link InterruptedIOException} if either
         * the thread is interrupted or if this timeout elapses before {@code monitor} is notified. The
         * caller must be synchronized on {@code monitor}.
         *
         * <p>Here's a sample class that uses {@code waitUntilNotified()} to await a specific state. Note
         * that the call is made within a loop to avoid unnecessary waiting and to mitigate spurious
         * notifications. <pre>{@code
         *
         *   class Dice {
         *     Random random = new Random();
         *     int latestTotal;
         *
         *     public synchronized void roll() {
         *       latestTotal = 2 + random.nextInt(6) + random.nextInt(6);
         *       System.out.println("Rolled " + latestTotal);
         *       notifyAll();
         *     }
         *
         *     public void rollAtFixedRate(int period, TimeUnit timeUnit) {
         *       Executors.newScheduledThreadPool(0).scheduleAtFixedRate(new Runnable() {
         *         public void run() {
         *           roll();
         *          }
         *       }, 0, period, timeUnit);
         *     }
         *
         *     public synchronized void awaitTotal(Timeout timeout, int total)
         *         throws InterruptedIOException {
         *       while (latestTotal != total) {
         *         timeout.waitUntilNotified(this);
         *       }
         *     }
         *   }
         * }</pre>
         */
        public void WaitUntilNotified(Object monitor)
        {
            try
            {
                bool hasDeadline = this.hasDeadline;
                long timeoutNanos = this.timeoutNanos;

                if (!hasDeadline && timeoutNanos == 0L)
                {
                    Monitor.Wait(monitor);
                    //monitor.wait(); // There is no timeout: wait forever.
                    return;
                }

                // Compute how long we'll wait.
                long waitNanos;
                long start = System.NanoTime();
                if (hasDeadline && timeoutNanos != 0)
                {
                    long deadlineNanos = deadlineNanoTime - start;
                    waitNanos = Math.Min(timeoutNanos, deadlineNanos);
                }
                else if (hasDeadline)
                {
                    waitNanos = deadlineNanoTime - start;
                }
                else
                {
                    waitNanos = timeoutNanos;
                }

                // Attempt to wait that long. This will break out early if the monitor is notified.
                long elapsedNanos = 0L;
                if (waitNanos > 0L)
                {
                    long waitMillis = waitNanos / 1000000L;
                    Monitor.Wait(waitMillis, (int)(waitNanos - waitMillis * 1000000L));
                    //monitor.wait(waitMillis, (int)(waitNanos - waitMillis * 1000000L));
                    elapsedNanos = System.NanoTime() - start;
                }

                // Throw if the timeout elapsed before the monitor was notified.
                if (elapsedNanos >= waitNanos)
                {
                    throw new InterruptedIOException("timeout");
                }
            }
            catch (Exception e)
            {
                Thread.CurrentThread.Interrupt();
                throw new InterruptedIOException("interrupted");
            }
        }
    }


    public class NoneTimeout : Timeout
    {
        public override Timeout SetTimeout(long timeout, TimeUnit unit)
        {
            return this;
        }

        public override Timeout DeadlineNanoTime(long deadlineNanoTime)
        {
            return this;
        }

        public override void ThrowIfReached()
        {
        }
    }
}

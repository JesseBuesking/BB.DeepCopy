using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Tests
{
    /// <summary>
    /// A simple helper class for doing micro-benchmarking.
    /// </summary>
    public class TimingHelper
    {
        /// <summary>
        /// The total number of iterations to sample the method timings.
        /// </summary>
        private readonly long _iterations;

        /// <summary>
        /// The total number of warmup iterations to perform.
        /// </summary>
        private readonly long _warmupIterations;

        /// <summary>
        /// A simple object to hold the name and action for each method to be timed.
        /// </summary>
        public class Data
        {
            public string Name;

            public Action Action;

            public Data(string name, Action action)
            {
                this.Name = name;
                this.Action = action;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="warmupIterations"></param>
        /// <param name="iterations"></param>
        public TimingHelper(long warmupIterations, long iterations)
        {
            this._warmupIterations = warmupIterations;
            this._iterations = iterations;
        }

        /// <summary>
        /// Times the <see cref="TimingHelper.Data"/> objects supplied, returning an array containing
        /// the total elapsed ticks for each action.
        /// </summary>
        /// <param name="testTitle"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public long[] TimeIt(string testTitle, params Data[] data)
        {
            // Warmup.
            var random = new Random();
            for (int i = 0; i < this._warmupIterations; i++)
                foreach (int idx in RandomList(random, data.Length))
                    data[idx].Action();

            // The total elapsed ticks for each action.
            long[] actionTicks = new long[data.Length];

            for (int i = 0; i < this._iterations; i++)
                foreach (int idx in RandomList(random, data.Length))
                    actionTicks[idx] += TimingHelper.Time(data[idx].Action);

            Console.WriteLine(
                "Test: {0} (In ticks | Ticks Per ms = {1:#,##0})", testTitle, TimeSpan.TicksPerMillisecond);
            for (int i = 0; i < data.Length; i++)
            {
                Console.WriteLine("{0}: Total: {1:#,##0.0#}, Individual: {2:#,##0.0#}",
                    data[i].Name, actionTicks[i], ((double) actionTicks[i])/this._iterations);
            }
            Console.WriteLine();

            return actionTicks;
        }

        /// <summary>
        /// Times the action supplied (using a new <see cref="StopWatch"/> instance).
        /// </summary>
        /// <param name="copy"></param>
        /// <returns></returns>
        private static long Time(Action copy)
        {
            Stopwatch sw = Stopwatch.StartNew();

            copy();

            sw.Stop();
            return sw.ElapsedTicks;
        }

        /// <summary>
        /// Creates a list of integers between 0 and size, shuffled randomly.
        /// </summary>
        /// <param name="random"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private static IEnumerable<int> RandomList(Random random, int size)
        {
            // Knuth-Fisher-Yates shuffle.
            var list = Enumerable.Range(0, size).ToList();
            for (int i = size - 1; i > 0; i--)
            {
                int n = random.Next(i + 1);
                int current = list[i];
                list[i] = list[n];
                list[n] = current;
            }

            for (int i = 0; i < size; i++)
                yield return list[i];
        }
    }
}
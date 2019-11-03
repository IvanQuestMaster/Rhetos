using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public static class CommnadLineArgumentExtensions
    {
        public static bool HasCommand(this IEnumerable<string> args, string command)
        {
            if (command.Equals(args.FirstOrDefault()))
                return true;
            return false;
        }

        public static string[] GetCommand(this IEnumerable<string> args, string command, Action action)
        {
            if (command.Equals(args.FirstOrDefault()))
                action();
            return args.Skip(1).ToArray();
        }

        public static IEnumerable<string> GetOption(this IEnumerable<string> args, string option, Action<bool> action)
        {
            var options = ParseOption(option);
            var ranges = args.TakeRanges(x => options.Contains(x), x => x.StartsWith("-"));
            if (ranges.Any(x => x.Skip(1).Any()))
                throw new ArgumentException($@"The option {option} should not have any value.");
            action(ranges.Any());
            return args.SkipRanges(x => options.Contains(x), x => x.StartsWith("-"));
        }

        public static IEnumerable<string> GetOptionValue(this IEnumerable<string> args, string option, Action<string> action)
        {
            var options = ParseOption(option);
            var ranges = args.TakeRanges(x => options.Contains(x), x => x.StartsWith("-"));
            if (ranges.Count() > 1 || ranges.Any(x => x.Count() > 2))
                throw new ArgumentException($@"The option {option} should only have one value.");
            if (ranges.Any(x => x.Count() == 1))
                throw new ArgumentException($@"The option {option} should contain a value.");
            if (ranges.Any())
                action(ranges.First().Skip(1).First());
            return args.SkipRanges(x => options.Contains(x), x => x.StartsWith("-"));
        }

        public static IEnumerable<string> GetOptionValues(this IEnumerable<string> args, string option, Action<string[]> action)
        {
            var options = ParseOption(option);
            var ranges = args.TakeRanges(x => options.Contains(x), x => x.StartsWith("-"));
            if (ranges.Any(x => x.Count() == 1))
                throw new ArgumentException($@"The option {option} should contain a value.");
            if (ranges.Any())
                action(ranges.Select(x => x.Skip(1)).SelectMany(x => x).ToArray());
            return args.SkipRanges(x => options.Contains(x), x => x.StartsWith("-"));
        }

        private static List<string> ParseOption(string option)
        {
            var options = option.Split('|').ToList();
            options[0] = "--" + options[0];
            if (options.Count == 2)
                options[1] = "-" + options[1];
            return options.ToList();
        }

        public static IEnumerable<IEnumerable<T>> TakeRanges<T>(this IEnumerable<T> enumerable, Func<T, bool> isStartElement, Func<T, bool> isEndElement)
        {
            var list = new List<List<T>>();
            var enumerator = enumerable.GetEnumerator();
            var inRange = false;
            List<T> innerList = null;
            while (enumerator.MoveNext())
            {
                if (inRange && isEndElement(enumerator.Current))
                {
                    inRange = false;
                    list.Add(innerList);
                }

                if (isStartElement(enumerator.Current))
                {
                    inRange = true;
                    innerList = new List<T>();
                }

                if (inRange)
                    innerList.Add(enumerator.Current);
            }

            if(inRange)
                list.Add(innerList);

            return list;
        }

        public static IEnumerable<T> SkipRanges<T>(this IEnumerable<T> enumerable, Func<T, bool> isStartElement, Func<T, bool> isEndElement)
        {
            var skippedRangesList = new List<T>();
            var enumerator = enumerable.GetEnumerator();
            var inRange = false;
            while (enumerator.MoveNext())
            {
                if (inRange && isEndElement(enumerator.Current))
                {
                    inRange = false;
                }

                if (isStartElement(enumerator.Current))
                {
                    inRange = true;
                }

                if (!inRange)
                    skippedRangesList.Add(enumerator.Current);
            }

            return skippedRangesList;
        }
    }
}

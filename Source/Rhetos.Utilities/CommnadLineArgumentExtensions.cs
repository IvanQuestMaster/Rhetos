using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public static class CommnadLineArgumentExtensions
    {
        public static string[] HasCommand(this string[] args, string command, Action action)
        {
            if (command.Equals(args.FirstOrDefault()))
                action();
            return args.Skip(1).ToArray();
        }

        public static string[] HasOption(this string[] args, string option, Action<bool> action)
        {
            var list = args.ToList();
            var options = option.Split('|').ToList();
            options[0] = "--" + options[0];
            if(options.Count == 2)
                options[1] = "-" + options[1];
            var ranges = args.TakeRanges(x => options.Contains(x), x => x.StartsWith("--") || x.StartsWith("-"));
            if (ranges.Any(x => x.Skip(1).Any()))
                throw new ArgumentException($@"The option {option} should not have any value.");
            action(ranges.Any());
            return args;
        }

        public static string[] GetOptionValue(this string[] args, string option, Action<string> action)
        {
            var list = args.ToList();
            var options = option.Split('|').ToList();
            options[0] = "--" + options[0];
            if (options.Count == 2)
                options[1] = "-" + options[1];
            var ranges = args.TakeRanges(x => options.Contains(x), x => x.StartsWith("--") || x.StartsWith("-"));
            if (ranges.Count() > 1 || ranges.Any(x => x.Count() > 2))
                throw new ArgumentException($@"The option {option} should only have one value.");
            if (ranges.Any(x => x.Count() == 1))
                throw new ArgumentException($@"The option {option} should contain a value.");
            if (ranges.Any())
                action(ranges.First().Skip(1).First());
            return args;
        }

        public static string[] GetOptionValues(this string[] args, string option, Action<string[]> action)
        {
            var list = args.ToList();
            var options = option.Split('|').ToList();
            options[0] = "--" + options[0];
            if (options.Count == 2)
                options[1] = "-" + options[1];
            var ranges = args.TakeRanges(x => options.Contains(x), x => x.StartsWith("--") || x.StartsWith("-"));
            if (ranges.Any(x => x.Count() == 1))
                throw new ArgumentException($@"The option {option} should contain a value.");
            if (ranges.Any())
                action(ranges.Select(x => x.Skip(1)).SelectMany(x => x).ToArray());
            return args;
        }

        public static IEnumerable<IEnumerable<T>> TakeRanges<T>(this IEnumerable<T> enumerable, Func<T, bool> isStartElement, Func<T, bool> isEndElement)
        {
            var list = new List<List<T>>();
            var enumerator = enumerable.GetEnumerator();
            var inRange = false;
            List<T> innerList = null;
            while (enumerator.MoveNext())
            {
                if (isStartElement(enumerator.Current))
                {
                    inRange = true;
                    innerList = new List<T>();
                }
                if (inRange && isEndElement(enumerator.Current))
                {
                    inRange = false;
                    list.Add(innerList);
                }

                if (inRange)
                    innerList.Add(enumerator.Current);
            }

            if(inRange)
                list.Add(innerList);

            return list;
        }
    }
}

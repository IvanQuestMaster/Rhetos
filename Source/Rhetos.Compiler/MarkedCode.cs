using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Compiler
{
    public class MarkedCode
    {
        private string _sourceCode;

        private List<Marker> _markers = new List<Marker>();

        private List<LinePosition> _newLines = new List<LinePosition>();

        public string StrippedCode { get; private set; }

        public MarkedCode(string sourceCode)
        {
            _sourceCode = sourceCode;
            Initialize();
        }

        private void Initialize()
        {
            var startEndMarkerMatch = Regex.Match(_sourceCode, @"\/\*Start marker.+?\*\/|\/\*End marker.+?\*\/");
            var sb = new StringBuilder(_sourceCode.Length);
            var markerContentRegex = new Regex(@"\/\*(\bStart marker\b|\bEnd marker\b) (.+?)\*\/");
            var lastIndex = 0;

            while (startEndMarkerMatch.Success)
            {
                sb.Append(_sourceCode.Substring(lastIndex, startEndMarkerMatch.Index - lastIndex));
                var markerContentMatch = markerContentRegex.Match(startEndMarkerMatch.Value);
                var marker = markerContentMatch.Groups[2].Value;
                var markerType = markerContentMatch.Groups[1].Value;
                var lastWhitespace = marker.LastIndexOf(' ');
                if (lastWhitespace == -1)
                    throw new InvalidOperationException("Internal error: " + startEndMarkerMatch.Value + " is not a valid marker.");
                var conceptKey = marker.Substring(0, lastWhitespace);
                var propertyName = marker.Substring(lastWhitespace + 1, marker.Length - lastWhitespace - 1);

                _markers.Add(new Marker
                {
                    Position = sb.Length,
                    Type = markerType.Contains("Start marker") ? MarkerType.Open : MarkerType.Close,
                    ConceptKey = conceptKey,
                    PropertyName = propertyName
                });

                lastIndex = startEndMarkerMatch.Index + startEndMarkerMatch.Length;
                startEndMarkerMatch = startEndMarkerMatch.NextMatch();
            }

            sb.Append(_sourceCode.Substring(lastIndex, _sourceCode.Length - lastIndex));
            StrippedCode = sb.ToString();

            _newLines = GetNewlines(StrippedCode);
        }

        public CodeOffset GetNearestMarker(int line, int column)
        {
            if (line < 1 || line > _newLines.Count)
                throw new ArgumentException("The line number does not fall within the allowed range.");

            if (column < 1 || column > _newLines[line - 1].Length + 1)
                throw new ArgumentException("The column number does not fall within the allowed range.");   

            var index = _newLines[line - 1].Start + column - 1;

            for (int i = _markers.Count - 1; i >= 0; i--)
            {
                if (_markers[i].Position <= index)
                {
                    if (_markers[i].Type == MarkerType.Open)
                        return new CodeOffset
                        {
                            Offset = index - _markers[i].Position,
                            ConceptKey = _markers[i].ConceptKey,
                            PropertyName = _markers[i].PropertyName
                        };
                    else if (_markers[i].Type == MarkerType.Close && _markers[i].Position == index)
                    {
                        var openMarker = _markers.FirstOrDefault(x => x.ConceptKey == _markers[i].ConceptKey && x.Type == MarkerType.Open);
                        return new CodeOffset
                        {
                            Offset = index - openMarker.Position,
                            ConceptKey = _markers[i].ConceptKey,
                            PropertyName = _markers[i].PropertyName
                        };
                    }
                    else
                        break;
                }
            }

            return null;
        }

        public static List<LinePosition> GetNewlines(string str)
        {
            var newLineCharacter = "\n";
            var linePositions = new List<LinePosition>();

            var lastStartLine = 0;
            for (int index = 0; ; index += newLineCharacter.Length)
            {
                index = str.IndexOf(newLineCharacter, index);
                if (index == -1)
                {
                    linePositions.Add(new LinePosition { Start = lastStartLine, Length = str.Length - lastStartLine });
                    return linePositions;
                }
                linePositions.Add(new LinePosition { Start = lastStartLine, Length = index - lastStartLine });
                lastStartLine = index + newLineCharacter.Length;
            }
        }

        public struct LinePosition
        {
            public int Start;
            public int Length;

            public override string ToString()
            {
                return $@"({Start},{Length})";
            }
        }

        internal enum MarkerType
        {
            Open,
            Close
        }

        internal class Marker
        {
            public int Position;
            public MarkerType Type;
            public string ConceptKey;
            public string PropertyName;
        }

        public class CodeOffset
        {
            public int Offset;
            public string ConceptKey;
            public string PropertyName;
        }
    }
}

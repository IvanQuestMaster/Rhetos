/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Globalization;
using Rhetos.Utilities;
using Rhetos.Logging;

namespace Rhetos.Dsl
{
    public class DslParser : IDslParser
    {
        protected readonly Tokenizer _tokenizer;
        protected readonly IConceptInfo[] _conceptInfoPlugins;
        protected readonly ILogger _performanceLogger;
        protected readonly ILogger _logger;
        protected readonly ILogger _keywordsLogger;

        protected Dictionary<string, List<ConcpetMemeberMetadata>> _conceptMetadata = new Dictionary<string, List<ConcpetMemeberMetadata>>();
        protected IEnumerable<IConceptInfo> _parsedConcepts;
        protected bool _isInitialized = false;

        public DslParser(Tokenizer tokenizer, IConceptInfo[] conceptInfoPlugins, ILogProvider logProvider)
        {
            _tokenizer = tokenizer;
            _conceptInfoPlugins = conceptInfoPlugins;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslParser");
            _keywordsLogger = logProvider.GetLogger("DslParser.Keywords");
        }

        public IEnumerable<IConceptInfo> ParsedConcepts
        {
            get { Initialize();  return _parsedConcepts; }
        }

        public ConcpetMemeberMetadata GetDslScriptPositionForMember(IConceptInfo conceptInfo, string memeberName)
        {
            Initialize();
            return _conceptMetadata[conceptInfo.GetKey()].FirstOrDefault(x => x.MemberName == memeberName);
        }

        //=================================================================

        private void Initialize()
        {
            if (_isInitialized)
                return;

            IEnumerable<IConceptParser> parsers = CreateGenericParsers();
            var parsedConcepts = ExtractConcepts(parsers);
            //var alternativeInitializationGeneratedReferences = InitializeAlternativeInitializationConcepts(parsedConcepts);
            _parsedConcepts = new[] { CreateInitializationConcept() }
                .Concat(parsedConcepts)
                //.Concat(alternativeInitializationGeneratedReferences)
                .ToList();

            _isInitialized = true;
        }

        private IConceptInfo CreateInitializationConcept()
        {
            return new InitializationConcept
            {
                RhetosVersion = SystemUtility.GetRhetosVersion()
            };
        }

        protected IEnumerable<IConceptParser> CreateGenericParsers()
        {
            var stopwatch = Stopwatch.StartNew();

            var conceptMetadata = _conceptInfoPlugins
                .Select(conceptInfo => conceptInfo.GetType())
                .Distinct()
                .Select(conceptInfoType => new
                            {
                                conceptType = conceptInfoType,
                                conceptKeyword = ConceptInfoHelper.GetKeyword(conceptInfoType)
                            })
                .Where(cm => cm.conceptKeyword != null)
                .ToList();

            _keywordsLogger.Trace(() => string.Join(" ", conceptMetadata.Select(cm => cm.conceptKeyword).OrderBy(keyword => keyword).Distinct()));

            var result = conceptMetadata.Select(cm => new GenericParser(cm.conceptType, cm.conceptKeyword)).ToList<IConceptParser>();
            _performanceLogger.Write(stopwatch, "DslParser.CreateGenericParsers.");
            return result;
        }

        protected IEnumerable<IConceptInfo> ExtractConcepts(IEnumerable<IConceptParser> conceptParsers)
        {
            var stopwatch = Stopwatch.StartNew();

            TokenReader tokenReader = new TokenReader(_tokenizer.GetTokens(), 0);

            List<IConceptInfo> newConcepts = new List<IConceptInfo>();
            Stack<IConceptInfo> context = new Stack<IConceptInfo>();

            tokenReader.SkipEndOfFile();
            while (!tokenReader.EndOfInput)
            {
                var conceptWithMetadata = ParseNextConcept(tokenReader, context, conceptParsers);
                IConceptInfo conceptInfo = conceptWithMetadata.ConceptInfo;
                newConcepts.Add(conceptInfo);

                var alteernateInterpretation = InitializeAlternativeInitializationConcepts(new List<IConceptInfo> { conceptInfo });
                newConcepts.AddRange(alteernateInterpretation);
                if (conceptWithMetadata.MemeberMetadata != null && !_conceptMetadata.ContainsKey(conceptInfo.GetKey()))
                    _conceptMetadata.Add(conceptInfo.GetKey(), conceptWithMetadata.MemeberMetadata);

                UpdateContextForNextConcept(tokenReader, context, conceptInfo);

                if (context.Count == 0)
                    tokenReader.SkipEndOfFile();
            }

            _performanceLogger.Write(stopwatch, "DslParser.ExtractConcepts (" + newConcepts.Count + " concepts).");

            if (context.Count > 0)
                throw new DslSyntaxException(string.Format(
                    ReportErrorContext(context.Peek(), tokenReader)
                    + "Expected \"}\" at the end of the script to close concept \"{0}\".", context.Peek()));

            return newConcepts;
        }

        class Interpretation { public IConceptInfo ConceptInfo; public TokenReader NextPosition; public List<ConcpetMemeberMetadata> ConceptMemeberMetadata; }
        public class ConceptWithMetadata { public IConceptInfo ConceptInfo; public List<ConcpetMemeberMetadata> MemeberMetadata; }

        protected ConceptWithMetadata ParseNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, IEnumerable<IConceptParser> conceptParsers)
        {
            var errors = new List<string>();
            List<Interpretation> possibleInterpretations = new List<Interpretation>();

            foreach (var conceptParser in conceptParsers)
            {
                TokenReader nextPosition = new TokenReader(tokenReader);
                //var conceptInfoOrError = conceptParser.Parse(nextPosition, context);
                var conceptInfoWithMetadataOrError = conceptParser.ParseConceptWithMetadata(nextPosition, context);
               
                if (!conceptInfoWithMetadataOrError.IsError)
                {
                    possibleInterpretations.Add(new Interpretation
                    {
                        ConceptInfo = conceptInfoWithMetadataOrError.Value.Concept,
                        NextPosition = nextPosition,
                        ConceptMemeberMetadata = conceptInfoWithMetadataOrError.Value.ConceptMemeberMetadata
                    });
                }
                else if (!string.IsNullOrEmpty(conceptInfoWithMetadataOrError.Error)) // Empty error means that this parser is not for this keyword.
                    errors.Add(string.Format("{0}: {1}\r\n{2}", conceptParser.GetType().Name, conceptInfoWithMetadataOrError.Error, tokenReader.ReportPosition()));
            }

            if (possibleInterpretations.Count == 0)
            {
                var nextToken = new TokenReader(tokenReader).ReadText(); // Peek, without changing the original tokenReader's position.
                string keyword = nextToken.IsError ? null : nextToken.Value;

                if (errors.Count > 0)
                {
                    string errorsReport = string.Join("\r\n", errors).Limit(500, "...");
                    throw new DslSyntaxException($"Invalid parameters after keyword '{keyword}'. {tokenReader.ReportPosition()}\r\n\r\nPossible causes:\r\n{errorsReport}");
                }
                else if (!string.IsNullOrEmpty(keyword))
                    throw new DslSyntaxException($"Unrecognized concept keyword '{keyword}'. {tokenReader.ReportPosition()}");
                else
                    throw new DslSyntaxException($"Invalid DSL script syntax. {tokenReader.ReportPosition()}");
            }

            int largest = possibleInterpretations.Max(i => i.NextPosition.PositionInTokenList);
            possibleInterpretations.RemoveAll(i => i.NextPosition.PositionInTokenList < largest);
            if (possibleInterpretations.Count > 1)
            {
                string msg = "Ambiguous syntax. " + tokenReader.ReportPosition()
                    + "\r\n Possible interpretations: "
                    + string.Join(", ", possibleInterpretations.Select(i => i.ConceptInfo.GetType().Name))
                    + ".";
                throw new DslSyntaxException(msg);
            }

            tokenReader.CopyFrom(possibleInterpretations.Single().NextPosition);
            var interpretation = possibleInterpretations.Single();
            IConceptInfo conceptInterpretation = interpretation.ConceptInfo;

            return new ConceptWithMetadata {
                ConceptInfo = interpretation.ConceptInfo,
                MemeberMetadata = interpretation.ConceptMemeberMetadata
            };
        }

        protected string ReportErrorContext(IConceptInfo conceptInfo, TokenReader tokenReader)
        {
            var sb = new StringBuilder();
            sb.AppendLine(tokenReader.ReportPosition());
            if (conceptInfo != null)
            {
                sb.AppendFormat("Previous concept: {0}", conceptInfo.GetUserDescription()).AppendLine();
                var properties = conceptInfo.GetType().GetProperties().ToList();
                properties.ForEach(it =>
                    sb.AppendFormat("Property {0} ({1}) = {2}",
                        it.Name,
                        it.PropertyType.Name,
                        it.GetValue(conceptInfo, null) ?? "<null>")
                        .AppendLine());
            }
            return sb.ToString();
        }

        protected void UpdateContextForNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, IConceptInfo conceptInfo)
        {
            if (tokenReader.TryRead("{"))
                context.Push(conceptInfo);
            else if (!tokenReader.TryRead(";"))
            {
                var sb = new StringBuilder();
                sb.Append(ReportErrorContext(conceptInfo, tokenReader));
                sb.AppendFormat("Expected \";\" or \"{{\".");
                throw new DslSyntaxException(sb.ToString());
            }

            while (tokenReader.TryRead("}"))
            {
                if (context.Count == 0)
                    throw new DslSyntaxException(tokenReader.ReportPosition() + "\r\nUnexpected \"}\". ");
                context.Pop();
            }
        }

        protected IEnumerable<IConceptInfo> InitializeAlternativeInitializationConcepts(IEnumerable<IConceptInfo> parsedConcepts)
        {
            var stopwatch = Stopwatch.StartNew();
            var newConcepts = AlternativeInitialization.InitializeNonparsableProperties(parsedConcepts, _logger);
            _performanceLogger.Write(stopwatch, "DslParser.InitializeAlternativeInitializationConcepts (" + newConcepts.Count() + " new concepts created).");
            return newConcepts;
        }
    }
}
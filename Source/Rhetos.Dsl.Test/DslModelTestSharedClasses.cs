using Autofac.Features.Indexed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dsl.Test
{
    #region Sample concept classes

    [ConceptKeyword("simple")]
    class SimpleConceptInfo : IConceptInfo
    {
        [ConceptKey]
        public string Name { get; set; }
        public string Data { get; set; }

        public override string ToString() { return "SIMPLE " + Name; }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public SimpleConceptInfo() { }
        public SimpleConceptInfo(string name, string data)
        {
            Name = name;
            Data = data;
        }
    }

    class DerivedConceptInfo : SimpleConceptInfo
    {
        public string Extra { get; set; }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public DerivedConceptInfo(string name, string data, string extra)
            : base(name, data)
        {
            Extra = extra;
        }
    }

    class RefConceptInfo : IConceptInfo
    {
        [ConceptKey]
        public string Name { get; set; }
        [ConceptKey]
        public SimpleConceptInfo Reference { get; set; }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public RefConceptInfo() { }
        public RefConceptInfo(string name, SimpleConceptInfo reference)
        {
            Name = name;
            Reference = reference;
        }
        public override string ToString()
        {
            return "REF " + Name + " " + Reference.ToString();
        }
    }

    #endregion

    internal class StubDslParser : IDslParser
    {
        private readonly IEnumerable<IConceptInfo> _rawConcepts;
        public StubDslParser(IEnumerable<IConceptInfo> rawConcepts) { _rawConcepts = rawConcepts; }
        public IEnumerable<IConceptInfo> ParsedConcepts { get { return _rawConcepts; } }
    }

    internal class StubMacroIndex : IIndex<Type, IEnumerable<IConceptMacro>>
    {
        public bool TryGetValue(Type key, out IEnumerable<IConceptMacro> value)
        {
            value = new IConceptMacro[] { };
            return true;
        }

        public IEnumerable<IConceptMacro> this[Type key]
        {
            get { return new IConceptMacro[] { }; }
        }
    }

    internal class StubMacroOrderRepository : IMacroOrderRepository
    {
        public List<MacroOrder> Load() { return new List<MacroOrder>(); }
        public void Save(IEnumerable<MacroOrder> macroOrders) { }
    }

    internal class StubDslModelFile : IDslModelFile
    {
        public void SaveConcepts(IEnumerable<IConceptInfo> concepts) { }
    }
}

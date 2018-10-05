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

using Autofac.Features.Indexed;
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public class DslModelWithReplacableConcepts : IDslModel
    {
        private readonly IDslParser _dslParser;
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly ILogger _evaluatorsOrderLogger;
        private readonly ILogger _dslModelConceptsLogger;
        private readonly IIndex<Type, IEnumerable<IConceptMacro>> _macros;
        private readonly IEnumerable<Type> _macroTypes;
        private readonly IEnumerable<Type> _conceptTypes;
        private readonly IMacroOrderRepository _macroOrderRepository;
        private readonly IDslModelFile _dslModelFile;

        List<IConceptInfo> _concepts;

        public IEnumerable<IConceptInfo> Concepts { get { return _concepts; } }

        public DslModelWithReplacableConcepts(
            IDslParser dslParser,
            ILogProvider logProvider,
            IIndex<Type, IEnumerable<IConceptMacro>> macros,
            IEnumerable<IConceptMacro> macroPrototypes,
            IEnumerable<IConceptInfo> conceptPrototypes,
            IMacroOrderRepository macroOrderRepository,
            IDslModelFile dslModelFile)
        {
            _dslParser = dslParser;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslModel");
            _evaluatorsOrderLogger = logProvider.GetLogger("MacroEvaluatorsOrder");
            _dslModelConceptsLogger = logProvider.GetLogger("DslModelConcepts");
            _macros = macros;
            _macroTypes = macroPrototypes.Select(macro => macro.GetType());
            _conceptTypes = conceptPrototypes.Select(conceptInfo => conceptInfo.GetType());
            _macroOrderRepository = macroOrderRepository;
            _dslModelFile = dslModelFile;
        }

        public IConceptInfo FindByKey(string conceptKey)
        {
            return _concepts.Where(c => c.GetKey() == conceptKey).SingleOrDefault();
        }

        public IEnumerable<IConceptInfo> FindByType(Type conceptType)
        {
            return _concepts.Where(c => conceptType.IsAssignableFrom(c.GetType()));
        }

        public T GetIndex<T>() where T : IDslModelIndex
        {
            IDslModelIndex index = (IDslModelIndex)typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { });
            foreach (var concept in Concepts)
                index.Add(concept);
            return (T)index;
        }

 

        private class ConceptDescription
        {
            public readonly IConceptInfo Concept;
            public readonly string Key;
            public int UnresolvedDependencies;

            public ConceptDescription(IConceptInfo concept)
            {
                Concept = concept;
                Key = concept.GetKey();
                UnresolvedDependencies = 0;
            }
        }

        public class AddNewConceptsReport
        {
            /// <summary>A subset of given new concepts. Some of the returned concepts might not have their references resolved yet.</summary>
            public List<IConceptInfo> NewUniqueConcepts;
            /// <summary>May include previously given concepts that have been resolved now.</summary>
            public List<IConceptInfo> NewlyResolvedConcepts;
        }

        private class UnresolvedReference
        {
            public readonly ConceptDescription Dependant;
            /// <summary>A member property on the Dependant concept that references another concept.</summary>
            public readonly ConceptMember Member;
            public readonly IConceptInfo ReferencedStub;
            public readonly string ReferencedKey;

            public UnresolvedReference(ConceptDescription dependant, ConceptMember referenceMember)
            {
                Dependant = dependant;
                Member = referenceMember;
                ReferencedStub = (IConceptInfo)Member.GetValue(Dependant.Concept);
                ReferencedKey = ReferencedStub?.GetKey();
            }
        }
    }
}

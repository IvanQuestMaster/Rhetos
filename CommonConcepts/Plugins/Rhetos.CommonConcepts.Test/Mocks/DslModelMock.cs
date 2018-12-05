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

using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.CommonConcepts.Test.Mocks
{
    public class DslModelMock : List<IConceptInfo>, IDslModel
    {
        public IDslSubset<IConceptInfo> Concepts { get { return new DslSubset<IConceptInfo>(this); } }

        public IConceptInfo FindByKey(string conceptKey)
        {
            return this.Where(c => c.GetKey() == conceptKey).SingleOrDefault();
        }

        public IDslSubset<IConceptInfo> FindByType(Type conceptType)
        {
            return new DslSubset<IConceptInfo>(this.Where(c => conceptType.IsAssignableFrom(c.GetType())));
        }

        public IDslSubset<TResult> QueryIndex<TIndex, TResult>(Func<TIndex, IEnumerable<TResult>> query) where TIndex : IDslModelIndex where TResult : IConceptInfo
        {
            IDslModelIndex index = (IDslModelIndex)typeof(TIndex).GetConstructor(new Type[] { }).Invoke(new object[] { });
            foreach (var concept in Concepts)
                index.Add(concept);
            return new DslSubset<TResult>(query((TIndex)index));
        }
    }
}

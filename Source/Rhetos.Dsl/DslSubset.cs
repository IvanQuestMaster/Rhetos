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
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Collections;

namespace Rhetos.Dsl
{
    public class DslSubset<T> : IDslSubset<T> where T : IConceptInfo
    {
        private readonly IDslTracker _dslModel;

        private readonly IEnumerable<T> _subset;

        public DslSubset(IEnumerable<T> subset)
        {
            _subset = subset;
        }

        public DslSubset(IDslTracker dslModel, IEnumerable<T> subset)
        {
            _dslModel = dslModel;
            _subset = subset;
        }

        public IDslSubset<TDestination> Query<TDestination>(Func<IEnumerable<T>, IEnumerable<TDestination>> query) where TDestination : IConceptInfo
        {
            return new DslSubset<TDestination>(_dslModel, query(_subset));
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_dslModel != null)
            {
                foreach (var concept in _subset)
                    _dslModel.AddConceptToTracker(concept);
            }
            return _subset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_dslModel != null)
            {
                foreach (var concept in _subset)
                    _dslModel.AddConceptToTracker(concept);
            }
            return _subset.GetEnumerator();
        }
    }
}
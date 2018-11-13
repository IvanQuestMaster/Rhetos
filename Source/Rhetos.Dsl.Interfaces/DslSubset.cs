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
    public class DslSubset<T> : IEnumerable<T> where T : IConceptInfo
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

        public DslSubset<T> Where(Func<T,bool> query)
        {
            return new DslSubset<T>(_dslModel, _subset.Where(query));
        }

        public DslSubset<TCast> OfType<TCast>() where TCast : IConceptInfo
        {
            return new DslSubset<TCast>(_dslModel, _subset.OfType<TCast>());
        }

        public IEnumerator<T> GetEnumerator()
        {
            if(_dslModel != null)   
                return new DslSubsetEnumeratorWithTracking<T>(_dslModel, _subset);
            else
                return new DslSubsetEnumerator<T>(_subset);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_dslModel != null)
                return new DslSubsetEnumeratorWithTracking<T>(_dslModel, _subset);
            else
                return new DslSubsetEnumerator<T>(_subset);
        }
    }

    public class DslSubsetEnumerator<T> : IEnumerator<T> where T : IConceptInfo
    {
        IEnumerable<T> _concepts;

        IEnumerator<T> _current;

        public DslSubsetEnumerator(IEnumerable<T> concepts)
        {
            _concepts = concepts;
            _current = concepts.GetEnumerator();
        }

        public object Current
        {
            get {
                return _current.Current;
            }
        }

        public bool MoveNext()
        {
            return _current.MoveNext();
        }

        public void Reset()
        {
            _current.Reset();
        }

        public IEnumerator GetEnumerator()
        {
            return this;
        }

        T IEnumerator<T>.Current
        {
            get {
                return _current.Current;
            }
        }

        public void Dispose()
        {}
    }

    public class DslSubsetEnumeratorWithTracking<T> : IEnumerator<T> where T : IConceptInfo
    {
        IDslTracker _dslTracker;

        IEnumerable<T> _concepts;

        IEnumerator<T> _current;

        public DslSubsetEnumeratorWithTracking(IDslTracker dslTracker, IEnumerable<T> concepts)
        {
            _dslTracker = dslTracker;
            _concepts = concepts;
            _current = concepts.GetEnumerator();
        }

        public object Current
        {
            get
            {
                _dslTracker.AddConceptToTracker(_current.Current);
                return _current.Current;
            }
        }

        public bool MoveNext()
        {
            return _current.MoveNext();
        }

        public void Reset()
        {
            _current.Reset();
        }

        public IEnumerator GetEnumerator()
        {
            return this;
        }

        T IEnumerator<T>.Current
        {
            get
            {
                _dslTracker.AddConceptToTracker(_current.Current);
                return _current.Current;
            }
        }

        public void Dispose()
        { }
    }
}
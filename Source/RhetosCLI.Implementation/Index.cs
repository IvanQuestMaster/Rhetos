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

namespace Rhetos.Implementations
{
    public class Index<TKey, TValue> : Rhetos.IIndex<TKey, TValue>
    {
        Autofac.Features.Indexed.IIndex<TKey, TValue> _index;

        public TValue this[TKey key] {
            get { return _index[key]; }
        }

        public Index(Autofac.Features.Indexed.IIndex<TKey, TValue> index)
        {
            _index = index;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _index.TryGetValue(key, out value);
        }
    }
}

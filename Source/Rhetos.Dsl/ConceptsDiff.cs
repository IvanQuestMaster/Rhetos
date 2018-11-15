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

namespace Rhetos.Dsl
{
    public static class ConceptsDiff
    {
        public static DiffResult Diff(this IEnumerable<IConceptInfo> oldConcepts, IEnumerable<IConceptInfo> newConcepts)
        {
            var removedList = new List<IConceptInfo>();
            var changedList = new List<IConceptInfo>();
            var addedList = new List<IConceptInfo>();

            List<IConceptInfo> newConceptsList = newConcepts.OrderBy(item => item.GetKey()).ToList();
            List<IConceptInfo> oldConceptsList = oldConcepts.OrderBy(item => item.GetKey()).ToList();

            IEnumerator<IConceptInfo> newEnum = oldConcepts.GetEnumerator();
            IEnumerator<IConceptInfo> oldEnum = newConcepts.GetEnumerator();

            bool newExists = newEnum.MoveNext();
            bool oldExists = oldEnum.MoveNext();

            var conceptComparer = new ConceptComparer();

            while (true)
            {
                int keyDiff;

                if (newExists)
                    if (oldExists)
                        keyDiff = conceptComparer.Compare(newEnum.Current, oldEnum.Current);
                    else
                        keyDiff = -1;
                else
                    if (oldExists)
                    keyDiff = 1;
                else
                    break;

                if (keyDiff == 0)
                {
                    if (!SameValue(oldEnum.Current, newEnum.Current))
                    {
                        changedList.Add(oldEnum.Current);
                    }
                    newExists = newEnum.MoveNext();
                    oldExists = oldEnum.MoveNext();
                }
                else if (keyDiff < 0)
                {
                    removedList.Add(newEnum.Current);
                    newExists = newEnum.MoveNext();
                }
                else
                {
                    addedList.Add(oldEnum.Current);
                    oldExists = oldEnum.MoveNext();

                }
            }

            return new DiffResult
            {
                Added = addedList,
                Changed = changedList,
                Removed = removedList
            };
        }

        private static bool SameValue(IConceptInfo concept1, IConceptInfo concept2)
        {
            if (concept1.GetKey() != concept2.GetKey())
                return false;

            if (concept1.GetType() != concept2.GetType())
                return false;

            var properties = ConceptMembers.Get(concept1);
            foreach (var property in properties)
            {
                if (property.GetValue(concept1) != property.GetValue(concept2))
                    return false;
            }

            return true;
        }

        public class DiffResult
        {
            public IEnumerable<IConceptInfo> Added;
            public IEnumerable<IConceptInfo> Changed;
            public IEnumerable<IConceptInfo> Removed;
        }

        internal class ConceptComparer : IComparer<IConceptInfo>
        {
            public int Compare(IConceptInfo x, IConceptInfo y)
            {
                return string.Compare(x.GetKey(), y.GetKey());
            }
        }
    }
}

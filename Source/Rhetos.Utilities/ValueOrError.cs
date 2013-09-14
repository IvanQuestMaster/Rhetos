﻿/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace Rhetos.Utilities
{
    internal interface IValueOrError
    {
    }

    /// <summary>
    /// Used as a replacement for exceptions in error handling. Exceptions can hinder performance
    /// in debug mode (about 100 exceptions per second can be processed).
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    public class ValueOrError<T> : IValueOrError where T : class
    {
        /// <summary>
        /// Implicit cast can be used instead of this function.
        /// </summary>
        public static ValueOrError<T> CreateValue(T value)
        {
            return new ValueOrError<T>(value, null);
        }

        public static ValueOrError<T> CreateError(string error)
        {
            return new ValueOrError<T>(null, error);
        }

        private readonly T _value;
        public T Value
        {
            get
            {
                if (IsError)
                    throw new FrameworkException(Error + " Reading the Value property while the Error property is set.");
                return _value;
            }
        }

        private readonly string _error;
        public string Error
        {
            get { return _error;  }
        }

        public bool IsError
        {
            get { return Error != null; }
        }

        public override string ToString()
        {
            if (_error != null)
                return "Error: " + _error;
            if (_value != null)
                return "Value: " + _value;
            return "<null>";
        }

        protected ValueOrError(T value, string error)
        {
            if (value != null && error != null)
                throw new FrameworkException(string.Format("Both value and error are returned. Value {0}, error {1}.", value, error));
            _value = value;
            _error = error;
        }

        public static implicit operator ValueOrError<T>(T value)
        {
            if (value is IValueOrError)
                throw new FrameworkException("Should not wrap a ValueOrError into another ValueOrError. Probably unexpected implicit casting. Use ChangeType<> function to change type of value in ValueOrError.");
            return new ValueOrError<T>(value, null);
        }

        public static implicit operator ValueOrError<T>(ValueOrError error)
        {
            return new ValueOrError<T>(null, error.Error);
        }

        public ValueOrError<TNew> ChangeType<TNew>() where TNew : class
        {
            TNew newValue = null;
            if (_value != null)
            {
                newValue = _value as TNew;
                if (newValue == null)
                    throw new Exception(string.Format("Cannot cast {0} to {1}.", typeof(T).Name, typeof(TNew).Name));
            }
            return new ValueOrError<TNew>(newValue, _error);
        }
    }

    /// <summary>
    /// This is a helper for returning an error without specifying a generic type.
    /// It will be implicitly converted to a generic ValueOrError<> class.
    /// </summary>
    public class ValueOrError : IValueOrError
    {
        private readonly string _error;
        public string Error
        {
            get { return _error; }
        }

        private ValueOrError(string error)
        {
            _error = error;
        }

        public static ValueOrError CreateError(string error)
        {
            return new ValueOrError(error);
        }
    }
}

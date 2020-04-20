﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal readonly struct SegmentedArray<T> : ICloneable, IList, IStructuralComparable, IStructuralEquatable, IList<T>, IReadOnlyList<T>
    {
        private readonly int _segmentSize;
        private readonly int _segmentShift;
        private readonly int _offsetMask;

        private readonly int _length;
        private readonly T[][] _items;

        public SegmentedArray(int segmentSize, int length)
        {
            if (segmentSize <= 1 || (segmentSize & (segmentSize - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentSize), CompilerExtensionsResources.Segment_size_must_be_power_of_2_greater_than_1);
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _segmentSize = segmentSize;
            _offsetMask = segmentSize - 1;
            _segmentShift = 0;

            while (0 != (segmentSize >>= 1))
            {
                _segmentShift++;
            }

            if (length == 0)
            {
                _items = Array.Empty<T[]>();
                _length = 0;
            }
            else
            {
                _items = new T[(length + _segmentSize - 1) >> _segmentShift][];
                for (var i = 0; i < _items.Length - 1; i++)
                {
                    _items[i] = new T[_segmentSize];
                }

                _items[^1] = new T[length & _offsetMask];
                _length = length;
            }
        }

        private SegmentedArray(int segmentSize, int segmentShift, int offsetMask, int length, T[][] items)
        {
            _segmentSize = segmentSize;
            _segmentShift = segmentShift;
            _offsetMask = offsetMask;
            _length = length;
            _items = items;
        }

        public bool IsFixedSize => true;

        public bool IsReadOnly => false;

        public bool IsSynchronized => false;

        public int Length => _length;

        public object SyncRoot => _items ?? Array.Empty<T[]>().SyncRoot;

        public ref T this[int index]
        {
            get
            {
                return ref _items[index >> _segmentShift][index & _offsetMask];
            }
        }

        int ICollection.Count => _length;

        int ICollection<T>.Count => _length;

        int IReadOnlyCollection<T>.Count => _length;

        T IReadOnlyList<T>.this[int index] => this[index];

        T IList<T>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value;
        }

        public object Clone()
        {
            var items = (T[][])_items.Clone();
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = (T[])items[i].Clone();
            }

            return new SegmentedArray<T>(_segmentSize, _segmentShift, _offsetMask, _length, items);
        }

        public void CopyTo(Array array, int index)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                _items[i].CopyTo(array, index + (i * _segmentSize));
            }
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                ICollection<T> collection = _items[i];
                collection.CopyTo(array, arrayIndex + (i * _segmentSize));
            }
        }

        public Enumerator GetEnumerator()
            => new Enumerator(this);

        int IList.Add(object value)
        {
            IList list = Array.Empty<T>();
            _ = list.Add(value);
            throw ExceptionUtilities.Unreachable;
        }

        void ICollection<T>.Add(T value)
        {
            ICollection<T> list = Array.Empty<T>();
            list.Add(value);
            throw ExceptionUtilities.Unreachable;
        }

        void IList.Clear()
        {
            foreach (IList list in _items)
            {
                list.Clear();
            }
        }

        void ICollection<T>.Clear()
        {
            ICollection<T> list = Array.Empty<T>();
            list.Clear();
            throw ExceptionUtilities.Unreachable;
        }

        bool IList.Contains(object value)
        {
            foreach (IList list in _items)
            {
                if (list.Contains(value))
                    return true;
            }

            return false;
        }

        bool ICollection<T>.Contains(T value)
        {
            foreach (ICollection<T> collection in _items)
            {
                if (collection.Contains(value))
                    return true;
            }

            return false;
        }

        int IList.IndexOf(object value)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                IList list = _items[i];
                var index = list.IndexOf(value);
                if (index >= 0)
                {
                    return index + i * _segmentSize;
                }
            }

            return -1;
        }

        int IList<T>.IndexOf(T value)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                IList<T> list = _items[i];
                var index = list.IndexOf(value);
                if (index >= 0)
                {
                    return index + i * _segmentSize;
                }
            }

            return -1;
        }

        void IList.Insert(int index, object value)
        {
            IList list = Array.Empty<T>();
            list.Insert(0, value);
            throw ExceptionUtilities.Unreachable;
        }

        void IList<T>.Insert(int index, T value)
        {
            IList<T> list = Array.Empty<T>();
            list.Insert(0, value);
            throw ExceptionUtilities.Unreachable;
        }

        void IList.Remove(object value)
        {
            IList list = Array.Empty<T>();
            list.Remove(value);
            throw ExceptionUtilities.Unreachable;
        }

        bool ICollection<T>.Remove(T value)
        {
            ICollection<T> collection = Array.Empty<T>();
            _ = collection.Remove(value);
            throw ExceptionUtilities.Unreachable;
        }

        void IList.RemoveAt(int index)
        {
            IList list = Array.Empty<T>();
            list.RemoveAt(0);
            throw ExceptionUtilities.Unreachable;
        }

        void IList<T>.RemoveAt(int index)
        {
            IList<T> list = Array.Empty<T>();
            list.RemoveAt(0);
            throw ExceptionUtilities.Unreachable;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        int IStructuralComparable.CompareTo(object other, IComparer comparer)
        {
            if (other is null)
                return 1;

            if (!(other is SegmentedArray<T> o))
            {
                // Delegate to T[] so the correct exception is thrown
                IStructuralComparable comparable = Array.Empty<T>();
                comparable.CompareTo(new object(), comparer);
                throw ExceptionUtilities.Unreachable;
            }

            if (Length != o.Length)
            {
                // Delegate to T[] so the correct exception is thrown
                IStructuralComparable comparable = Array.Empty<T>();
                comparable.CompareTo(new T[1], comparer);
                throw ExceptionUtilities.Unreachable;
            }

            for (var i = 0; i < Length; i++)
            {
                var result = comparer.Compare(this[i], o[i]);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
        {
            if (other is null)
                return false;

            if (!(other is SegmentedArray<T> o))
                return false;

            if ((object)_items == o._items)
                return true;

            if (Length != o.Length)
                return false;

            for (var i = 0; i < Length; i++)
            {
                if (!comparer.Equals(this[i], o[i]))
                    return false;
            }

            return true;
        }

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            _ = comparer ?? throw new ArgumentNullException(nameof(comparer));

            var ret = 0;
            for (var i = Length >= 8 ? Length - 8 : 0; i < Length; i++)
            {
                ret = Hash.Combine(comparer.GetHashCode(this[i]), ret);
            }

            return ret;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[][] _items;
            private int _nextItemSegment;
            private int _nextItemIndex;
            private T _current;

            public Enumerator(SegmentedArray<T> array)
            {
                _items = array._items;
                _nextItemSegment = 0;
                _nextItemIndex = 0;
                _current = default;
            }

            public T Current => _current;
            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_items.Length == 0)
                    return false;

                if (_nextItemIndex == _items[_nextItemSegment].Length)
                {
                    if (_nextItemSegment == _items.Length - 1)
                    {
                        return false;
                    }

                    _nextItemSegment++;
                    _nextItemIndex = 0;
                }

                _current = _items[_nextItemSegment][_nextItemIndex];
                _nextItemIndex++;
                return true;
            }

            public void Reset()
            {
                _nextItemSegment = 0;
                _nextItemIndex = 0;
                _current = default;
            }
        }
    }
}

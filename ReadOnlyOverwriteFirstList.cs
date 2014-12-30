using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

namespace KeySAV2
{
    public class ReadOnlyOverwriteFirstList<T> : IList<T>, IBindingList
    {
        protected IList<T> _data;

        public virtual IList<T> Data
        {
            get { return _data; }
            set
            {
                if (value.Count == _data.Count)
                {
                    _data = value;
                    OnListChanged();
                }
                else throw new IndexOutOfRangeException();
            }
        }

        public T Overwrite { get; set; }

        public ReadOnlyOverwriteFirstList(IList<T> data, T overwrite)
        {
            _data = data;
            Overwrite = overwrite;
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            yield return Overwrite;
            for (int i = 1; i < Data.Count; ++i)
                yield return Data[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return Overwrite;
            for (int i = 1; i < Data.Count; ++i)
                yield return Data[i];
        }

        public void Add(T item)
        {
            throw new ReadOnlyException();
        }

        public int Add(object value)
        {
            throw new ReadOnlyException();
        }

        public bool Contains(object value)
        {
            return IndexOf((T) value) != -1;
        }

        public void Clear()
        {
            throw new ReadOnlyException();
        }

        public int IndexOf(object value)
        {
            return IndexOf((T) value);
        }

        public void Insert(int index, object value)
        {
            throw new ReadOnlyException();
        }

        public void Remove(object value)
        {
            throw new ReadOnlyException();
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            Data.CopyTo(array, arrayIndex);
            array[arrayIndex] = Overwrite;
        }

        public bool Remove(T item)
        {
            throw new ReadOnlyException();
        }

        public void CopyTo(Array array, int index)
        {
            CopyTo((T[]) array, index);
        }

        public virtual int Count
        {
            get { return Math.Max(1, Data.Count); }
        }

        public object SyncRoot
        {
            get { return ((ICollection) Data).SyncRoot; }
        }

        public bool IsSynchronized
        {
            get { return ((ICollection) Data).IsSynchronized; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool IsFixedSize
        {
            get { return true; }
        }

        public virtual int IndexOf(T item)
        {
            if (EqualityComparer<T>.Default.Equals(item, Overwrite)) return 0;
            int res = Data.IndexOf(item);
            if (res == 0) return -1;
            return res;
        }

        public void Insert(int index, T item)
        {
            throw new ReadOnlyException();
        }

        public void RemoveAt(int index)
        {
            throw new ReadOnlyException();
        }

        public virtual object this[int index]
        {
            get { return index == 0 ? Overwrite : Data[index]; }
            set { throw new ReadOnlyException(); }
        }

        T IList<T>.this[int index]
        {
            get { return index == 0 ? Overwrite : Data[index]; }
            set { throw new ReadOnlyException(); }
        }

        public object AddNew()
        {
            throw new ReadOnlyException();
        }

        public void AddIndex(PropertyDescriptor property)
        {
            throw new ReadOnlyException();
        }

        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            throw new NotImplementedException();
        }

        public int Find(PropertyDescriptor property, object key)
        {
            throw new NotImplementedException();
        }

        public void RemoveIndex(PropertyDescriptor property)
        {
            throw new NotImplementedException();
        }

        public void RemoveSort()
        {
            throw new NotImplementedException();
        }

        public bool AllowNew
        {
            get { return false; }
        }

        public bool AllowEdit
        {
            get { return false; }
        }

        public bool AllowRemove
        {
            get { return false; }
        }

        public bool SupportsChangeNotification
        {
            get { return true; }
        }

        public bool SupportsSearching
        {
            get { return false; }
        }

        public bool SupportsSorting
        {
            get { return false; }
        }

        public bool IsSorted
        {
            get { throw new NotImplementedException(); }
        }

        public PropertyDescriptor SortProperty
        {
            get { throw new NotImplementedException(); }
        }

        public ListSortDirection SortDirection
        {
            get { throw new NotImplementedException(); }
        }

        public event ListChangedEventHandler ListChanged;

        protected virtual void OnListChanged()
        {
            if (ListChanged != null)
                ListChanged(this, new ListChangedEventArgs(ListChangedType.Reset, 0));
        }
    }
}

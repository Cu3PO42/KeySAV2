using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeySAV2
{
    public class ReadOnlyInsertFirstList<T> : ReadOnlyOverwriteFirstList<T>, IList<T>
    {
        public ReadOnlyInsertFirstList(IList<T> data, T insert) : base(data, insert) {}

        public override IEnumerator<T> GetEnumerator()
        {
            yield return Overwrite;
            for (int i = 0; i < Data.Count; ++i)
                yield return Data[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return Overwrite;
            for (int i = 0; i < Data.Count; ++i)
                yield return Data[i];
        }

        public override void CopyTo(T[] array, int arrayIndex)
        {
            Data.CopyTo(array, arrayIndex+1);
            array[arrayIndex] = Overwrite;
        }

        public override int Count
        {
            get { return 1 + Data.Count; }
        }

        public override int IndexOf(T item)
        {
            if (EqualityComparer<T>.Default.Equals(item, Overwrite)) return 0;
            int res = Data.IndexOf(item);
            if (res == -1) return -1;
            return res+1;
        }

        public override object this[int index]
        {
            get { return index == 0 ? Overwrite : Data[index-1]; }
            set { throw new ReadOnlyException(); }
        }

        T IList<T>.this[int index]
        {
            get { return index == 0 ? Overwrite : Data[index-1]; }
            set { throw new ReadOnlyException(); }
        }
    }
}

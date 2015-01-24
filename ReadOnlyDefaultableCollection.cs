using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KeySAV2
{
    public class ReadOnlyDefaultableCollection<T> : ReadOnlyCollection<T>
    {
        public ReadOnlyDefaultableCollection(IList<T> list) : base(list) {}

        new public T this[int i]
        {
            get
            {
                if (i >= Count)
                    return default(T);
                return (this as ReadOnlyCollection<T>)[i];
            }
        }

    }
}

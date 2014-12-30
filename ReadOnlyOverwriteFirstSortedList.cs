using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeySAV2
{
    public class ReadOnlyOverwriteFirstSortedList<T> : ReadOnlyOverwriteFirstList<T>
    {
        private IList<T> _org;
        private ComboBox _parent;
         
        public override IList<T> Data
        {
            get { return _data; }
            set
            {
                if (_data.Count != value.Count)
                    throw new IndexOutOfRangeException();
                List<T> tmp = SortWithoutFirst(value);
                int orgIndex = _parent.SelectedIndex == 0 ? 0 : _org.IndexOf(_data[_parent.SelectedIndex]);
                _org = value;
                _data = tmp;
                OnListChanged();
                _parent.SelectedItem = value[orgIndex];
            }
        } 
        public ReadOnlyOverwriteFirstSortedList(IList<T> data, T overwrite, ComboBox parent) : base(data, overwrite)
        {
            _data = SortWithoutFirst(data);
            _parent = parent;
        }

        private static List<T> SortWithoutFirst(IList<T> inList)
        {
            List<T> tmp = new List<T>(inList);
            tmp.RemoveAt(0);
            tmp.Sort();
            tmp.Insert(0, inList[0]);
            return tmp;
        }

    }
}

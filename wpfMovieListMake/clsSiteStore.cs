using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace wpfMovieListMake
{
    class SiteStoreLabelComparer : EqualityComparer<SiteStore>
    {
        public override bool Equals(SiteStore myData1, SiteStore myData2)
        {
            if (Object.Equals(myData1, myData2))
            {
                return true;
            }

            if (myData1 == null || myData2 == null)
            {
                return false;
            }

            return (myData1.Label == myData2.Label);
        }

        public override int GetHashCode(SiteStore myData)
        {
            return myData.Label.GetHashCode();
        }
    }
    public class SiteStore : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Explanation { get; set; }
        public string Label { get; set; }
        public int Kind { get; set; }

        private bool _IsNotExists;
        public bool IsNotExists
        {
            get
            {
                return _IsNotExists;
            }
            set
            {
                _IsNotExists = value;
                NotifyPropertyChanged("IsNotExists");
            }
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get
            {
                return _IsSelected;
            }
            set
            {
                _IsSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }
    }
}

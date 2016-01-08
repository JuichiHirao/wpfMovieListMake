using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace wpfMovieListMake
{
    class DisplayFile : TargetFile
    {
        private long _Size;
        public long Size
        {
            get
            {
                return _Size;
            }
            set
            {
                _Size = value;
            }
        }

        private string _DispSize;
        public string DispSize
        {
            get
            {
                return _DispSize;
            }
            set
            {
                _DispSize = value;
            }
        }

        public string DirName { get; set; }
    }
    class TargetFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        protected bool _IsSelected;
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

        protected DirectoryInfo _DirInfo;
        public DirectoryInfo DirInfo
        {
            get
            {
                return _DirInfo;
            }
            set
            {
                _DirInfo = value;
                Name = _DirInfo.Name;
            }
        }
        public string Name { get; set; }

        /// <summary>
        /// 動画件数はサイトにより「9/8」などにするので文字列を用意（画像JPEGのみなので不要）
        /// </summary>
        protected string _strMovieCount = "";
        public string strMovieCount
        {
            get
            {
                return _strMovieCount;
            }
            set
            {
                _strMovieCount = value;
                NotifyPropertyChanged("strMovieCount");
            }
        }
        protected int _MovieCount = 0;
        public int MovieCount
        {
            get
            {
                return _MovieCount;
            }
            set
            {
                _MovieCount = value;
                NotifyPropertyChanged("MovieCount");
            }
        }
        protected int _PhotoCount = 0;
        public int PhotoCount
        {
            get
            {
                return _PhotoCount;
            }
            set
            {
                _PhotoCount = value;
                NotifyPropertyChanged("PhotoCount");
            }
        }
        protected string _strListUpdateDate;
        public string strListUpdateDate
        {
            get
            {
                return _strListUpdateDate;
            }
            set
            {
                _strListUpdateDate = value;
                NotifyPropertyChanged("strListUpdateDate");
            }
        }
        protected DateTime _ListUpdateDate;
        public DateTime ListUpdateDate
        {
            get
            {
                return _ListUpdateDate;
            }
            set
            {
                _ListUpdateDate = value;
                if (value == null)
                    strListUpdateDate = "";
                else
                    strListUpdateDate = ListUpdateDate.ToString("yyyy/MM/dd HH:mm:ss");

                NotifyPropertyChanged("ListUpdateDate");
            }
        }
        protected string _strMovieNewDate;
        public string strMovieNewDate
        {
            get
            {
                return _strMovieNewDate;
            }
            set
            {
                _strMovieNewDate = value;
                NotifyPropertyChanged("strMovieNewDate");
            }
        }
        protected DateTime _MovieNewDate;
        public DateTime MovieNewDate
        {
            get
            {
                return _MovieNewDate;
            }
            set
            {
                _MovieNewDate = value;
                if (value == null)
                    strMovieNewDate = "";
                else
                {
                    if (_MovieNewDate.Year == 1900)
                        strMovieNewDate = "";
                    else
                        strMovieNewDate = MovieNewDate.ToString("yyyy/MM/dd HH:mm:ss");
                }

                NotifyPropertyChanged("MovieNewDate");
            }
        }
        protected string _Message;
        public string Message
        {
            get
            {
                return _Message;
            }
            set
            {
                if (_Message == null)
                    _Message = value;
                else if (_Message.Length > 0)
                    _Message = _Message + "、" + value;
                else
                    _Message = value;
                NotifyPropertyChanged("Message");
            }
        }
    }
}

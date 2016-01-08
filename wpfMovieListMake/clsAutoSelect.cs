using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;

namespace wpfMovieListMake
{
    class AutoSelect
    {
        public int No { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Extention { get; set; }
        public string Folder { get; set; }
    }

    class AutoSelectXmlControl
    {
        public static string XmlFilename = "AUTOSELECT_SETTING.xml";
        public static List<AutoSelect> GetList()
        {
            List<AutoSelect>  listAutoSelect = new List<AutoSelect>();
            //string ListPathname = "AUTOSELECT_SETTING.xml";

            XElement root = null;

            try
            {
                root = XElement.Load(XmlFilename);
            }
            catch (FileNotFoundException)
            {
                //                _logger.Debug("FileNotFoundException [" + ListPathname + "] GetDownloadListFromXmlFile");
                root = new XElement("AutoSelectSetting");
            }

            var listAll = from element in root.Elements("List")
                          select element;

            foreach (XContainer xcon in listAll)
            {
                AutoSelect autosel = new AutoSelect();

                try
                {
                    autosel.Name = xcon.Element("名前").Value;
                    autosel.No = Convert.ToInt32(xcon.Element("No").Value);
                    autosel.Kind = xcon.Element("種類").Value;
                    autosel.Extention = xcon.Element("拡張子").Value;
                    autosel.Folder = xcon.Element("フォルダ").Value;
                }
                catch (NullReferenceException)
                {
                    //_logger.Error("項目取得エラー");
                    // XML内にElementが存在しない場合に発生、無視する
                }

                listAutoSelect.Add(autosel);
            }

            return listAutoSelect;
        }

        public static void Save(List<AutoSelect> myList, AutoSelect myData)
        {
            if (myData.No == -1)
            {
                myData.No = myList.Count() + 1;
                myList.Add(myData);
            }
            else
            {
                foreach (AutoSelect data in myList)
                {
                    if (myData.No == data.No)
                    {
                        data.Name = myData.Name;
                        data.Kind = myData.Kind;
                        data.Extention = myData.Extention;
                        data.Folder = myData.Folder;
                    }
                }
            }

            XElement root = null;

            root = new XElement("AutoSelectSetting");

            foreach (AutoSelect data in myList)
            {
                root.Add(new XElement("List"
                                    , new XElement("No", data.No)
                                    , new XElement("名前", data.Name)
                                    , new XElement("種類", data.Kind)
                                    , new XElement("拡張子", data.Extention)
                                    , new XElement("フォルダ", data.Folder)
                            ));
            }

            //int i = 0;
            root.Save(XmlFilename);
        }

        public static void Delete(List<AutoSelect> myList, AutoSelect myData)
        {
            foreach (AutoSelect data in myList)
            {
                if (myData.No == data.No)
                {
                    myList.Remove(data);
                    break;
                }
            }

            XElement root = null;

            root = new XElement("AutoSelectSetting");

            foreach (AutoSelect data in myList)
            {
                root.Add(new XElement("List"
                                    , new XElement("No", data.No)
                                    , new XElement("名前", data.Name)
                                    , new XElement("種類", data.Kind)
                                    , new XElement("拡張子", data.Extention)
                                    , new XElement("フォルダ", data.Folder)
                            ));
            }

            //int i = 0;
            root.Save(XmlFilename);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace wpfMovieListMake
{
    /// <summary>
    /// winMakeFolder.xaml の相互作用ロジック
    /// </summary>
    public partial class winMakeFolder : Window
    {
        public winMakeFolder(string mySourceParentFolder)
        {
            InitializeComponent();

            label1.Content = mySourceParentFolder;
            txtFolderName.Focus();
        }

        private void btnMakeExecute_Click(object sender, RoutedEventArgs e)
        {
            string FolerPathname = System.IO.Path.Combine(label1.Content.ToString(), txtFolderName.Text);

            if (Directory.Exists(label1.Content.ToString()))
            {
                Directory.CreateDirectory(FolerPathname);
            }

            if (chkCopyFilePathText.IsChecked == true)
                Clipboard.SetText(FolerPathname);

            this.Close();
        }
    }
}

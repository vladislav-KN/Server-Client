using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClientWPF
{
    /// <summary>
    /// Логика взаимодействия для ipAddresPortSaver.xaml
    /// </summary>
    public partial class ipAddresPortSaver : Window
    {
        string ipAddress;
        int port;
        public string Address
        {
            get
            {
                return ipAddress;
            }
        }
        public int Port
        {
            get
            {
                return port;
            }
        }

        public ipAddresPortSaver()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ipAddress = myUpDownControl1.Value + "." + myUpDownControl_Copy4.Value + "." + myUpDownControl_Copy5.Value + "." + myUpDownControl_Copy6.Value;
            port = int.Parse(myUpDownControl_Copy7.Value.ToString());
            this.DialogResult = true;
        }
    }
}

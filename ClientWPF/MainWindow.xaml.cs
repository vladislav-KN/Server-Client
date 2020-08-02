using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ClientWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string fileName;
        static Settings Settings;
        static string newFileName;
        public MainWindow()
        {
            InitializeComponent();
            bool fileEx = File.Exists(Environment.CurrentDirectory + "\\set.xml");
            Settings = new Settings();
            //выполняем проверку существования настроек
            if (!fileEx)//отключаем кнопки и выводим сообщение если файл с настройками отсутствует
            {
                sendFile_Button.IsEnabled = false;
                chouseFile_Button.IsEnabled = false;
                Lable1.IsEnabled = false;
                MessageBox.Show("Ip адрес и порт не задан. Задайте ip адрес и порт через настройки.", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                //загружаем настройки из файла
                try
                {
                    Settings.ReadXml();
                }
                //если не получилось удаляем файл set.xml и пишем ошибку
                catch
                {
                    File.Delete(Environment.CurrentDirectory + "\\set.xml");
                    MessageBox.Show("Настройки Ip адреса и порта заданы не корректно. Задайте ip адрес и порт через настройки.", "Ошибка чтения файла", MessageBoxButton.OK, MessageBoxImage.Error);
                    sendFile_Button.IsEnabled = false;
                    chouseFile_Button.IsEnabled = false;
                    Lable1.IsEnabled = false;
                }
            }

        }
        //
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                fileName = dlg.FileName;
                Lable1.Content = fileName;
            }
            
        }
        // при 2-ном нажатии на label выполняется обработка нажатия на кнопку "Выбрать файл"
        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Button_Click(sender, e);
        }
        //меняем цвет и ширину рамок 
        private void Label_MouseEnter(object sender, MouseEventArgs e)
        {
            Lable1.BorderBrush = Brushes.SlateBlue;
            Lable1.BorderThickness = new Thickness(2);
            
        }
        private void Lable1_MouseLeave(object sender, MouseEventArgs e)
        {
            Lable1.BorderThickness = new Thickness(0);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (File.Exists(fileName))
            {
                Task.Run(async () =>
                {
                    long fileSize;
                    // получаем адреса для запуска сокета
                    IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(Settings.Fields.ipAddres), Settings.Fields.port);
                    // создаем сокет
                    Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    server.Connect(ipPoint);
                    byte[] data = new byte[256]; // буфер для ответа
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0; // количество полученных байт
                                   //получаем сообщение о возможности подключения
                    do
                    {
                        bytes = server.Receive(data, data.Length, 0);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (server.Available > 0);
                    Console.WriteLine(builder.ToString());
                    if (builder.ToString() == "Сервер переполнен, попытайтесь подключится позднее!")
                    {
                        MessageBox.Show(builder.ToString(), "Ошибка подключения к серверу", MessageBoxButton.OK, MessageBoxImage.Information);
                        server.Shutdown(SocketShutdown.Both);
                        server.Close();
                    }
                    else
                    {
                        //обрабатываем 
                        byte[] fileNameByte = Encoding.ASCII.GetBytes(fileName);//преобразуем имя файла в байты
                        byte[] fileNameBLen = BitConverter.GetBytes(fileName.Length); //Преобразуем длину имени файла в байты
                        byte[] fileData = File.ReadAllBytes(fileName);//преобразуем файл в байты
                        byte[] sendData = new byte[4 + fileNameByte.Length + fileData.Length];//выделяем место для отправки всей информации
                        fileNameBLen.CopyTo(sendData, 0);//первые 4 байта занимаем под информацию о длине файла
                        fileNameByte.CopyTo(sendData, 4);//заполняем информацией о файле
                        fileData.CopyTo(sendData, 4 + fileNameByte.Length);//оставшиеся место заполняем данным из файла
                        server.Send(sendData);
                        Application.Current.Dispatcher.BeginInvoke(
                          DispatcherPriority.Background,
                          new Action(() => this.Stage_Lable.Content = "Файл отправлен"));
                        Thread.SpinWait(1000000);
                        newFileName = System.IO.Path.GetDirectoryName(fileName) + "\\New_" + System.IO.Path.GetFileName(fileName);
                        FileStream file = new FileStream(newFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                        int len = 1024 * 5000;
                        var buffer = new byte[len];
                        int bytesRead;
                        //получаем файл обратно
                        Application.Current.Dispatcher.BeginInvoke(
                          DispatcherPriority.Background,
                          new Action(() => this.Stage_Lable.Content = "Получаем файл"));
                        while ((bytesRead = server.Receive(buffer, 0, buffer.Length, SocketFlags.None)) > 0)
                        {
                            file.Write(buffer, 0, bytesRead);
                        }
                        file.Close();
                        Application.Current.Dispatcher.BeginInvoke(
                          DispatcherPriority.Background,
                          new Action(() => this.Stage_Lable.Content = "Файл получен"));
                        server.Shutdown(SocketShutdown.Both);
                        server.Close();
                        Thread.SpinWait(1000000);
                        Application.Current.Dispatcher.BeginInvoke(
                          DispatcherPriority.Background,
                          new Action(() => this.openFile_Button.IsEnabled = true));
                    }

                });
                
            }
        }
        //обработчик нажатия на кнопку "Открыть файл"
        private void openFile_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(newFileName);
        }
        //обработчик нажатия на кнопку меню
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ipAddresPortSaver ipAddresPort = new ipAddresPortSaver();
            if (ipAddresPort.ShowDialog().Value)
            {
                Settings.Fields.ipAddres = ipAddresPort.Address;
                Settings.Fields.port = ipAddresPort.Port;
                sendFile_Button.IsEnabled = true;
                chouseFile_Button.IsEnabled = true;
                Lable1.IsEnabled = true;
                Settings.WriteXml();
            }
            
        }
    }
}

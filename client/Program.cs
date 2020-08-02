
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace client
{
    class Program
    {
        //порт и адрес сервера в локальной сети
        static Settings Settings = new Settings();
        static void NewSettings()
        {
            Settings = new Settings();
            do
            {
                Console.Write("Введите ip адрес сервера: ");
                IPAddress iPAddress;
                bool ok = IPAddress.TryParse(Console.ReadLine(), out iPAddress);
                Settings.Fields.ipAddres = iPAddress.ToString();
                if (ok) break;
            } while (true);
            do
            {
                Console.Write("Введите порт сервера: ");
                bool ok = int.TryParse(Console.ReadLine(), out Settings.Fields.port);
                if (ok && Settings.Fields.port > 1025 && Settings.Fields.port <= 65535) break;
            } while (true);
            Settings.WriteXml();
        }

        static void Main(string[] args)
        {
            int iter = 0;
            ConsoleKeyInfo btn;
            bool fileEx = File.Exists(Environment.CurrentDirectory + "\\set.xml");
            if (!fileEx)
            {
                NewSettings();
            }
            else
            {
                bool ok;
                do
                {
                    Console.Write("Обновить настройки (Y/N): ");
                    string answer = Console.ReadLine();
                    if (answer.ToLower() == "y")
                    {
                        ok = true;
                        NewSettings();
                    }
                    else if (answer.ToLower() == "n")
                    {
                        ok = true;
                        Settings.ReadXml();
                    }
                    else
                    {
                        ok = false;
                    }
                } while (!ok);
            }
            
            while (true)
            {
                string fileName;
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
                    server.Shutdown(SocketShutdown.Both);
                    server.Close();
                    break;
                }
                else
                {
                    //обрабатываем 
                    do
                    {
                        Console.Write("Введите путь до файла: ");
                        fileName = Console.ReadLine();
                        try
                        {
                            fileSize = new System.IO.FileInfo(fileName).Length;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                    } while (true);

                    byte[] fileNameByte = Encoding.ASCII.GetBytes(fileName);//преобразуем имя файла в байты
                    byte[] fileNameBLen = BitConverter.GetBytes(fileName.Length); //Преобразуем длину имени файла в байты
                    byte[] fileData = File.ReadAllBytes(fileName);//преобразуем файл в байты
                    byte[] sendData = new byte[4 + fileNameByte.Length + fileData.Length];//выделяем место для отправки всей информации
                    fileNameBLen.CopyTo(sendData, 0);//первые 4 байта занимаем под информацию о длине файла
                    fileNameByte.CopyTo(sendData, 4);//заполняем информацией о файле
                    fileData.CopyTo(sendData, 4 + fileNameByte.Length);//оставшиеся место заполняем данным из файла
                    server.Send(sendData);
                    string newFileName = Path.GetDirectoryName(fileName) + "\\New_" + iter+ Path.GetFileName(fileName);
                    iter++;
                    FileStream file = new FileStream(newFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    //выделяем память
                    int len = 1024 * 5000;
                    var buffer = new byte[len];
                    int bytesRead;
                    //получаем файл обратно
                    while ((bytesRead = server.Receive(buffer, 0, buffer.Length, SocketFlags.None)) > 0)
                    {
                        file.Write(buffer, 0, bytesRead);
                    }
                    file.Close();
                    server.Shutdown(SocketShutdown.Both);
                    server.Close();
                    var p = new Process();
                    p.StartInfo = new ProcessStartInfo(newFileName)
                    {
                        UseShellExecute = true
                    };
                    p.Start();
                    Console.WriteLine("Для продолжения нажмите любую кнопку. Для остановки нажмите ESC.");
                    btn = Console.ReadKey();
                    if (btn.Key == ConsoleKey.Escape) break;
                }
            }
        }   
    }
   
}

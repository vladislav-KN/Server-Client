using System;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;

namespace Server
{
    class Program
    {
        static List<Connection> connection = new List<Connection>();
        static readonly byte[] CONNECTION_REFUSED = Encoding.Unicode.GetBytes("Сервер переполнен, попытайтесь подключится позднее!");
        static readonly byte[] CONNECTION_ACCEPTED = Encoding.Unicode.GetBytes("Подключение к серверу выполнено!");
        static Settings Settings = new Settings();

        static int limit;
        //Функция для обработки ошибки ввода числа
        //на вход ничего не принимает нужна для избежания вылета сервера при вводе параметра N
        static int NEnter()
        {
            int N;
            do
            {
                Console.Write("Введите число N: ");
                //Проверяем ввод числа N
                bool ok = int.TryParse(Console.ReadLine(), out N);
                if (ok) break;//если проверка успешна выходим из цикла
            } while (true);
            return N;
        }
        
        static void NewSettings()
        {
            Settings = new Settings();
            do
            {
                Console.Write("Введите ip адрес для сервера: ");
                IPAddress iPAddress;
                bool ok = IPAddress.TryParse(Console.ReadLine(), out iPAddress);
                Settings.Fields.ipAddres = iPAddress.ToString();
                if (ok) break;
            } while (true);
            do
            {
                Console.Write("Введите порт для сервера: ");
                bool ok = int.TryParse(Console.ReadLine(), out Settings.Fields.port);
                if (ok && Settings.Fields.port > 1025 && Settings.Fields.port <= 65535) break;
            } while (true);
            Settings.WriteXml();
        }




        static void Main(string[] args)
        {
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
            limit = NEnter();//задаём параметр N
            //Получаем адрес
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(Settings.Fields.ipAddres), Settings.Fields.port);
            //создаем сокет
            Socket listenSocet = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //Связываем сокет с адресом
                listenSocet.Bind(ipPoint);
                int counter = 0;
                //начинаем прослушивание
                listenSocet.Listen(limit);

                while (true)
                {
                    counter++;

                    Socket client = listenSocet.Accept();

                    //получаем сообщение 
                    StringBuilder builder = new StringBuilder();
                    try {
                        //создаем новый поток
                        new Thread(delegate ()
                        {
                            //проверяем количество клиентов в очереди
                            if (connection.Count <= limit)
                            {
                                //отправляем информацию об успешном подключении
                                client.Send(CONNECTION_ACCEPTED);
                                connection.Add(new Connection(client, counter));//добавляем клиента в очередь
                                int i = connection.Count - 1;
                                while (connection.Any())
                                {
                                    Program.connection[i].RequestFileHandler();//выполняем 
                                    ChecUssers();
                                    break;
                                }
                            }
                            else
                            {
                                //отправляем информацию о неудачном подключении
                                client.Send(CONNECTION_REFUSED);
                                //разрываем связь
                                new Connection(client, counter).Abort();
                            }
                           
                        }).Start();
                    }catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        foreach(Connection connection in connection)
                        {
                            Console.WriteLine(((IPEndPoint)client.RemoteEndPoint).Address.ToString());
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        //обновляем очередь из подключенных клиентов
        //удаляем из очереди тех клиентов, которые уже отключились
        static void ChecUssers()
        {
            List<Connection> buf1 = new List<Connection>(connection);
            for(int i = 0; i < buf1.Count; i++)
            {
                if (buf1[i].Client.Connected)
                {
                    buf1.Remove(buf1[i]);
                }
            }
            
            connection = new List<Connection>(buf1.Distinct());
        }
    }
}


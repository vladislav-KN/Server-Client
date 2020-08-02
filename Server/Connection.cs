using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    //не наследуемый класс содержит информацию о клиенте его номер, сокет и информацию об окончании подключения
    public sealed class Connection
    {
        readonly Socket client;
        bool compleated = false;
        int counter;
        
        public Socket Client
        {
            get
            {
                return client;
            }
        }
        
        public Connection(Socket client, int number)
        {
            this.client = client;
            counter = number;
        }

        //функция отключения клиента
        public void Abort()
        {
            client.Close();
            compleated = true;
        }


        public int GetCount
        {
            get
            {
                return counter;
            }
        }
        public bool Finished
        {
            get
            {
                return compleated;
            }
        }

        //обработка файла 
        public void RequestFileHandler()
        {
            try {
                //получаем файл от клиента
                byte[] clientData = new byte[1024 * 5000];
                int recivedBytesLen = client.Receive(clientData);
                int fileNameLen = BitConverter.ToInt32(clientData, 0);
                string fileName = Encoding.ASCII.GetString(clientData, 4, fileNameLen);
                fileName = fileName.Remove(fileName.LastIndexOf('.')) + counter + ".txt";
                BinaryWriter bWrite = new BinaryWriter(File.Open(fileName, FileMode.Create));
                bWrite.Write(clientData, 4 + fileNameLen, recivedBytesLen - 4 - fileNameLen);
                bWrite.Close();

                //открываем присланный файл и создаем новый для отправки
                StreamReader readFile = new StreamReader(fileName);
                StreamWriter sendingFile = new StreamWriter(File.Open("Send" + counter + ".txt", FileMode.Create));
                //проверяем слова 
                while (!readFile.EndOfStream)
                {
                    string line = readFile.ReadLine();
                    string addToFile = line + (IsPalindrom(line) ? " - слово палиндром" : " - слово не палиндром");
                    sendingFile.WriteLine(addToFile);
                }

                sendingFile.Close();
                readFile.Close();

                // подготавливаем файл для отправки
                byte[] fileNameByte = Encoding.ASCII.GetBytes("Send" + counter + ".txt");
                byte[] fileNameBLen = BitConverter.GetBytes(("Send" + counter + ".txt").Length);
                byte[] fileData = File.ReadAllBytes("Send" + counter + ".txt");
                byte[] sendData = new byte[4 + fileNameByte.Length + fileData.Length];
                fileNameBLen.CopyTo(sendData, 0);
                fileNameByte.CopyTo(sendData, 0);
                fileData.CopyTo(sendData, 0);

                Thread.SpinWait(100000000);//ставим таймаут для клиента
                //отправляем файл
                client.Send(sendData);
                //удаляем существующие
                File.Delete(fileName); 
                File.Delete("Send" + counter + ".txt");
                this.Abort();
            }catch
            {
                Abort();
            }
        }
        //Функция определяет палиндром ли слово
        //На вход получает слово которое нужно проверить
        static bool IsPalindrom(string word)
        {
            char[] arr = word.ToCharArray();//преобразуем слово в массив 
            Array.Reverse(arr);// переворачиваем массив
            string rWord = new string(arr);// преобразуем обратно
            return rWord == word;
        }
    }
}

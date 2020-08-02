using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace client
{
    /// <summary>
    /// Класс настроек
    /// Сохраняет настройки (ip адрес и порт) в xml файл 
    /// </summary>
    public class SettingsFields
    {
        //путь к файлу
        public readonly String XMLFileName = Environment.CurrentDirectory + "\\set.xml";
        //переменные для сохранения 
        public string ipAddres = @"127.0.0.1";
        public int port = 1;
    }
    public class Settings
    {
            public SettingsFields Fields;

            public Settings()
            {
                Fields = new SettingsFields();
            }
            //Запись настроек в файл
            public void WriteXml()
            {
                XmlSerializer ser = new XmlSerializer(typeof(SettingsFields));
                TextWriter writer = new StreamWriter(Fields.XMLFileName);
                ser.Serialize(writer, Fields);
                writer.Close();
            }
            //Чтение насроек из файла
            public void ReadXml()
            {
                if (File.Exists(Fields.XMLFileName))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(SettingsFields));
                    TextReader reader = new StreamReader(Fields.XMLFileName);
                    Fields = ser.Deserialize(reader) as SettingsFields;
                    reader.Close();
                }
            }
    }
}


using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using NLog;
using Npgsql;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Xml;
using System.Xml.Linq;

namespace Test
{
    class Program
    {
        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            if (args.Length == 0)   //проверка аргумента
            {
                XDocument xmlDoc = new XDocument(
               new XElement("SiteTry",
                       new XAttribute("type", "Site"),
                       new XElement("Site", ""),
                       new XElement("available", "")));
                xmlDoc.Save("SiteTry.xml"); //запись новой проверки

                XDocument xmldoc = XDocument.Load("SiteTry.xml");

                var Confdata = ConfigurationManager.AppSettings; //получение данных из конфигурационного файла
                foreach (var key in Confdata.AllKeys) //перебор данных из конф. файла
                {
                    if (Confdata.Get(key).Contains("https")) //получение данных сайтов из конф. файла
                    {
                        logger.Trace("Адрес проверки: " + Confdata.Get(key));
                        

                        XElement? root = xmldoc.Element("SiteTry");
                        if (testSite(Confdata.Get(key)) == true)  //проверка на доступность сайта
                        {
                            root.Add(new XElement("SiteTry",
                            new XAttribute("type", "Site"),
                            new XElement("Site", Confdata.Get(key)),
                            new XElement("available", testSite(Confdata.Get(key))
                            )));
                            Console.WriteLine(Confdata.Get(key) + " Сайт доступен");
                            logger.Trace("Адрес проверки: " + Confdata.Get(key) + " Результат: сайт доступен");



                        }
                        else { 
                        Console.WriteLine(Confdata.Get(key) + " Сайт не доступен");

                        root.Add(new XElement("SiteTry",
                        new XAttribute("type", "Site"),
                        new XElement("Site", Confdata.Get(key)),
                        new XElement("available", testSite(Confdata.Get(key))
                        )));
                        logger.Trace("Адрес проверки: " + Confdata.Get(key) + " Результат: сайт не доступен");
                        }
                    }
                    if (Confdata.Get(key).Contains("Host")) //проверка на подключение к бд
                    {

                        XElement? root2 = xmldoc.Element("SiteTry");
                        root2.Add(new XElement("dBTry",
                               new XAttribute("type", "dB"),
                               new XElement("dB", Confdata.Get(key)),
                               new XElement("available", testDb(Confdata.Get(key)
                               ))));


                    }
                    xmldoc.Save("SiteTry.xml");
                    if (Confdata.Get(key).Contains("@")) //получение эмеил-адреса для отправки
                    {
                        SendMail("smtp.mail.ru", "testforjob2022@mail.ru", "iZgSFwGHxn0YgtPJ6kv0", Confdata.Get(key), "Тема письма", "Отчет о работе", "SiteTry.xml");
                        logger.Trace("Адрес отправки: " + Confdata.Get(key) + " Результат: письмо отправленно");
                        Console.WriteLine("Адрес отправки: " + Confdata.Get(key) + " Результат: письмо отправленно");


                    }
                }

            }
            else //условие для действия с параметром 
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load("SiteTry.xml");
                                
                XmlElement? xRoot = xDoc.DocumentElement;
                if (xRoot != null)
                {
                    // обход всех узлов в корневом элементе
                    foreach (XmlElement xnode in xRoot)
                    {
                        // получаем атрибут Site
                        XmlNode? attr = xnode.Attributes.GetNamedItem("Site");

                        // обходим все дочерние узлы элемента Site
                        foreach (XmlNode childnode in xnode.ChildNodes)
                        {
                            // если узел - Site
                            if (childnode.Name == "Site")
                            {
                                Console.WriteLine($"Site: {childnode.InnerText}");
                            }
                            // если узел dB 

                            if (childnode.Name == "dB")
                            {
                                Console.WriteLine($"db: {childnode.InnerText}");
                            }
                            // если узел available
                            if (childnode.Name == "available")
                            {
                                Console.WriteLine($"available: {childnode.InnerText}");
                            }
                           
                        }
                                                
                        Console.WriteLine();
                    }

            }




        }
        }

        static bool testSite(string url) //проверка подключения
        {

            Uri uri = new Uri(url);
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(uri);
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch
            {

                return false;
            }
            return true;
        }


        static bool testDb(string db) //проверка подключение к бд
        {

            try
            {
                using var con = new NpgsqlConnection(db);
                con.Open();

                logger.Trace("Бд доступна " + db);

            }
            catch
            {
                Console.WriteLine("Бд не доступна ");

                logger.Error("Бд не доступна " + db);

                return false;
            }
            Console.WriteLine("Бд доступна");
            return true;
        }


        public static void SendMail(string smtpServer, string from, string password, //отправка сообщения на почту
        string mailto, string caption, string message, string attachFile = null)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(from);
                mail.To.Add(new MailAddress(mailto));
                mail.Subject = caption;
                mail.Body = message;
                if (!string.IsNullOrEmpty(attachFile))
                    mail.Attachments.Add(new Attachment(attachFile));
                SmtpClient client = new SmtpClient();
                client.Host = smtpServer;
                client.Port = 587;
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(from.Split('@')[0], password);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Send(mail);
                mail.Dispose();
            }
            catch (Exception e)
            {
                logger.Error("Результат: письмо не отправленно");

                throw new Exception("Mail.Send: " + e.Message);

            }
        }






    }
}


using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using System.Xml;

[assembly: AssemblyTitle(Core.Program.assemblyTitle)]
[assembly: AssemblyDescription(Core.Program.assemblyDescription)]
[assembly: AssemblyCopyright(Core.Program.assemblyCopyright)]
[assembly: ComVisible(false)]
[assembly: Guid(Core.Program.assemblyGUID)]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyVersion("1.0.*")]

namespace Core
{
    class Program
    {
        public static bool isPauseAfterExit = true;
        public const string assemblyTitle = "1c xml dumper";
        public const string assemblyDescription = "Dump .xml data conversion files ...";
        public const string assemblyCopyright = "Written by de1ta0ne";
        public const string assemblyGUID = "4C125770-06AF-43C7-B243-39FCE3492BD6";
        private static Version _assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        public static readonly string assemblyVersion = _assemblyVersion.Major.ToString() + "." + _assemblyVersion.Minor.ToString() + " build " + _assemblyVersion.Build;
        public static readonly string assemblyDate = (new DateTime(2000, 1, 1).AddDays(_assemblyVersion.Build).AddSeconds(_assemblyVersion.Revision * 2)).ToString("dd.MM.yyyy");
        public static readonly string version = "v" + assemblyVersion + " [" + assemblyDate + "]";

        public static readonly string assemblyFile = Assembly.GetExecutingAssembly().Location;
        public static readonly string assemblyFolder = Path.GetDirectoryName(assemblyFile) + Path.DirectorySeparatorChar;
        public static readonly string startupFolder = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;

        public static List<string> logLines = new List<string>();

        public static void print(string message)
        {
            Console.Write(message + "\n");
            logLines.Add(message);
        }

        public static void print(string format, params object[] paramList)
        {
            Console.Write(String.Format(format, paramList) + "\n");
            logLines.Add(String.Format(format, paramList));
        }

        public enum EntryType
        {
            Document,
            Dictionary,
            Other
        }

        public class Entry
        {
            public string npp;
            public EntryType type;
            public string typeName;
            public DateTime date;
            public string number;
            public string name;
        }

        static void _dump(string file)
        {
            if (!File.Exists(file))
            {
                print("ERROR: {0} not found !", file);
                return;
            }

            var _xml = new XmlDocument();

            try
            {
                //_xmlOriginal.PreserveWhitespace = true;
                print("Loading: '{0}'!", file);
                _xml.Load(file);
            }
            catch (Exception ex)
            {
                print("ERROR: {0}", ex.Message);
                return;
            }

            var result = new Dictionary<string, int>();
            var objects = new Dictionary<string, List<Entry>>();
                

            var nsmgr_o = new XmlNamespaceManager(_xml.NameTable);
            nsmgr_o.AddNamespace("n", _xml.DocumentElement.NamespaceURI);

            var nodeList = _xml.SelectNodes("/n:ФайлОбмена/n:Объект", nsmgr_o);
            foreach (XmlNode node in nodeList)
            {
                string typeName = node.Attributes["Тип"].Value;
                if (result.ContainsKey(typeName)) result[typeName] += 1;
                else result[typeName] = 1;

                if (!objects.ContainsKey(typeName)) objects[typeName] = new List<Entry>();                

                Entry entry = new Entry();                
                entry.typeName = typeName;
                entry.npp = node.Attributes["Нпп"].Value;

                if(typeName.StartsWith("СправочникСсылка.")) entry.type = EntryType.Dictionary;
                else if(typeName.StartsWith("ДокументСсылка.")) entry.type = EntryType.Document;
                else entry.type = EntryType.Other;

                XmlNode n;
                n = node.SelectSingleNode("n:Ссылка/n:Свойство[@n:Имя='Дата'][@n:Тип='Дата']", nsmgr_o);
                if (n == null) n = node.SelectSingleNode("n:Свойство[@n:Имя='Дата'][@n:Тип='Дата']", nsmgr_o);
                try
                {
                    if (n != null) entry.date = DateTime.Parse(n.InnerText);  //print("=> " + n.Attributes["Имя"].Value);
                }
                catch (System.Exception ex)
                {
                    // print("ERROR: Can't parse into date '" + n.InnerText + "'\n\t" + ex.Message);
                }                

                n = node.SelectSingleNode("n:Ссылка/n:Свойство[@n:Имя='Номер'][@n:Тип='Строка']", nsmgr_o);                
                if (n == null) n = node.SelectSingleNode("n:Ссылка/n:Свойство[@n:Имя='Код'][@n:Тип='Строка']", nsmgr_o);
                if (n == null) n = node.SelectSingleNode("n:Свойство[@n:Имя='Номер'][@n:Тип='Строка']", nsmgr_o);                
                if (n == null) n = node.SelectSingleNode("n:Свойство[@n:Имя='Код'][@n:Тип='Строка']", nsmgr_o);                
                if (n != null) entry.number = n.InnerText.Trim();

                n = node.SelectSingleNode("n:Ссылка/n:Свойство[@n:Имя='Наименование'][@n:Тип='Строка']", nsmgr_o);
                if (n == null) n = node.SelectSingleNode("n:Свойство[@n:Имя='Наименование'][@n:Тип='Строка']", nsmgr_o);                
                if (n != null) entry.name = n.InnerText.Trim();

                objects[typeName].Add(entry);
            }

            print("");

            var dump = new List<string>();
            var keys = new List<string>(result.Keys);            
            keys.Sort();
            
            foreach (var k in keys)
            {
                var line = "[" + result[k] + "]\t" + k;
                dump.Add(line);
                print(line);
            }

            dump.Add("\n");
            foreach (var k in keys)
            {
                dump.Add("\n[" + result[k] + "]\t" + k);                
                foreach (var e in objects[k].OrderBy(o => o.date).ToList())
                {
                    string date = (e.date == DateTime.MinValue ? "" : "[" + e.date.ToString("dd-MM-yyyy HH:mm:ss") + "]");
                    string name = (e.name == null ? "" : " '" + e.name + "'");
                    dump.Add("\t" + date + " (" + e.npp + ") " + k + " " + e.number + name);
                }
            }

            File.WriteAllLines(file + ".log", dump.ToArray());
        }

        static void Main(string[] args)
        {
            Console.WindowWidth = 120;
            Console.WindowHeight = 42;
            print(assemblyTitle + " " + version + "\n" + assemblyCopyright + "\n\nTask: " + assemblyDescription + "\n");

            if (args.Length != 1)
            {
                print("Usage:\n\n" +
                      "    1c-xml-dump.exe <XML File>\n");
            }
            else
            {
                _dump(args[0]);
                print("\nDone!");
            }
            
            if (isPauseAfterExit) Console.ReadKey(true);
        }
    }
}

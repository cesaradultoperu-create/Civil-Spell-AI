using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace CasillasField
{
    // An explicit per-user list. Opening a different drawing never grants access to its DST.
    public sealed class ProjectAccess
    {
        public string Name,Root,DatabasePath,SheetSet,Block;
        public bool EditValues;
        internal static string TestConfiguration;
        public static string LocalFolder=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CasillasField");
        public static string ConfigurationFile=>TestConfiguration??Path.Combine(LocalFolder,"projects.xml");
        static string cachedFile,cachedXml;
        static DateTime cachedStamp;
        static ProjectAccess[] cached=new ProjectAccess[0];
        static string Full(string path)=>Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
        internal static bool Within(string path,string folder)
        {
            if(String.IsNullOrWhiteSpace(path)||String.IsNullOrWhiteSpace(folder))return false;
            try{return Full(path).StartsWith(Full(folder)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);}
            catch(ArgumentException){return false;}catch(NotSupportedException){return false;}
        }
        internal static ProjectAccess[] Parse(string xml)
        {
            var document=new XmlDocument {XmlResolver=null};
            using(var reader=XmlReader.Create(new StringReader(xml),new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))document.Load(reader);
            return document.SelectNodes("/Projects/Project").Cast<XmlElement>().Select(p=>
            {
                var project=new ProjectAccess {Name=p.GetAttribute("name"),Root=Full(p.GetAttribute("root")),
                    DatabasePath=Full(p.GetAttribute("dst")),SheetSet=p.GetAttribute("sheetSet"),Block=p.GetAttribute("block"),
                    EditValues=p.GetAttribute("editValues")=="true"};
                Guid guid;
                if(!Within(project.DatabasePath,project.Root)||!Guid.TryParse(project.SheetSet.TrimStart('g','G'),out guid)||String.IsNullOrWhiteSpace(project.Block))
                    throw new InvalidDataException("Configuración de proyecto incompleta o DST fuera de su carpeta.");
                return project;
            }).ToArray();
        }
        static ProjectAccess[] Projects()
        {
            string file=ConfigurationFile;
            if(!File.Exists(file))return new ProjectAccess[0];
            var stamp=File.GetLastWriteTimeUtc(file);
            if(file!=cachedFile||stamp!=cachedStamp)
            {
                string xml=File.ReadAllText(file);
                if(file!=cachedFile||xml!=cachedXml){cached=Parse(xml);cachedXml=xml;}
                cachedFile=file;cachedStamp=stamp;
            }
            return cached;
        }
        public static ProjectAccess ForDrawing(string drawing)
        {
            if(!String.Equals(Path.GetExtension(drawing),".dwg",StringComparison.OrdinalIgnoreCase))return null;
            var matches=Projects().Where(p=>Within(drawing,p.Root)).ToArray();
            return matches.Length==1?matches[0]:null;
        }
        public bool Matches(FieldIdentity id)=>id!=null && id.Component.Length==0 &&
            String.Equals(Full(id.DatabasePath),DatabasePath,StringComparison.OrdinalIgnoreCase) &&
            String.Equals(id.SheetSet,SheetSet,StringComparison.OrdinalIgnoreCase);
        public static ProjectAccess Editable(string drawing,FieldIdentity id)
        {
            var project=ForDrawing(drawing);
            return project!=null && project.EditValues && project.Matches(id)?project:null;
        }
        public static string LogFile(string drawing)
        {
            // No helper logs, backups or configuration are written beside shared drawings.
            string key;
            using(var hash=SHA256.Create())key=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(drawing.ToUpperInvariant()))).Replace("-","").Substring(0,16);
            string folder=Path.Combine(LocalFolder,"Logs");Directory.CreateDirectory(folder);
            return Path.Combine(folder,Path.GetFileNameWithoutExtension(drawing)+"-"+key+".log");
        }
        internal static string PrepareBackup(string drawing,FieldIdentity id,string before,string after)
        {
            string folder=Path.Combine(LocalFolder,"Backups",DateTime.Now.ToString("yyyy-MM-dd"));Directory.CreateDirectory(folder);
            string stem=Path.Combine(folder,DateTime.Now.ToString("HHmmssfff")+"-"+Guid.NewGuid().ToString("N"));
            // FileShare.ReadWrite permits a snapshot of an open DST without touching its locks.
            using(var input=new FileStream(id.DatabasePath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
            using(var output=new FileStream(stem+".dst",FileMode.CreateNew,FileAccess.Write,FileShare.None))input.CopyTo(output);
            var document=new XmlDocument();var root=document.CreateElement("FieldValueChange");document.AppendChild(root);
            foreach(var entry in new[]{new[]{"Drawing",drawing},new[]{"DST",id.DatabasePath},new[]{"SheetSet",id.SheetSet},
                new[]{"Property",id.Property},new[]{"Before",before},new[]{"After",after},new[]{"Status","Prepared"},new[]{"UTC",DateTime.UtcNow.ToString("O")}})
            {var child=document.CreateElement(entry[0]);child.InnerText=entry[1];root.AppendChild(child);}
            document.Save(stem+".xml");return stem+".xml";
        }
        internal static void FinishBackup(string file,string status)
        {
            try{var document=new XmlDocument {XmlResolver=null};document.Load(file);document.SelectSingleNode("/FieldValueChange/Status").InnerText=status;document.Save(file);}
            catch(IOException){}catch(UnauthorizedAccessException){}
        }
    }
}

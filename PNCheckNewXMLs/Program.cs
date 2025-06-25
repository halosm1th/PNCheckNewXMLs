// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices.JavaScript;
using System.Xml;
using DefaultNamespace;
using HtmlAgilityPack;
using PNCheckNewXMLs;

class PNCheckerNewXmls
{
    private static Logger logger { get; set; }
    private static List<string> notFoundFiles { get; set; }
    
    public static void Main()
    {
        notFoundFiles = new List<string>();
        
        logger = new Logger();
        try
        {
            logger.Log("Starting program.\nTrying to find NewXMl Directory");
            Console.WriteLine("Starting program.\nTrying to find NewXMl Directory");
            var dirFinder = new XMLDirectoryFinder(logger);

            var startingDir = Directory.GetCurrentDirectory();
            var biblioDir = dirFinder.FindBiblioDirectory(startingDir);
            var newXml = dirFinder.FindXmlDirectory(startingDir);

            logger.Log($"Found NewXMl directory {newXml}, now starting XmlEntryGatherer to gather xml entries");
            Console.WriteLine($"Found NewXMl directory {newXml}, now starting XmlEntryGatherer to gather xml entries");
            var XmlEntryEGatherer = new XMLEntryGatherer(newXml, logger);

            var entries = XmlEntryEGatherer.GetEntriesFromFolder(newXml);
            logger.Log($"{entries.Count} New entries have been gathered, beginning to parse entries.");
            Console.WriteLine($"{entries.Count} New entries have been gathered, beginning to parse entries.");

            int index = 0;
            foreach (var entry in entries)
            {
                Console.WriteLine($"Processing entry {entry.BPNumber}");
                logger.Log($"Processing entry {entry.BPNumber}");
                HandleNewEntry(entry, biblioDir, index, entries.Count);
                index++;
            }

            logger.Log("Saving list of new files.");
            Console.WriteLine("Saving list of new files.");
            SaveNewFileList(startingDir);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            logger.LogError("There was an error in processor", e);
        }
    }

    static void SaveNewFileList(string saveDir)
    {
        string name = $"XmlFrom {DateTime.Today}({DateTime.Now.Hour}-{DateTime.Now.Minute})";
        var path = Path.Combine(saveDir, name);
        
        logger.LogProcessingInfo($"Saving list of new files to {path}");
        Console.WriteLine($"Saving list of new files to {path}");
        
        File.WriteAllLines(path, notFoundFiles);
    }

    static void HandleNewEntry(XMLDataEntry entry, string idpDataPath, int numb, int total)
    {
        logger.LogProcessingInfo($"Processing new entry: {entry.BPNumber} ({numb}/{total})");
        logger.LogProcessingInfo("Getting URL For entry");
        logger.Log($"Getting URL for entry {entry.BPNumber}");
        Console.WriteLine($"Getting URL for entry {entry.BPNumber}");
        var url = GetURLForEntry(entry);

        var table = TableEntriesFromPN(url);
        if (table.Count > 0)
        {
            Console.WriteLine($"There were {table.Count} results for entry {entry.BPNumber} (# {numb}/{total})");
            logger.LogProcessingInfo($"There were {table.Count} results for entry {entry.BPNumber}");
            var choice = UserSelectFromTable(entry, table);
            HandleChoice(choice, entry, table, idpDataPath);
            
        }
        else
        {
            Console.WriteLine($"There were no results for entry {entry.Name} ({entry.BPNumber})");
            logger.LogProcessingInfo($"There were no results for entry {entry.Name} ({entry.BPNumber})");
            NewEntry(entry);
        }

    }

    private static void HandleChoice(int choice, XMLDataEntry entry, List<string> table, string path)
    {
        logger.LogProcessingInfo("Handling choice of which entry is correct.");
        if (choice == 0)
        {
            logger.LogProcessingInfo("The choice was that this is a new entry. Starting New Entry code.");
            NewEntry(entry);
        }
        else
        {
            var chosen = table[choice - 1];
            logger.LogProcessingInfo($"{chosen} was chosen to be an existing entry. Finding entry filePath.");
            var chosenNumber = chosen.Split(".")[0];
            var filePath = GetFilePath(chosenNumber, path);
            logger.LogProcessingInfo($"Found file path ({filePath}), now loading file");
            
            var file = LoadFile(filePath);
            logger.LogProcessingInfo("File loaded, now adding segs and saving file.");
            
            AddSegsSaveFile(entry, file, filePath);
            
            Console.WriteLine($"Segs added, now deleting {entry.PNFileName} as the proper file has been updated");
            logger.LogProcessingInfo($"Segs added, now deleting {entry.PNFileName} as the proper file has been updated");
            File.Delete(entry.PNFileName);
        }
    }

    private static void AddSegsSaveFile(XMLDataEntry entry, XmlDocument file, string path)
    { 
        logger.LogProcessingInfo("Adding segs to file.");
        
        var nsManager = new XmlNamespaceManager(file.NameTable);
        nsManager.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");
        
        logger.LogProcessingInfo("Finding segs in document");
        var root = file.DocumentElement;
        var name = root.SelectSingleNode("//tei:seg[@subtype='nom'][@resp='#BP']", nsManager);
        var idno = root.SelectSingleNode("//tei:idno[@type='bp']", nsManager);
        var index = root.SelectSingleNode("//tei:seg[@subtype='index'][@resp='#BP']", nsManager);
        var indexBis = root.SelectSingleNode("//tei:seg[@subtype='indexBis']", nsManager);
        var title = root.SelectSingleNode("//tei:seg[@subtype='titre']", nsManager);
        var publisher = root.SelectSingleNode("//tei:seg[@subtype='publication']", nsManager);
        var resume = root.SelectSingleNode("//tei:seg[@subtype='resume']", nsManager);
        var sbandSeg = root.SelectSingleNode("//tei:seg[@subtype='sbSeg']", nsManager);
        var cr = root.SelectSingleNode("//tei:seg[@subtype='cr']", nsManager);
        var internet = root.SelectSingleNode("//tei:seg[@subtype='internet']", nsManager);
        var note = root.SelectSingleNode("//tei:note[@resp='#BP']", nsManager);
        
        
        logger.LogProcessingInfo("Now checking what segs the entry has, and comparing them against the file, so that any segs which are missing can be added.");
        if(entry.HasName) AddItem(root,file, name,"Name", entry.Name, path);
        if(entry.HasBPNum) AddIdno(root, file, idno,  entry.BPNumber, path);
        if(entry.HasIndex) AddItem(root, file, index, "index", entry.Index, path);
        if(entry.HasIndexBis) AddItem(root, file, indexBis, "indexBis" ,entry.IndexBis, path);
        if(entry.HasTitle) AddItem(root, file, title, "titre" ,entry.Title, path);
        if(entry.HasPublication) AddItem(root, file, publisher, "publication" , entry.Publication, path);
        if(entry.HasSBandSEG) AddItem(root, file,sbandSeg, "sbSeg", entry.SBandSEG, path);
        if(entry.HasCR) AddItem(root, file, cr, "cr", entry.CR, path);
        if(entry.HasInternet) AddItem(root, file, internet, "internet", entry.Internet, path);
        
        if (entry.HasResume)
        {
            AddNote(root, file, note, entry.Resume, path);
            AddItem(root, file, resume, "resume", entry.Resume, path);
        }
        
        logger.LogProcessingInfo("Saving file");
        file.Save(path);
    }
    
    static void AddIdno(XmlElement root, XmlDocument file, XmlNode? childNode, string fieldValue, string fileName)
    {
        if (childNode == null)
        {
            logger.LogProcessingInfo($"Trying to append {fieldValue} to {fieldValue} in {Path.GetFileName(fileName)}.");
            var newIdno = file.CreateElement("idno", "http://www.tei-c.org/ns/1.0");

            // Set type attribute
            var type = file.CreateAttribute("type");
            type.Value = "bp";
            newIdno.Attributes.Append(type);

            // Set name text
            newIdno.InnerText = fieldValue;
            
            root.AppendChild(newIdno);
            
            Console.WriteLine($"Appended {fieldValue} to {fieldValue} in {Path.GetFileName(fileName)}.");
            logger.LogProcessingInfo($"Appended {fieldValue} to {fieldValue} in {Path.GetFileName(fileName)}.");}
        else
        {
            logger.LogProcessingInfo($"Could not append {fieldValue} to {fileName}.");
        }
    }
    
    static void AddNote(XmlElement root, XmlDocument file, XmlNode? childNode, string fieldValue, string fileName)
    {
        if (childNode == null)
        {
            logger.LogProcessingInfo($"Trying to append {fieldValue} to {fieldValue} in {Path.GetFileName(fileName)}.");
            var newIdno = file.CreateElement("note", "http://www.tei-c.org/ns/1.0");

            // Set type attribute
            var type = file.CreateAttribute("type");
            type.Value = "resume";
            newIdno.Attributes.Append(type);

            // Set name text
            newIdno.InnerText = fieldValue;
            
            root.AppendChild(newIdno);
            
            Console.WriteLine($"Appended {fieldValue} to {fieldValue} in {Path.GetFileName(fileName)}.");
            logger.LogProcessingInfo($"Appended {fieldValue} to {fieldValue} in {Path.GetFileName(fileName)}.");}
        else
        {
            logger.LogProcessingInfo($"Could not append {fieldValue} to {fileName}.");
        }
    }
    
    static void AddItem(XmlElement root, XmlDocument file, XmlNode? childNode,  string fieldName, string fieldValue, string fileName)
    {
        if (childNode == null)
        {
            logger.LogProcessingInfo($"Trying to append {fieldValue} to {fieldValue} in {Path.GetFileName(fileName)}.");
            var newNameElement = file.CreateElement("seg", "http://www.tei-c.org/ns/1.0");

            // Set subtype attribute
            var subtypeAttr = file.CreateAttribute("subtype");
            subtypeAttr.Value = fieldName;
            newNameElement.Attributes.Append(subtypeAttr);

            // Set resp attribute
            var respAttr = file.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set type attribute
            var type = file.CreateAttribute("type");
            type.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = fieldValue;
            
            root.AppendChild(newNameElement);
            
            Console.WriteLine($"Appended {fieldValue} to {fieldValue} in {Path.GetFileName(fileName)}.");
            logger.LogProcessingInfo($"Appended {fieldValue} to {fieldValue} in {Path.GetFileName(fileName)}.");}
        else
        {
            logger.LogProcessingInfo($"Could not append {fieldValue} to {fileName}.");
        }
    }

    private static XmlDocument LoadFile(string filePath)
    {
        logger.LogProcessingInfo($"Trying to load {filePath}");
        var file = new XmlDocument();
        file.Load(filePath);
        Console.WriteLine($"File {filePath} loaded.");
        logger.LogProcessingInfo($"File {filePath} loaded.");

        return file;
    }

    private static string GetFilePath(string chosen, string path)
    {
        logger.LogProcessingInfo($"Extracting number from {chosen} to generate file path.");
        var len = chosen.Length;
        string numb = "";
        //If it 1,10,111-999, or 1000 its in category 1, else its in one of the other categories
        if (len < 4 || chosen == "1000")
        {
            numb = "1";
        } 
        
        //If it is exactly 4
        if (chosen.Length == 4)
        {
            numb = chosen[0].ToString();
        }
        
        //If its 5, then we know its 11-99 
        if (chosen.Length == 5)
        {
            numb= $"{chosen[0]}{chosen[1]}";
        }
        
        var finalPath =  GetPathFromNumber(numb, chosen, path);
        logger.LogProcessingInfo($"Retrived number {numb} from {chosen} to construct path is at: {finalPath}.");
       
        return finalPath;
        throw new ArgumentOutOfRangeException("Error Invalid Number!");
    }

    private static string GetPathFromNumber(string numbString, string chosen, string path)
    {
        if (Int32.TryParse(numbString, out var numb))
        {
            numb++;
            return Path.Combine(path, $"{numb}/{chosen}.xml");
        }

        throw new ArgumentOutOfRangeException("Error I need a number!");
    }

    private static void NewEntry(XMLDataEntry entry)
    {
        logger.Log($"Could not find an entry in PN for BP# {entry.BPNumber}.\n" +
            $"Not deleting {entry.PNFileName}");
        logger.LogProcessingInfo($"Could not find an entry in PN for BP# {entry.BPNumber}.\n" +
                   $"Not deleting {entry.PNFileName}");
        Console.WriteLine($"Could not find an entry in PN for BP# {entry.BPNumber}.\n" +
                   $"Not deleting {entry.PNFileName}");
        notFoundFiles.Add(entry.PNFileName);
    }

    private static int  UserSelectFromTable(XMLDataEntry entry, List<string> table)
    {
        Console.WriteLine($"════════════════════════════════");
        Console.Write("Entry to compare again:");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{entry.Title} ({entry.BPNumber}) \n");
        Console.ResetColor();
        int index = 1;
        Console.WriteLine("0) None");
        Console.WriteLine("----------------");
        foreach (var td in table)
        {
            Console.WriteLine($"{index}) {td}");
            Console.WriteLine("----------------");
            index++;
        }

		return GetChoice(table.Count);
    }


	static int GetChoice(int max)
    {
        var choice = "";
        var numb = 0;
        bool haschosen = false;
        bool runAgain = true;
        do
        {
            if (haschosen)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Error {choice} is not a valid selection. Please pick a number between 0-{max}");
                Console.ResetColor();
            }
            Console.Write(">: ");
            choice = Console.ReadLine();
            haschosen = true;
            var isNumb = Int32.TryParse(choice, out numb);
            runAgain = numb < 0 || numb > max || !isNumb;
            
        } while (runAgain);

        logger.LogProcessingInfo($"The user chose {numb}");
        
        return numb;
    }

    static List<string> TableEntriesFromPN(string url)
    {
        logger.LogProcessingInfo("Trying to get html from PN site");
        var doc = new HtmlWeb();
        var html = doc.Load(url);
        logger.LogProcessingInfo("Gathered HTML, now extracting data from html.");

        return GetTableData(html);
    }

    private static List<string> GetTableData(HtmlDocument document)
    {    
        var results = new List<string>();

        // Select all table rows with class "result-record"
        var rows = document.DocumentNode.SelectNodes("//tr[@class='result-record']");

        if (rows != null)
        {
            logger.LogProcessingInfo("Was able to extract a result record table from the page, now processing");
            foreach (var row in rows)
            {
                // Extract the text content of the cell
                var cell = row.SelectSingleNode(".//td");
                if (cell != null)
                {
                    var text = HtmlEntity.DeEntitize(cell.InnerText)
                        .Replace("\n","")
                        .Replace("\t"," ")
                        .Trim();
                    logger.LogProcessingInfo($"Table results included: {text}");
                    results.Add(text ?? "");
                }
            }
        }

        logger.LogProcessingInfo($"Finished gathering table data. Total results: {results.Count}");
        
        return results;
    }

    static string GetURLForEntry(XMLDataEntry entry)
    {
        logger.LogProcessingInfo("Getting URL for the entry.");
        
        var year = "1932";
        var name = "";
        if (entry.HasBPNum) year = entry.BPNumber.Split("-")[0];
        if (entry.HasName) name = entry.Name;
        else name = GetName(entry);
        var requestURL = $"https://papyri.info/bibliosearch?q=date%3A+{year}+{name}";

        logger.LogProcessingInfo($"Gathered URL: {requestURL}");
        
        return requestURL;
    }


    static string GetName(XMLDataEntry entry)
    {
        if (entry.HasTitle)
        {
            var parts = entry.Title.Split(",");
            logger.LogProcessingInfo($"Gathered authors name: {parts[0]}");
            
            return parts[0];
        }
        else throw new ArgumentOutOfRangeException("Error! The entry did not have a name element!");
    }
}

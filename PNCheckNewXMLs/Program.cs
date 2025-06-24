// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices.JavaScript;
using DefaultNamespace;
using HtmlAgilityPack;
using PNCheckNewXMLs;

class PNCheckerNewXmls
{
    private static Logger logger { get; set; }
    
    public static void Main()
    {
        logger = new Logger();
        try
        {
            logger.Log("Starting program.\nTrying to find NewXMl Directory");
            Console.WriteLine("Starting program.\nTrying to find NewXMl Directory");
            var dirFinder = new XMLDirectoryFinder(logger);

            var startingDir = Directory.GetCurrentDirectory();
            var newXml = dirFinder.FindXmlDirectory(startingDir);

            logger.Log($"Found NewXMl directory {newXml}, now starting XmlEntryGatherer to gather xml entries");
            Console.WriteLine($"Found NewXMl directory {newXml}, now starting XmlEntryGatherer to gather xml entries");
            var XmlEntryEGatherer = new XMLEntryGatherer(newXml, logger);

            var entries = XmlEntryEGatherer.GetEntriesFromFolder(newXml);
            logger.Log($"{entries.Count} New entries have been gathered, beginning to parse entries.");
            Console.WriteLine($"{entries.Count} New entries have been gathered, beginning to parse entries.");

            foreach (var entry in entries)
            {
                Console.WriteLine($"Processing entry {entry.BPNumber}");
                logger.Log($"Processing entry {entry.BPNumber}");
                HandleNewEntry(entry);
            }
        }
        catch (Exception e)
        {
            logger.LogError("There was an error in processor", e);
        }
    }

    static void HandleNewEntry(XMLDataEntry entry)
    {
        logger.LogProcessingInfo($"Processing new entry: {entry.BPNumber}");
        logger.LogProcessingInfo("Getting URL For entry");
        logger.Log($"Getting URL for entry {entry.BPNumber}");
        var url = GetURLForEntry(entry);

        var table = TableEntriesFromPN(url);
        if (table.Count > 0)
        {
            Console.WriteLine($"There were {table.Count} results for entry {entry.BPNumber}");
            logger.LogProcessingInfo($"There were {table.Count} results for entry {entry.BPNumber}");
            UserSelectFromTable(entry, table);
        }
        else
        {
            Console.WriteLine($"There were no results for entry {entry.BPNumber}");
            logger.LogProcessingInfo($"There were no results for entry {entry.BPNumber}");
        }
    }

    private static void UserSelectFromTable(XMLDataEntry entry, List<string> table)
    {
        Console.Write("Entry to compare again:");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{entry.Title} \n");
        Console.ResetColor();
        int index = 1;
        Console.WriteLine("0) None");
        foreach (var td in table)
        {
            Console.WriteLine($"{index}) {td}");
            index++;
        }

		var choice = GetChoice();
    }


	static string GetChoice(){

	}

    static List<string> TableEntriesFromPN(string url)
    {
        var doc = new HtmlWeb();
        var html = doc.Load(url);

        return GetTableData(html);
    }

    private static List<string> GetTableData(HtmlDocument document)
    {    
        var results = new List<string>();

        // Select all table rows with class "result-record"
        var rows = document.DocumentNode.SelectNodes("//tr[@class='result-record']");

        if (rows != null)
        {
            foreach (var row in rows)
            {
                // Extract the text content of the cell
                var cell = row.SelectSingleNode(".//td");
                if (cell != null)
                {
                    var text = HtmlEntity.DeEntitize(cell.InnerText.Trim());
                    results.Add(text);
                }
            }
        }

        return results;
    }

    static string GetURLForEntry(XMLDataEntry entry)
    {
        var year = "1932";
        var name = "";
        if (entry.HasBPNum) year = entry.BPNumber.Split("-")[0];
        if (entry.HasName) name = entry.Name;
        else name = GetName(entry);
        var requestURL = $"https://papyri.info/bibliosearch?q=date%3A+{year}+author%3A+{name}";

        return requestURL;
    }


    static string GetName(XMLDataEntry entry)
    {

        if (entry.HasTitle)
        {
            var parts = entry.Title.Split(",");
            return parts[0];
        }
        else throw new ArgumentOutOfRangeException("Error! The entry did not have a name element!");
    }
}

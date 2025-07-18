using System.Reflection.Emit;
using DefaultNamespace;

namespace PNCheckNewXMLs;

public class XMLDirectoryFinder
{
    private Logger _logger { get; }

    public XMLDirectoryFinder(Logger logger)
    {
        _logger = logger;
    }

    public string FindXmlDirectory(string startingDir)
    {
        _logger.LogProcessingInfo($"Finding IDP.Data Directory, starting in: {startingDir}");
        var idpData = FindIDPDataDirectory(startingDir);
        _logger.LogProcessingInfo($"Found IDPData @: {idpData}, now looking for newXMlDirectory");
        var xmlDir = FindNewXmlDirectory(idpData);
        
        _logger.LogProcessingInfo($"Found XMl Dir @: {xmlDir}");
        return xmlDir;
    }
    
    public string FindBiblioDirectory(string startingDir)
    {
        Console.WriteLine($"Trying to find IDP.Data Directory. Starting at: {startingDir}");
        var idpData = FindIDPDataDirectory(startingDir);
        
        var DirsInIDP = Directory.GetDirectories(idpData);
        if (DirsInIDP.Any(x => x.ToLower().Contains("biblio")))
        {
            var biblio = DirsInIDP.First(x => x.ToLower().Contains("biblio"));
            return biblio;
        }
        
        throw new DirectoryNotFoundException("Could not find BPToPNOutput or NewXmlEntries directories.");
    }
    
    
    private string FindNewXmlDirectory(string idp_DataDir, string searchingForName = "bptopnoutput")
    {
        var DirsInIDP = Directory.GetDirectories(idp_DataDir);
        if (DirsInIDP.Any(x => x.ToLower().Contains("bptopnoutput")))
        {
            var bptopnDir = DirsInIDP.First(x => x.ToLower().Contains("bptopnoutput"));
            var dirsInBPToPN = Directory.GetDirectories(bptopnDir);
            if(dirsInBPToPN.Any(x => x.Contains("NewXmlEntries")))
            {
                return dirsInBPToPN.First(x => x.Contains("NewXmlEntries"));
            }
        }
        
        throw new DirectoryNotFoundException("Could not find BPToPNOutput or NewXmlEntries directories.");
    }
    
    public string FindIDPDataDirectory(string startingDirectory, string searchTerm = "idp.data")
    {
        var dirs = Directory.GetDirectories(startingDirectory);
        if (dirs.Any(x => x.Contains(searchTerm)))
        {
            return dirs.First(x => x.Contains(searchTerm));
        }
        else
        {
            var fullName = Directory.GetParent(startingDirectory)?.FullName;
            if (fullName != null)
                return FindIDPDataDirectory(fullName, searchTerm);
            else throw new DirectoryNotFoundException($"Could not find IDP.Data starting from: {startingDirectory}");
        }
    }
}
# PNCheckNewXMLs
A .NET 9 console application to merge and validate new 'fiches' created by the [BPtoPNDataCompiler](https://github.com/halosm1th/BPtoPNDataCompiler), originating in the online [Bibliographie papyrologique](https://bibpap.be/) but not yet found in the bibliography database of [papyri.info](https://papyri.info)  

It will:
1. **Discover**  
   – Your **idp.data/biblio** directory of existing XML files, organized into numeric subfolders (e.g. `…/biblio/2/1234.xml`, `…/biblio/3/2345.xml`, etc.)  
   – A **NewXML** directory of freshly exported XML files you want to integrate.  

2. **Gather & parse**  
   – Reads each `*.xml` in your NewXML folder and extracts the `<seg>` and `<idno>` fields (BP number, author name, title, publication, résumé, etc.).  

3. **Lookup & compare**  
   – Builds a search URL for each entry on papyri.info and scrapes the results table using HtmlAgilityPack.  
   – Prompts you to choose the matching existing entry—or “0” to mark it as new.  

4. **Merge or record**  
   – **If matched**: Loads the existing file, adds any missing `<seg subtype="…">` or `<note resp="#BP">` elements, saves it, and deletes the NewXML file.  
   – **If new**: Logs its filename for later manual review.  

5. **Report**  
   – At the end, writes a timestamped `XmlFrom YYYY-MM-DD(HH‑MM).txt` in your working directory listing every NewXML file that had no match.  

---

## 🗂️ Folder Layout

```text
project-root/
├── PNCheckNewXMLs/               ← this C# console project
│   ├── PNCheckNewXMLs.csproj
│   ├── Program.cs                ← entry point & orchestration  :contentReference[oaicite:0]{index=0}
│   ├── XMLDirectoryFinder.cs     ← finds your “biblio” & “NewXML” dirs  
│   ├── XMLEntryGatherer.cs       ← reads and parses NewXML files  
│   ├── XMLDataEntry.cs           ← model for parsed TEI fields  
│   ├── Logger.cs                 ← basic file‑and‑console logging  
│   └── … (other helpers)  
│
├── NewXML/                       ← put your freshly exported XMLs here  
│
└── idp.data/biblio/                       ← this is the idp.data/biblio archive  
````

> **Note:** The directory names `NewXMLEntries/` and `Biblio/` are case‑insensitive but must exist somewhere under your working directory.  The tool will search upward from where you launch it, locate each folder by name, then pair files by their base BP‑number.

---

## ⚙️ Prerequisites & Installation

1. **.NET 9 SDK**
   Install from [https://aka.ms/dotnet-download](https://aka.ms/dotnet-download).

2. **Clone & restore**

   ```bash
   git clone https://github.com/halosm1th/PNCheckNewXMLs.git
   cd PNCheckNewXMLs/PNCheckNewXMLs
   dotnet restore
   ```

3. **HtmlAgilityPack**
   Already referenced in the project; restore will pull it in.

---

## 🚀 Running the Tool

From the `PNCheckNewXMLs/` folder:

```bash
dotnet run
```

You’ll see console logs as it:

1. Finds your `NewXML` & `biblio` directories.

2. Gathers and parses each new entry.

3. Hits papyri.info, scrapes possible matches, and prompts you:

   ```
   ════════════════════════════════
   Entry to compare again:  Aristophanes, Lysistrata (1932‐BP1234)
   0) None
   ----------------
   1) 1932-BP1234. “Aristophanes, Lysistrata: TEI record”  
   2) 1932-BP1234. “Lysistrata: Papyrus fragments”  
   ...
   >:
   ```

4. On choosing a match (e.g. `2`), it locates `biblio/?.?/1932-BP1234.xml`, inserts any missing `<seg>` or `<note>` elements, saves, deletes the NewXML file, and moves on.

5. If you choose `0`, it logs that filename for review.

At the end, you’ll get a report file:

```
XmlFrom 2025-07-16(11-42).txt
```

…listing NewXML files that had no match.
---

## 🛠️ Troubleshooting

* **“Directory not found”**
  Ensure `NewXML` and `biblio` folders exist under where you run `dotnet run`.
* **HTTP errors**
  Check your internet connection; papyri.info must be reachable.
---
Generated with the help of Chatgpt

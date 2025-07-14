// Create Components.wxs File
using Wix.Setup.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Windows.Input;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

var irisxBuildPath = @"D:\Github\Analysis-Express-Inc\IrisX_2025\src\x64\Release\";
var ignoreFileNames = new List<string> { "PdfiumViewer.resources.dll", "gitattributes", "Yolo_4", "Yolo_11" };

UpdateBatfileForYolo(7);
UpdateBatfileForYolo(11);
UpdateInstaller();
void UpdateBatfileForYolo(int balloonversion)
{
	var commands = new List<string>();
	var batfilePath = $"D:\\Github\\Analysis-Express-Inc\\IrisX_2025\\src\\Python\\BalloonServer{balloonversion}\\build.bat";
	var serverPath = $"D:\\Github\\Analysis-Express-Inc\\IrisX_2025\\src\\Python\\BalloonServer{balloonversion}";
	commands = GetBatchFilesDataV2(serverPath, balloonversion);
	File.WriteAllLines(batfilePath, commands);
}

List<string> GetBatchFilesData(string serverPath,int balloonversion)
{
	List<string> strings =
	[
		"@echo off",
		$"cd /d {serverPath}",
		$"call {serverPath}\\env\\Scripts\\activate.bat",
		"pip install -r requirements.txt",
		$"pyinstaller --clean {GetAllModelNames(serverPath + "\\requirements.txt")} irisx-server.py -y  --paths=utils --add-data=\"./utils/Yolo_{balloonversion}_*.py;utils\"  --add-data=\"./models/yolo.py;models\"",
		$"set \"source={serverPath}\\dist\\irisx-server\"",
		$"set \"zipfile={serverPath}\\dist\\irisx-server-{balloonversion}.zip\"",
		$"powershell -Command \"Compress-Archive -Path '%source%\\*' -DestinationPath '%zipfile%' -Force\"",
		$"echo Folder zipped successfully!at '%zipfile%'",
		$"start \"\" \"{serverPath}\\dist\"",
		$"pause".Trim()
	];
	return strings;
}
List<string> GetBatchFilesDataV2(string serverPath, int balloonversion)
{
	List<string> strings =
	[
		"@echo off",
		$"cd /d %~dp0",
		$"call .\\env\\Scripts\\activate.bat",
		"pip install -r requirements.txt",
		$"pyinstaller --clean {GetAllModelNames(serverPath + "\\requirements.txt")} irisx-server.py -y  --paths=utils --add-data=\"./utils/Yolo_{balloonversion}_*.py;utils\"  --add-data=\"./models/yolo.py;models\"",
		$"set \"source=%~dp0dist\\irisx-server\"",
		$"set \"zipfile=%~dp0dist\\irisx-server-{balloonversion}.zip\"",
		$"powershell -Command \"Compress-Archive -Path '%source%\\*' -DestinationPath '%zipfile%' -Force\"",
		$"echo Folder zipped successfully!at '%zipfile%'",
		$"pause".Trim()
	];
	return strings;
}
string GetAllModelNames(string reqPath)
{
	var packageswithversion = File.ReadAllLines(reqPath);

	var packages = new List<string>();
	foreach (var item in packageswithversion)
	{
		if(item.Contains("=="))
		{
			packages.Add(item.Split(new string[] { "==" }, StringSplitOptions.None)[0]);
		}
		else
		{
			packages.Add(item);
		}
	}
	packages.Distinct();
	var hiddenImports = packages
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.Select(line =>
			{
				var package = line.Split(new[] { "==", ">=", "<=", ">", "<" }, StringSplitOptions.None)[0].Trim();
				return $"--hidden-import={package} ";
			});

	string result = string.Join(" ", hiddenImports);
	return result;
}
void UpdateInstaller()
{
	
	var defaultFileSource = "$(var.SolutionDir)x64\\$(var.IrisX.Configuration)";

	var folderTree = BuildFolderStructure(irisxBuildPath);



	var ignoreFolderNames = new List<string> { };

	PrintFolderStructure(folderTree, 0);


	var productComponents = new List<string>();
	productComponents = Directory.GetFiles(irisxBuildPath, "*.*", SearchOption.AllDirectories).OrderBy(x => x).ToList();


	FolderStructure folderStructureUpdated = new FolderStructure();
	List<string> components = new List<string>();
	Dictionary<string, List<string>> FolderAndFiles = new Dictionary<string, List<string>>();
	foreach (var item in productComponents)
	{
		if (Path.GetDirectoryName(item) == Path.GetDirectoryName(irisxBuildPath))
		{
			if (!FolderAndFiles.ContainsKey("Main"))
			{
				FolderAndFiles.Add("Main", new List<string>());
			}
			FolderAndFiles["Main"].Add(item);
		}
		else
		{
			var key = (Path.GetDirectoryName(item).Replace(irisxBuildPath, ""));
			if (!FolderAndFiles.ContainsKey(key))
			{
				FolderAndFiles.Add(key, new List<string>());
			}
			FolderAndFiles[key].Add(item);
		}

		components.Add(FileNameWithReplacechar(Path.GetFileName(item), true));
	}

	//var componentRef = new List<string>();
	//foreach (var item in components)
	//{
	//	componentRef.Add($"<ComponentRef Id=\"{item}\"");
	//}

	components = components.Distinct().ToList();
	var filter = components.Where(x => !ignoreFileNames.Any(y => x.Contains(y))).ToList();

	var xmlDocument = GenerateXmlComponentRef(components);
	string outputXmlPath = Path.Combine(irisxBuildPath, "ComponentGroup.xml");
	xmlDocument.Save(outputXmlPath);


	xmlDocument = CreateDirectoryRef(FolderAndFiles);
	outputXmlPath = Path.Combine(irisxBuildPath, "DirectoryRef.xml");
	xmlDocument.Save(outputXmlPath); 
}

//xmlDocument = GenerateXml(folderTree);
//outputXmlPath = Path.Combine(irisxBuildPath, "FolderStructure.xml");
//xmlDocument.Save(outputXmlPath);

//Console.WriteLine($"XML file saved at: {outputXmlPath}");


static string FileNameWithReplacechar(string strFileName,bool withComponent)
{
	var path = $"{Path.GetDirectoryName(strFileName)}{strFileName.Replace(" ", "_").Replace("-", "_").Replace(".", "_").Replace(Path.GetExtension(strFileName), "_")}";
	if (withComponent)
	{
		return $"{path}_Component";
	}
	else
	{
		return path;
	}
}

static XDocument GenerateXml(FolderStructure rootFolder)
{
	var xml = new XDocument(
		new XElement("RootFolder",
			CreateXmlElement(rootFolder)
		)
	);

	return xml;
}

static XDocument GenerateXmlComponentRef(List<string> rootFolder)
{
	var xml = new XDocument(
		new XElement("Fragment", new XElement("ComponentGroup",
			new XAttribute("Id", "ProductComponents"),
			CreateComponentRef(rootFolder)
		)));
	return xml;
}

static XElement CreateComponentRef(List<string> folder)
{
	var element = new XElement("ComponentRef");
	foreach (var file in folder)
	{
		element.Add(
			new XElement("ComponentRef",
			new XAttribute("Id", file)));
	}
	return element;
}

static XDocument CreateDirectoryRef(Dictionary<string, List<string>> folder)
{

	var xml = new XDocument(
	new XElement("Fragment", new XElement("ComponentGroup")));
	//foreach (var file in folder)
	//{
	//	var element = new XElement("DirectoryRef", new XAttribute("Id", "IRISXPRODUCTFOLDER"), new XAttribute("FileSource", $"$(var.SolutionDir)x64\\$(var.IrisX.Configuration){(file.Key.Equals("Main") ? "" : "\\" + file.Key + "\\")}"));
	//	foreach (string item in file.Value)
	//	{
	//		element.Add(
	//		new XElement("Component",
	//		new XAttribute("Id", FileNameWithReplacechar(Path.GetFileName(item), true)),
	//		new XAttribute("Guid", Guid.NewGuid()),
	//		new XElement("File", new XAttribute("Id", FileNameWithReplacechar(Path.GetFileName(item), false)), new XAttribute("Name", Path.GetFileName(item)), new XAttribute("KeyPath", "yes"))));
	//	}
	//	xml.Root.Add(element);
	//}

	foreach (var file in folder)
	{
		var folderid = file.Key.Contains("\\") ? file.Key.Replace("\\", "_") : file.Key;
		var foldername = file.Key.Contains("\\") ? file.Key.Split("\\").LastOrDefault() : file.Key;
		var element = new XElement("Directory", new XAttribute("Id", folderid), new XAttribute("Name", foldername));
		foreach (string item in file.Value)
		{
			element.Add(
			new XElement("Component",
			new XAttribute("Id", FileNameWithReplacechar(Path.GetFileName(item), true)),
			new XAttribute("Guid", Guid.NewGuid()),
			new XElement("File", new XAttribute("Id", FileNameWithReplacechar(Path.GetFileName(item), false)), new XAttribute("Name", Path.GetFileName(item)), new XAttribute("KeyPath", "yes"))));
		}
		xml.Root.Add(element);
	}
	return xml;
}


static XElement CreateXmlElement(FolderStructure folder)
{
	var element = new XElement("Folder",
				new XAttribute("FolderName", folder.FolderName),
						new XAttribute("FolderGuid", folder.FolderGuid)
							);

	foreach (var file in folder.Files)
	{
		element.Add(new XElement("File",
						new XAttribute("FileName", file.Key),
									new XAttribute("FileGuid", file.Value)
											));
	}

	foreach (var child in folder.ChildFolders)
	{
		element.Add(CreateXmlElement(child));
	}

	return element;
}

static FolderStructure BuildFolderStructure(string path)
{
	var folder = new FolderStructure
	{
		FolderName = Path.GetFileName(path),
		ChildFolders = Directory.GetDirectories(path)
							   .Select(BuildFolderStructure)
							   .ToList(),
		Files = Directory.GetFiles(path)
						 .Select(file => new KeyValuePair<string, Guid>(Path.GetFileName(file), Guid.NewGuid()))
						 .ToList()
	};

	return folder;
}

static void PrintFolderStructure(FolderStructure folder, int indent)
{
	Console.WriteLine($"{new string(' ', indent * 2)}📁 {folder.FolderName} (GUID: {folder.FolderGuid})");

	foreach (var file in folder.Files)
	{
		Console.WriteLine($"{new string(' ', (indent + 1) * 2)}📄 {file.Key} (GUID: {file.Value})");
	}

	foreach (var child in folder.ChildFolders)
	{
		PrintFolderStructure(child, indent + 1);
	}
}

//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Xml.Linq;

//public class FolderStructure
//{
//	public Guid FolderGuid { get; set; } = Guid.NewGuid();
//	public string FolderName { get; set; }
//	public List<FolderStructure> ChildFolders { get; set; } = new List<FolderStructure>();
//	public List<KeyValuePair<string, Guid>> Files { get; set; } = new List<KeyValuePair<string, Guid>>();
//}

//class Program
//{
//	static void Main()
//	{
//		var irisxBuildPath = @"D:\Github\Analysis-Express-Inc\IrisX\src\x64\Release\";
//		var folderTree = BuildFolderStructure(irisxBuildPath);

//		var wixXml = GenerateWixXml(folderTree);
//		string outputXmlPath = Path.Combine(irisxBuildPath, "WixStructure.wsx");
//		wixXml.Save(outputXmlPath);

//		Console.WriteLine($"WiX structure XML saved at: {outputXmlPath}");
//	}

//	static FolderStructure BuildFolderStructure(string path)
//	{
//		return new FolderStructure
//		{
//			FolderName = Path.GetFileName(path),
//			ChildFolders = Directory.GetDirectories(path)
//								   .Select(BuildFolderStructure)
//								   .ToList(),
//			Files = Directory.GetFiles(path)
//							 .Select(file => new KeyValuePair<string, Guid>(Path.GetFileName(file), Guid.NewGuid()))
//							 .ToList()
//		};
//	}

//	static XDocument GenerateWixXml(FolderStructure rootFolder)
//	{
//		var wixXml = new XDocument(
//				// Create Fragment
//				new XElement("Fragment",
//					new XElement("ComponentGroup",
//						new XAttribute("Id", "ProductComponents"),
//						GenerateComponentRefs(rootFolder)
//					)
//				),

//				// Generate Components
//				new XElement("Fragment",
//					GenerateComponents(rootFolder)
//				)
//		);

//		return wixXml;
//	}

//	static IEnumerable<XElement> GenerateComponentRefs(FolderStructure folder)
//	{
//		var fileRefs = folder.Files.Select(file =>
//			new XElement("ComponentRef",
//				new XAttribute("Id", $"{file.Key.Replace(".", "_")}_Component")
//			)
//		);

//		var childFolderRefs = folder.ChildFolders.SelectMany(GenerateComponentRefs);

//		return fileRefs.Concat(childFolderRefs);
//	}

//	static IEnumerable<XElement> GenerateComponents(FolderStructure folder)
//	{
//		var fileComponents = folder.Files.Select(file =>
//			new XElement("Component",
//				new XAttribute("Id", $"{file.Key.Replace(".", "_")}_Component"),
//				new XAttribute("Guid", file.Value),
//				new XElement("File",
//					new XAttribute("Id", file.Key.Replace(".", "_")),
//					new XAttribute("Name", file.Key),
//					new XAttribute("Source", $"$(var.SourceDir)\\{folder.FolderName}\\{file.Key}")
//				)
//			)
//		);

//		var childFolderComponents = folder.ChildFolders.SelectMany(GenerateComponents);

//		return fileComponents.Concat(childFolderComponents);
//	}
//}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wix.Setup.Utility
{
	public class FolderStructure
	{
		public string Id { get; set; }
		public Guid FolderGuid { get; set; } = Guid.NewGuid();
		public string FolderName { get; set; }

		public List<FolderStructure> ChildFolders { get; set; } = new List<FolderStructure>();
		public List<KeyValuePair<string, Guid>> Files { get; set; } = new List<KeyValuePair<string, Guid>>();
	}
}

using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Linq;
using System.Windows.Forms;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;
using Mono.Cecil.Cil;


namespace CosturaPlugin
{
	[ExportContextMenuEntryAttribute(Header = "_Load Embedded References", Category = "CosturaPlugin", Icon = "Images/Load.png")]
	public class LoadEmbeddedReferences : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.SelectedTreeNodes != null && context.SelectedTreeNodes.All(n => n is AssemblyTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length == 1;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			AssemblyTreeNode node = (AssemblyTreeNode)context.SelectedTreeNodes[0];
			AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(node.LoadedAssembly.FileName);
			ModuleDefinition module = asm.MainModule;
			string assemblyPath = Path.GetDirectoryName(node.LoadedAssembly.FileName);
			foreach (var resource in module.Resources) {
				if (! resource.Name.StartsWith("costura.") && ! resource.Name.EndsWith(".dll.compressed")) {
					continue;
				}
				string fileName = assemblyPath + "/" + resource.Name.Substring(8, resource.Name.LastIndexOf(".compressed") - 8);
				if (File.Exists(fileName)) {
					// Assembly has already been decompressed and saved in the local path, just load it.
					MainWindow.Instance.CurrentAssemblyList.OpenAssembly(fileName);
				} else {
					EmbeddedResource er = resource as EmbeddedResource;
					MemoryStream memoryStream = DecompressEmbeddedAssembly(er.GetResourceStream());
					WriteAssemblyToFile(memoryStream, fileName);
					OpenAssemblyFromStream(memoryStream, fileName);
				}
			}
		}

		private static void PrintDebugWindow(string message)
		{
			string caption = "CosturaPlugin Debug Output";
			MessageBoxButtons buttons = MessageBoxButtons.OK;
			MessageBox.Show(message, caption, buttons);
		}

		private static void OpenAssemblyFromStream(Stream stream, string fileName)
		{
			stream.Position = 0L;
			MainWindow.Instance.CurrentAssemblyList.OpenAssembly(fileName, stream);
		}

		private static MemoryStream DecompressEmbeddedAssembly(Stream embeded_resource)
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			DeflateStream source = new DeflateStream(embeded_resource, CompressionMode.Decompress);
			MemoryStream memoryStream = new MemoryStream();
			CopyTo(source, memoryStream);
			memoryStream.Position = 0L;
			return memoryStream;
		}

		private static void CopyTo(Stream source, Stream destination)
		{
			byte[] array = new byte[81920];
			int count;
			while ((count = source.Read(array, 0, array.Length)) != 0) {
				destination.Write(array, 0, count);
			}
		}

		private static void WriteAssemblyToFile(MemoryStream memoryStream, string fileName)
		{
			memoryStream.Position = 0L;
			using (FileStream output = new FileStream(fileName, FileMode.Create)) {
				memoryStream.CopyTo(output);
			}
		}

	}

	[ExportContextMenuEntryAttribute(Header = "_Remove Costura Module Initializer", Category = "CosturaPlugin", Icon = "Images/Chop.png")]
	public class DefuseCostura : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.SelectedTreeNodes != null && context.SelectedTreeNodes.All(n => n is AssemblyTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length == 1;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			AssemblyTreeNode node = (AssemblyTreeNode)context.SelectedTreeNodes[0];
			AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(node.LoadedAssembly.FileName);
			ModuleDefinition module = asm.MainModule;

			foreach (var type in module.Types) {
				if (type.FullName == "<Module>") {
					foreach (var method in type.Methods) {
						if (method.Name == ".cctor") {
							ClearAttachCallFromMethod(method);
						}
					}

				}
			}
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.FileName = node.LoadedAssembly.FileName;
			dlg.Filter = "Assembly|*.dll;*.exe";
			if (dlg.ShowDialog() == DialogResult.OK) {
				asm.MainModule.Write(dlg.FileName);
				module.Write(dlg.FileName);
			}
		}

		private static void ClearAttachCallFromMethod(MethodDefinition methodDefinition)
		{
			var body = methodDefinition.Body;
			var processor = body.GetILProcessor();

			foreach (var instruction in body.Instructions.ToList()) {
				if (instruction.OpCode == OpCodes.Call) {
					processor.Remove(instruction);
				} else if (instruction.OpCode == OpCodes.Ret) {
					break;
				}
			}
		}

		private static void PrintDebugWindow(string message)
		{
			string caption = "CosturaPlugin Debug Output";
			MessageBoxButtons buttons = MessageBoxButtons.OK;
			MessageBox.Show(message, caption, buttons);
		}
	}
}

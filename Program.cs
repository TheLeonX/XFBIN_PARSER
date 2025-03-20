using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using XFBIN_LIB;
using System.Text.Json;
using XFBIN_LIB;
using XFBIN_LIB.XFBIN;
using System.Collections.ObjectModel;
using XFBIN_LIB.Converter;
using static System.Net.Mime.MediaTypeNames;
using MiscUtil.IO;
using MiscUtil.Conversion;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace ConsoleApplication3
{
    public class Program
    {
        public static XFBIN_READER S_XFBIN_READER = new XFBIN_READER();
        public static void Main(string[] args)
        {
            try
            {
                //to get rid from this, you need to delete folders from registry HKEY_CLASSES_ROOT\.xfbin, HKEY_CLASSES_ROOT\XFBIN and HKEY_CLASSES_ROOT\Folder\shell\XFBIN_PARSER
                RegistryKey key;
                key = Registry.ClassesRoot.CreateSubKey(@"Folder\shell\XFBIN_PARSER");
                key = Registry.ClassesRoot.CreateSubKey(@"Folder\shell\XFBIN_PARSER\command");
                key.SetValue("", System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe") + " %1");
                FileAssociationHelper.AssociateFileExtension(".xfbin", "XFBIN", "XFBIN", System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe"));

                string path = "";
                if (args.Length > 0)
                {
                    args[0] = String.Join(" ", args);
                    path = Path.GetFullPath(args[0]);
                } else
                {
                    Console.Write("Write path of file/directory: ");
                    path = Console.ReadLine();
                    path = path.Replace("\"", "");
                    path = path.Replace("\\", "\\\\");

                }
                FileAttributes attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    XFBIN_WRITER.RepackXFBIN(path);
                else
                    UnpackXFBIN(path);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

        public static void UnpackXFBIN(string path)
        {
            S_XFBIN_READER.ReadXFBIN(path);
            var xfbin = S_XFBIN_READER.XfbinFile;
            string dirPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
            Directory.CreateDirectory(dirPath);

            foreach (PAGE page in xfbin.Pages)
            {
                string pageDir = Path.Combine(dirPath, page.PageName);
                Directory.CreateDirectory(pageDir);

                // Save page JSON
                string jsonPath = Path.Combine(pageDir, "_page.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new PageConverter());
                using (var fs = new FileStream(jsonPath, FileMode.Create))
                {
                    JsonSerializer.Serialize(fs, page, options);
                }

                // Save each chunk file
                foreach (CHUNK chunk in page.Chunks)
                {
                    int mapIndex = (int)chunk.ChunkMapIndex;
                    var chunkMapIndex = xfbin.ChunkTable.ChunkMapIndices[mapIndex].ChunkMapIndex;
                    var chunkMap = xfbin.ChunkTable.ChunkMaps[(int)chunkMapIndex];
                    string type = xfbin.ChunkTable.ChunkTypes[(int)chunkMap.ChunkTypeIndex].ChunkTypeName;

                    // Determine file format/extension
                    string format = ".bin";
                    if (PageConverter.file_format.TryGetValue(type, out string ext))
                        format = ext;

                    // Skip types that shouldn't be unpacked
                    if (type == "nuccChunkNull" || type == "nuccChunkPage" || type == "nuccChunkIndex")
                        continue;

                    string chunkName = xfbin.ChunkTable.ChunkNames[(int)chunkMap.ChunkNameIndex].ChunkName;
                    string filePath = Path.Combine(pageDir, chunkName + format);
                    File.WriteAllBytes(filePath, chunk.ChunkData);
                }
            }
            Console.WriteLine("Saved json");
        }



    }
    static class FileAssociationHelper
    {
        public static void AssociateFileExtension
        (string fileExtension, string name, string description, string appPath)
        {
            //Create a key with specified file extension
            RegistryKey _extensionKey = Registry.ClassesRoot.CreateSubKey(fileExtension);
            _extensionKey.SetValue("", name);

            //Create main key for the specified file format
            RegistryKey _formatNameKey = Registry.ClassesRoot.CreateSubKey(name);
            _formatNameKey.SetValue("", description);
            _formatNameKey.CreateSubKey("DefaultIcon").SetValue("", "\"" + appPath + "\",0");

            //Create the 'Open' action under 'Shell' key
            RegistryKey _shellActionsKey = _formatNameKey.CreateSubKey("Shell");
            _shellActionsKey.CreateSubKey("open").CreateSubKey("command").SetValue
                                         ("", "\"" + appPath + "\" \"%1\"");

            _extensionKey.Close();
            _formatNameKey.Close();
            _shellActionsKey.Close();

            // Update Windows Explorer windows for this new file association
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify
                (uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}

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

namespace ConsoleApplication3 {
    public class Program {
        public static XFBIN_READER S_XFBIN_READER =  new XFBIN_READER();
        public static void Main(string[] args)
        {
            try
            {
                // Delete registry folders if needed
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
                Console.WriteLine("An error occurred:");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        public static void UnpackXFBIN(string path) {
            S_XFBIN_READER.ReadXFBIN(path);
            string dir_path = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path);
            if (Directory.Exists(dir_path))
                Directory.Delete(dir_path, true);
            Directory.CreateDirectory(dir_path);
            foreach (PAGE page in S_XFBIN_READER.XfbinFile.Pages) {
                Directory.CreateDirectory(dir_path + "\\" + page.PageName);
                using (FileStream fs = new FileStream(dir_path + "\\" + page.PageName + "\\_page.json", FileMode.Create)) {
                    var options = new JsonSerializerOptions {
                        WriteIndented = true,
                    };
                    options.Converters.Add(new PageConverter());

                    JsonSerializer.Serialize(fs, page, options);
                }
                foreach (CHUNK chunk in page.Chunks) {
                    string format = ".bin";
                    string type = S_XFBIN_READER.XfbinFile.ChunkTable.ChunkTypes[(int)S_XFBIN_READER.XfbinFile.ChunkTable.ChunkMaps[(int)S_XFBIN_READER.XfbinFile.ChunkTable.ChunkMapIndices[(int)chunk.ChunkMapIndex].ChunkMapIndex].ChunkTypeIndex].ChunkTypeName;
                    if (PageConverter.file_format.ContainsKey(type))
                        format = PageConverter.file_format[type];
                    if (type != "nuccChunkNull" &&
                        type != "nuccChunkPage" &&
                        type != "nuccChunkIndex")
                        File.WriteAllBytes(dir_path + "\\" + page.PageName + "\\" + S_XFBIN_READER.XfbinFile.ChunkTable.ChunkNames[(int)S_XFBIN_READER.XfbinFile.ChunkTable.ChunkMaps[(int)S_XFBIN_READER.XfbinFile.ChunkTable.ChunkMapIndices[(int)chunk.ChunkMapIndex].ChunkMapIndex].ChunkNameIndex].ChunkName + format, chunk.ChunkData);
                }
            }
            Console.WriteLine("Saved json");


        }

        
            
    }
    static class FileAssociationHelper {
        public static void AssociateFileExtension
        (string fileExtension, string name, string description, string appPath) {
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

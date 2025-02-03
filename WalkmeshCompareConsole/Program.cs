using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using KotOR_IO;
using KotOR_IO.Helpers;
using System.Diagnostics;
using System.Security.Permissions;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Text;
using WalkmeshVisualizerWpf.Helpers;
using System.Windows;

namespace WalkmeshCompareConsole
{
    class Program
    {
        private static readonly string K1_DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\swkotor";
        private static readonly string K2_DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Knights of the Old Republic II";

        static KPaths Paths = new KPaths(K1_DEFAULT_PATH);
        static string GameName = "KotOR 1";

        /// <summary>
        /// Lookup from a RIM filename to the WOKs it contains.
        /// </summary>
        private static readonly Dictionary<string, IEnumerable<WOK>> RimWoksLookup = new Dictionary<string, IEnumerable<WOK>>();

        /// <summary>
        /// Lookup from a Room name to the room's walkmesh.
        /// </summary>
        private static readonly Dictionary<string, WOK> RoomWoksLookup = new Dictionary<string, WOK>();

        /// <summary>
        /// Lookup from RIM filename to a more readable Common Name.
        /// </summary>
        private static readonly Dictionary<string, string> RimNamesLookup = new Dictionary<string, string>();


        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);


        static void Main(string[] args)
        {
            ReadKotorMemory();
        }

        private static void ReadKotorMemory()
        {
            int version = 0;
            while (version == 0) version = GetRunningKotor();

            var km = new KotorManager(version);

            km.pr.ReadInt(km.ka.ADDRESS_BASE, out int testRead);
            if (testRead != 0x00905a4d)
                throw new Exception($"Failed Test Read!\r\nExpected: {0x00905a4d}\r\nGot: {testRead}");

            Console.WriteLine("Reading from swkotor.exe initialized...\r\n");
            Console.CursorVisible = false;

            //try
            //{

            //}
            //catch (Exception e)
            //{

            //    throw;
            //}
            while (true)
            {
                var p = km.GetLeaderPosition();
                Console.Write($"({p.X:0.0000}, {p.Y:0.0000})");
                System.Threading.Thread.Sleep(100);
                ClearCurrentConsoleLine();
            }
        }

        public static void ClearCurrentConsoleLine()
        {
            //Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, currentLineCursor);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private static int GetRunningKotor()
        {
            var process = Process.GetProcessesByName("swkotor").FirstOrDefault();
            if (process != null) return 1;
            process = Process.GetProcessesByName("swkotor2").FirstOrDefault();
            if (process != null) return 2;
            return 0;
        }

        private static void UseGameFilesToCompareWok()
        {
            while (true)
            {
                RequestGameFromUser();

                FetchGameData();

                WokCompareConsole();

                ClearGameData();
            }
        }

        private static void ClearGameData()
        {
            RimNamesLookup.Clear();
            RimWoksLookup.Clear();
            RoomWoksLookup.Clear();

            Console.WriteLine();
        }

        private static void RequestGameFromUser()
        {
            while (true)
            {
                Console.WriteLine("Which game do you wish to investigate? Options 1 and 2 assume default steam installation on the C:");
                Console.WriteLine("If your installation differs, please use option 3 or 4.");
                Console.WriteLine("    1: KotOR 1");
                Console.WriteLine("    2: KotOR 2");
                Console.WriteLine("    3: Custom game directory entry");
                Console.WriteLine("    4: Extracted walkmesh directory");
                Console.WriteLine(" exit: Close the program");
                Console.Write("Option = ");

                var line = Console.ReadLine();
                if (line.ToLower() == "exit") Environment.Exit(0);

                if (!int.TryParse(line, out var game) ||
                    game < 1 || game > 4)
                {
                    Console.WriteLine("Invalid game selection. Please try again.\n");
                    continue;
                }

                if (game == 1)
                {
                    GameName = "KotOR 1";
                    Paths = new KPaths(K1_DEFAULT_PATH);
                }
                else if (game == 2)
                {
                    GameName = "KotOR 2";
                    Paths = new KPaths(K2_DEFAULT_PATH);
                }
                else if (game == 3)
                {
                    Console.WriteLine("Enter KotOR 1 or 2 Game Directory Path: ");
                    var path = Console.ReadLine();
                    var di = new DirectoryInfo(path);

                    if (!di.Exists)
                    {
                        Console.WriteLine("Directory does not exist. Please try again.\n");
                        continue;
                    }

                    var files = di.EnumerateFiles();

                    if (files.Any(fi => fi.Name.ToLower() == "swkotor.exe"))
                    {
                        GameName = "KotOR 1";
                    }
                    else if (files.Any(fi => fi.Name.ToLower() == "swkotor2.exe"))
                    {
                        GameName = "KotOR 2";
                    }
                    else
                    {
                        Console.WriteLine("Unable to find swkotor.exe or swkotor2.exe in the given directory. Please try again.\n");
                        continue;
                    }

                    Paths = new KPaths(path);
                }
                else if (game == 4)
                {
                    WokCompareConsoleExtractedFiles();
                    Console.WriteLine("\n");
                    continue;
                }

                break;
            }
        }

        private static void FetchGameData()
        {
            // Create KEY and TLK
            Console.WriteLine($"\nExtracting walkmesh files from {GameName} ...");
            var key = new KEY(Paths.chitin);
            var tlk = new TLK(Paths.dialog);

            // Create BIF for layouts.bif
            Console.WriteLine("Opening layouts.bif ...");
            var lytBif = new BIF(Path.Combine(Paths.data, "layouts.bif"));
            lytBif.AttachKey(key, "data\\layouts.bif");
            var lytVREs = lytBif.VariableResourceTable.Where(vre => vre.ResourceType == ResourceType.LYT);
            var lytFiles = new Dictionary<string, LYT>();   // lytFiles[ResRef] = lytObj; ResRef == RimFilename

            // Create LYT objects
            Console.WriteLine("Retrieving module layouts ...");
            foreach (var vre in lytVREs)
            {
                var lyt = new LYT(vre.EntryData);
                if (lyt.Rooms.Any())    // Only save the LYT if it has rooms.
                    lytFiles.Add(vre.ResRef.ToLower(), lyt);
            }

            // Create BIF for models.bif - contains walkmeshes
            Console.WriteLine("Opening models.bif ...");
            var mdlBif = new BIF(Path.Combine(Paths.data, "models.bif"));
            mdlBif.AttachKey(key, "data\\models.bif");
            var wokVREs = mdlBif.VariableResourceTable.Where(vre => vre.ResourceType == ResourceType.WOK);

            // Create WOK objects
            Console.WriteLine("Retrieving walkmeshes ...");
            foreach (var vre in wokVREs)
            {
                var wok = new WOK(vre.EntryData);
                if (wok.Verts.Any())    // Only save the WOK if it has verts.
                    RoomWoksLookup.Add(vre.ResRef.ToLower(), wok);
            }

            // Parse RIMs for common name and LYTs in use.
            Console.WriteLine("Parsing rims ...");
            var rimFiles = Paths.FilesInModules.Where(fi => !fi.Name.EndsWith("_s.rim") && fi.Extension == ".rim");
            foreach (var fi in rimFiles)
            {
                // Create RIM object
                var rim = new RIM(fi.FullName);
                var rimName = fi.Name.Replace(".rim", "").ToLower();  // something akin to warp codes ("danm13")

                // Fetch ARE file
                var rfile = rim.File_Table.First(rf => rf.TypeID == (int)ResourceType.ARE);
                var are = new GFF(rfile.File_Data);
                var lytResRef = rfile.Label;  // label used for LYT resref and (usually) room prefix

                // Find the module (common) name
                string moduleName = string.Empty;   // something like "Dantooine - Jedi Enclave"
                if (are.Top_Level.Fields.First(f => f.Label == "Name") is GFF.CExoLocString field)
                {
                    moduleName = tlk[field.StringRef];
                }
                RimNamesLookup.Add(rimName, moduleName);

                // Check LYT file to collect this RIM's rooms.
                if (lytFiles.ContainsKey(lytResRef))
                {
                    // LYT that matches this RIM
                    var lyt = lytFiles[lytResRef];

                    var roomNames = lyt.Rooms
                        .Select(r => r.Name.ToLower())
                        .Where(r => !r.Contains("****") ||  // remove invalid
                                    !r.Contains("stunt"));  // remove cutscene
                    if (!roomNames.Any()) continue; // Only store RIM if it has rooms.

                    var rimWoks =
                        RoomWoksLookup.Where(kvp => roomNames.Contains(kvp.Key))
                                      .Select(kvp => kvp.Value);
                    if (!rimWoks.Any()) continue;   // Only store RIM if it has WOKs.

                    RimWoksLookup.Add(rimName, rimWoks);
                }
                else
                {
                    Console.WriteLine($"ERROR: no layout file corresponds to the name '{lytResRef}'.");
                }
            }

            //var pits = new List<WOK.Face>();
            //var nwgs = new List<WOK.Face>();
            //var muds = new List<WOK.Face>();
            //var drts = new List<WOK.Face>();
            //var wtrs = new List<WOK.Face>();
            //var dwtr = new List<WOK.Face>();
            //foreach (var wok in RoomWoksLookup.Values)
            //{
            //    pits.AddRange(wok.Faces.Where(f => f.SurfaceMaterial == SurfaceMaterial.BottomlessPit));
            //    nwgs.AddRange(wok.Faces.Where(f => f.SurfaceMaterial == SurfaceMaterial.NonWalkGrass));
            //    muds.AddRange(wok.Faces.Where(f => f.SurfaceMaterial == SurfaceMaterial.Mud));
            //    drts.AddRange(wok.Faces.Where(f => f.SurfaceMaterial == SurfaceMaterial.Dirt));
            //    wtrs.AddRange(wok.Faces.Where(f => f.SurfaceMaterial == SurfaceMaterial.Water));
            //    dwtr.AddRange(wok.Faces.Where(f => f.SurfaceMaterial == SurfaceMaterial.DeepWater));
            //}

            Console.WriteLine();
        }

        private static void WokCompareConsole()
        {
            while (true)
            {
                Console.WriteLine("\n------------------------------------------------------------------------------------");
                Console.WriteLine("Please enter set of coordinates to be checked (x, y). Type 'exit' to go back.");

                Console.Write("x = ");
                var line = Console.ReadLine();
                if (line.ToLower() == "exit") break;
                if (!float.TryParse(line, out var x))
                {
                    Console.WriteLine("Unable to parse the x coordinate.");
                    continue;
                }

                Console.Write("y = ");
                line = Console.ReadLine();
                if (line.ToLower() == "exit") break;
                if (!float.TryParse(line, out var y))
                {
                    Console.WriteLine("Unable to parse the y coordinate.");
                    continue;
                }

                Console.WriteLine($"\nSearching walkmeshes for walkable coordinates at [{x}, {y}] ...");
                foreach (var rimKvp in RimWoksLookup)
                {
                    if (rimKvp.Key.Contains("stunt"))
                        continue;   // Skip cutscene rims.

                    foreach (var wok in rimKvp.Value)
                    {
                        var sw = new Stopwatch();
                        sw.Start();
                        if (wok.ContainsWalkablePoint(x, y))
                        {
                            Console.WriteLine($"{rimKvp.Key,12}, {RimNamesLookup[rimKvp.Key]}, {sw.Elapsed}");
                            break;
                        }
                        sw.Stop();
                    }
                }
            }
        }

        private static void WokCompareConsoleExtractedFiles()
        {
            Console.WriteLine("Enter Walkmesh Directory Path: ");
            string path = Console.ReadLine();

            if (!Directory.Exists(path))
            {
                Console.WriteLine("Directory doesn't exist. Please try again.");
                return;
            }

            List<WOK> w_array = new List<WOK>();
            List<string> w_names = new List<string>();

            Console.WriteLine("\nInitializing Walkmeshes.\nPlease Wait ...");
            Initialize(ref w_array, ref w_names, path);

            while (true)
            {
                Console.WriteLine("\n------------------------------------------------------------------------------------");
                Console.WriteLine("Please enter set of coordinates to be checked (x, y). Type 'exit' to go back.");

                Console.Write("x = ");
                var line = Console.ReadLine();
                if (line.ToLower() == "exit") break;
                if (!float.TryParse(line, out var x))
                {
                    Console.WriteLine("Unable to parse the x coordinate.");
                    continue;
                }

                Console.Write("y = ");
                line = Console.ReadLine();
                if (line.ToLower() == "exit") break;
                if (!float.TryParse(line, out var y))
                {
                    Console.WriteLine("Unable to parse the y coordinate.");
                    continue;
                }

                Console.WriteLine($"\nChecking coordinates [{x}, {y}] ...");
                for (var i = 0; i < w_array.Count; i++)
                {
                    if (w_array[i].ContainsWalkablePoint(x, y))
                    {
                        Console.WriteLine($"{w_names[i],12}");
                    }
                    //foreach (var f in w_array[i].Faces)
                    //{
                    //    if (f.IsWalkable && f.ContainsPoint2D(x, y))
                    //    {
                    //        Console.WriteLine($"{w_names[i],12}");
                    //        break;
                    //    }
                    //}
                }
            }
        }

        public static void Initialize(ref List<WOK> wok_array, ref List<string> wok_names, string directory_path)
        {
            var di = new DirectoryInfo(directory_path);
            var files = di.GetFiles().Where(fi => fi.Extension == ".wok");

            Console.WriteLine("Parsing module walkmeshes ...");
            foreach (var room in files)
            {
                var wok = new WOK(room.OpenRead());
                wok_array.Add(wok);
                wok_names.Add(room.Name.Replace(".wok", ""));
            }

            Console.WriteLine("Done.\n");
        }
    }
}

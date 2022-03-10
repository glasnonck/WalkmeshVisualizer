using KotOR_IO;
using KotOR_IO.GffFile;
using KotOR_IO.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using WalkmeshVisualizerWpf.Helpers;

namespace WalkmeshVisualizerWpf.Models
{
    /// <summary>
    /// Responsible for building and delivering <see cref="KotorDataModel"/> objects. This encapsulates object construction from data storage.
    /// </summary>
    public static class KotorDataFactory
    {
        #region Constants
        public const string K1_DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\swkotor";
        public const string K2_DEFAULT_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Knights of the Old Republic II";
        public const string K1_EXE_NAME = "swkotor.exe";
        public const string K2_EXE_NAME = "swkotor2.exe";
        public const string K1_NAME = "KotOR 1";
        public const string K2_NAME = "KotOR 2";
        #endregion

        #region Properties
        /// <summary>
        /// Returns true if KotOR 1 game data has been cached.
        /// </summary>
        public static bool IsKotor1Cached => Directory.Exists(Path.Combine(Environment.CurrentDirectory, $"{K1_NAME} Data"));

        /// <summary>
        /// Returns true if KotOR 2 game data has been cached.
        /// </summary>
        public static bool IsKotor2Cached => Directory.Exists(Path.Combine(Environment.CurrentDirectory, $"{K2_NAME} Data"));

        /// <summary>
        /// Most recently parsed <see cref="KotorDataModel"/> of each <see cref="SupportedGame"/>.
        /// </summary>
        private static Dictionary<SupportedGame, KotorDataModel> GameToKotorData { get; set; } = new Dictionary<SupportedGame, KotorDataModel>();

        /// <summary>
        /// Lookup from the game path to the <see cref="SupportedGame"/> that it contains.
        /// </summary>
        private static Dictionary<string, SupportedGame> PathToGame { get; set; } = new Dictionary<string, SupportedGame>();
        #endregion

        #region Delegates
        /// <summary>
        /// Delegate of the ReportProgress method of a <see cref="BackgroundWorker"/>.
        /// </summary>
        /// <param name="percentProgress">The percentage, from 0 to 100, of the background operation that is complete.</param>
        public delegate void ReportProgressDelegate(int percentProgress);
        #endregion

        #region Access Methods
        /// <summary>
        /// Builds and returns a data model of KotOR 1 at the default install path.
        /// </summary>
        /// <param name="report">Delegate method used to report build progress.</param>
        /// <exception cref="DirectoryNotFoundException" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="NotSupportedException" />
        public static KotorDataModel GetKotor1Data(ReportProgressDelegate report = null)
        {
            return GetKotorDataByPath(K1_DEFAULT_PATH, report);
        }

        /// <summary>
        /// Builds and returns a data model of KotOR 2 at the default install path.
        /// </summary>
        /// <param name="report">Delegate method used to report build progress.</param>
        /// <exception cref="DirectoryNotFoundException" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="NotSupportedException" />
        public static KotorDataModel GetKotor2Data(ReportProgressDelegate report = null)
        {
            return GetKotorDataByPath(K2_DEFAULT_PATH, report);
        }

        /// <summary>
        /// Builds and returns a <see cref="KotorDataModel" /> installed at the requested path.
        /// </summary>
        /// <param name="gamePath">Path to the KotOR game directory.</param>
        /// <param name="report">Delegate method used to report build progress.</param>
        /// <exception cref="DirectoryNotFoundException" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="NotSupportedException" />
        public static KotorDataModel GetKotorDataByPath(string gamePath, ReportProgressDelegate report = null)
        {
            if (PathToGame.ContainsKey(gamePath) && GameToKotorData.ContainsKey(PathToGame[gamePath]))
                return GameToKotorData[PathToGame[gamePath]];
            //if (GameToKotorData.ContainsKey(gamePath)) return GameToKotorData[gamePath];

            var gameDir = new DirectoryInfo(gamePath);
            if (!gameDir.Exists) throw new DirectoryNotFoundException($"Unable to find directory: {gamePath}");

            // Look for kotor executable files.
            var exeFiles = gameDir.EnumerateFiles("*.exe").Select(fi => fi.Name.ToLower());
            var k1Found = exeFiles.Any(s => s == K1_EXE_NAME);
            var k2Found = exeFiles.Any(s => s == K2_EXE_NAME);

            // No KotOR executable file found.
            if (!k1Found && !k2Found)
                throw new FileNotFoundException($"Unable to find KotOR 1 or 2 executable file in directory: {gamePath}");

            // Both KotOR executable files found.
            if (k1Found && k2Found)
                throw new NotSupportedException($"Both KotOR 1 and 2 executable files found. Simultaneous game data loading is not supported.");

            XmlGameData.Initialize();
            report?.Invoke(25);

            var gameEnum = k1Found ? SupportedGame.Kotor1 : SupportedGame.Kotor2;
            var gameName = gameEnum.ToDescription();
            var kpaths = new KPaths(gamePath);
            var cachePath = Path.Combine(Environment.CurrentDirectory, $"{gameName} Data");
            var kdm = new KotorDataModel
            {
                Game = gameEnum,
                GameName = gameName,
                KPaths = kpaths,
                CachePath = cachePath,
            };

            if (Directory.Exists(cachePath))
            {
                kdm.KEY = new KEY(Path.Combine(cachePath, "chitin.key"));
                //kdm.TLK = new TLK(Path.Combine(cachePath, "dialog.tlk"));
                ReadRimFileCache(kdm, report);
            }
            else
            {
                kdm.KEY = new KEY(kpaths.chitin);
                //kdm.TLK = new TLK(kpaths.dialog);
                FetchWokFiles(kdm, report);
                FetchRimData(kdm, report);
                SaveRimFileCache(kdm, report);
            }

            // Store then return kotor data.
            GameToKotorData.Add(gameEnum, kdm);
            PathToGame.Add(gamePath, gameEnum);
            return kdm;
        }
        #endregion

        #region Cache Methods
        /// <summary>
        /// Persist RIM data for future application use.
        /// </summary>
        private static void SaveRimFileCache(KotorDataModel kdm, ReportProgressDelegate report)
        {
            var cacheDir = Directory.CreateDirectory(kdm.CachePath);
            var count = 0;
            var totalWoks = kdm.RimNameToWoks.Sum(kvp => kvp.Value.Count());

            kdm.KEY.WriteToFile(Path.Combine(kdm.CachePath, "chitin.key"));
            //kdm.TLK.WriteToFile(Path.Combine(kdm.CachePath, "dialog.tlk"));

            foreach (var rim in kdm.RimNameToWoks)
            {
                var rimDir = cacheDir.CreateSubdirectory(rim.Key);
                foreach (var wok in rim.Value)
                {
                    report?.Invoke(100 * count++ / totalWoks);
                    var wokPath = Path.Combine(rimDir.FullName, $"{wok.RoomName}.wok");
                    if (File.Exists(wokPath))
                        throw new Exception($"Save file error: File already exists at '{wokPath}'");
                    else
                        wok.WriteToFile(wokPath);
                }

                File.Create(Path.Combine(rimDir.FullName, $"{kdm.RimNameToCommonName[rim.Key]}.txt")).Close();
                kdm.RimNameToGit[rim.Key].WriteToFile(Path.Combine(rimDir.FullName, $"{rim.Key}.git"));
                kdm.RimNameToLyt[rim.Key].WriteToFile(Path.Combine(rimDir.FullName, $"{rim.Key}.lyt"));
            }
        }

        /// <summary>
        /// Deletes the cached game data of a <see cref="SupportedGame"/> if it exists.
        /// </summary>
        /// <param name="game">Game cache to delete.</param>
        private static void DeleteCachedGameData(SupportedGame game)
        {
            if (GameToKotorData.ContainsKey(game))
                Directory.Delete(GameToKotorData[game].CachePath, true);
        }

        /// <summary>
        /// Deletes the cached game data of all <see cref="SupportedGame"/>s.
        /// </summary>
        private static void DeleteAllCachedData()
        {
            DeleteCachedGameData(SupportedGame.Kotor1);
            DeleteCachedGameData(SupportedGame.Kotor2);
        }
        #endregion

        #region Data Retrieval Methods
        /// <summary>
        /// Read walkmesh files we saved previously.
        /// </summary>
        private static void ReadRimFileCache(KotorDataModel kdm, ReportProgressDelegate report)
        {
            var cacheDir = new DirectoryInfo(kdm.CachePath);
            var count = 0;
            var totalWoks = cacheDir.EnumerateFiles("*.wok", SearchOption.AllDirectories).Count();
            foreach (var rimDir in cacheDir.EnumerateDirectories())
            {
                var woks = new List<WOK>();
                foreach (var wokFile in rimDir.EnumerateFiles("*.wok"))
                {
                    report?.Invoke(100 * count++ / totalWoks);
                    woks.Add(new WOK(wokFile.OpenRead())
                    {
                        RoomName = wokFile.Name.Replace(".wok", ""),
                    });
                }
                kdm.RimNameToWoks.Add(rimDir.Name, woks);

                var nameFile = rimDir.EnumerateFiles("*.txt").First();
                kdm.RimNameToCommonName.Add(rimDir.Name, nameFile.Name.Replace(".txt", ""));

                var gitFile = rimDir.EnumerateFiles("*.git").First();
                kdm.RimNameToGit.Add(rimDir.Name, GIT.NewGIT(new GFF(gitFile.FullName)));

                var lytFile = rimDir.EnumerateFiles("*.lyt").First();
                kdm.RimNameToLyt.Add(rimDir.Name, new LYT(File.OpenRead(lytFile.FullName)));
            }
        }

        /// <summary>
        /// Retrieves all walkmesh files from game data.
        /// </summary>
        private static void FetchWokFiles(KotorDataModel kdm, ReportProgressDelegate report)
        {
            var mdlBif = new BIF(Path.Combine(kdm.KPaths.data, "models.bif"));
            mdlBif.AttachKey(kdm.KEY, "data\\models.bif");
            var wokVREs = mdlBif.VariableResourceTable.Where(vre => vre.ResourceType == ResourceType.WOK).ToList();

            for (var i = 0; i < wokVREs.Count; i++)
            {
                report?.Invoke(100 * i / wokVREs.Count);
                var wok = new WOK(wokVREs[i].EntryData)
                {
                    RoomName = wokVREs[i].ResRef.ToLower(),
                };

                // Only save the WOK if it has verts.
                if (wok.Verts.Any()) kdm.RoomNameToWok.Add(wok.RoomName, wok);
            }
        }

        /// <summary>
        /// Returns a collection of all module layout files found in the game files.
        /// </summary>
        private static Dictionary<string, LYT> FetchLayoutFiles(KotorDataModel kdm, ReportProgressDelegate report)
        {
            var lytFiles = new Dictionary<string, LYT>();
            var lytBif = new BIF(Path.Combine(kdm.KPaths.data, "layouts.bif"));
            lytBif.AttachKey(kdm.KEY, "data\\layouts.bif");
            var lytVREs = lytBif.VariableResourceTable.Where(vre => vre.ResourceType == ResourceType.LYT).ToList();

            // Create LYT objects
            for (var i = 0; i < lytVREs.Count; i++)
            {
                report?.Invoke(100 * i / lytVREs.Count);
                var lyt = new LYT(lytVREs[i].EntryData);

                // Only save the LYT if it has rooms.
                if (lyt.Rooms.Any()) lytFiles.Add(lytVREs[i].ResRef.ToLower(), lyt);
            }

            return lytFiles;
        }

        /// <summary>
        /// Retrieves game data from RIM files.
        /// </summary>
        private static void FetchRimData(KotorDataModel kdm, ReportProgressDelegate report)
        {
            var lytFiles = FetchLayoutFiles(kdm, report);

            // Find the module (common) names
            var xmlGame = kdm.GameName == K1_NAME ? XmlGameData.Kotor1Xml : XmlGameData.Kotor2Xml;
            kdm.RimNameToCommonName = xmlGame.Rims.ToDictionary(k => k.FileName, e => e.CommonName);

            // Parse RIMs for common name and LYTs in use.
            var rimFiles = kdm.KPaths.FilesInModules.Where(fi => fi.Extension == ".rim" && !fi.Name.EndsWith("_s.rim")).ToList();
            for (var i = 0; i < rimFiles.Count; i++)
            {
                report?.Invoke(100 * i / rimFiles.Count);

                // Create RIM object.
                var rim = new RIM(rimFiles[i].FullName);
                var rimName = rimFiles[i].Name.Replace(".rim", "").ToLower();

                // Store GIT file.
                kdm.RimNameToGit.Add(rimName, rim.GitFile);

                // Fetch ARE file.
                var rfile = rim.File_Table.First(rf => rf.TypeID == (int)ResourceType.ARE);
                var are = new GFF(rfile.File_Data);
                var lytResRef = rfile.Label;    // label used for LYT resref and (usually) room prefix

                // Check LYT file to collect this RIM's rooms.
                if (lytFiles.ContainsKey(lytResRef))
                {
                    // LYT that matches this RIM
                    var lyt = lytFiles[lytResRef];
                    kdm.RimNameToLyt.Add(rimName, lyt);

                    var roomNames = lyt.Rooms
                            .Select(r => r.Name.ToLower())
                            .Where(r => !r.Contains("****") ||  // remove invalid
                                        !r.Contains("stunt"));  // remove cutscene
                    if (!roomNames.Any()) continue;         // Only store RIM if it has rooms.

                    var rimWoks = kdm.RoomNameToWok
                            .Where(kvp => roomNames.Contains(kvp.Key))
                            .Select(kvp => kvp.Value);
                    if (!rimWoks.Any()) continue;   // Only store RIM if it has WOKs.

                    kdm.RimNameToWoks.Add(rimName, rimWoks);
                }
                else
                {
                    Console.WriteLine($"ERROR: no layout file corresponds to the name '{lytResRef}'.");
                }
            }
        }
        #endregion
    }
}

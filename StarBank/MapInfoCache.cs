using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StarBank.Bank_Stuffs;

namespace StarBank
{
    public class MapInfoCache
    {
        private readonly string CACHE_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            @"Blizzard Entertainment\Battle.net\Cache");

        //Regex to find the map-name within the DocumentHeader file
        private readonly Regex MAP_NAME_REGEX = new Regex("DocInfo/Name.....\x00(.+?).\x00", RegexOptions.Compiled | RegexOptions.Singleline);

        private readonly BankInfoLoader _bankInfoLoader;
        private readonly MapCacheStore _mapCacheStore = new MapCacheStore();

        //For progress bars
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        private void OnProgressChanged(double progress)
        {
            if(ProgressChanged != null)
                ProgressChanged(this, new ProgressChangedEventArgs((int) (progress*100), null));
        }

        public MapInfoCache(BankInfoLoader bankInfoLoader)
        {
            _bankInfoLoader = bankInfoLoader;
        }

        /// <summary>
        /// Returns a list of all SCII maps found on the system (in the cache).
        /// Removed duplicates (only the latest version of a map is returned) based on the map's name
        /// Note that BankInfoCache.InitializeCache() MUST be called before this method!!
        /// </summary>
        public IEnumerable<MapInfo> GetMaps()
        {
            MapProtection mapProtection = new MapProtection();
            SortedList<string, MapInfo> mapList = new SortedList<string, MapInfo>();
            DirectoryInfo cacheFolder = new DirectoryInfo(CACHE_FOLDER);
            if(!cacheFolder.Exists)
                return mapList.Values;

            IEnumerable<FileInfo> mapFiles = cacheFolder.GetFiles("*.s2ma", SearchOption.AllDirectories);
            int numMapFiles = mapFiles.Count();
            int numMapsProcessed = 0;

            //Info about maps that haven't changed since the last run is loaded from disk instead of re-parsing the map
            Dictionary<string, MapCacheEntry> storedEntries = _mapCacheStore.Load();
            ConcurrentDictionary<string, MapCacheEntry> currentEntries =
                new ConcurrentDictionary<string, MapCacheEntry>(StringComparer.OrdinalIgnoreCase);
            int numMapsReparsed = 0;

            //Load the maps in parallel, but don't use every core - the work is mostly disk-bound,
            //and using all cores at normal priority makes the whole system unresponsive
            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Math.Min(4, Environment.ProcessorCount/2))
            };
            Parallel.ForEach(mapFiles, parallelOptions,
                //Initialization
                () => new SortedList<string, MapInfo>(),

                //Loop body
                (file, loopState, mapListThread) =>
                {
                    MapCacheEntry entry;
                    if(!storedEntries.TryGetValue(file.FullName, out entry) || !entry.IsUpToDate(file))
                    {
                        entry = CreateMapCacheEntry(file, mapProtection);
                        Interlocked.Increment(ref numMapsReparsed);
                    }
                    currentEntries[file.FullName] = entry;

                    MapInfo mapInfo = CreateMapInfo(file, entry);
                    if(mapInfo != null)
                    {
                        bool mapAdded = CheckForDuplicatesAndAdd(mapInfo, mapListThread);
                        if(mapAdded)
                        {
                            //Bank files change between runs, so match the bank names to bank files every time
                            mapInfo.BankInfos = _bankInfoLoader.GetBanksFromBankNames(entry.BankNames);
                        }
                    }
                    Interlocked.Increment(ref numMapsProcessed);
                    OnProgressChanged((double) numMapsProcessed/numMapFiles);
                    return mapListThread;
                },

                //Finalization
                mapListThread =>
                {
                    lock(mapList)
                    {
                        foreach(var key in mapListThread.Keys)
                        {
                            CheckForDuplicatesAndAdd(mapListThread[key], mapList);
                        }
                    }
                }
                );

            //Only rewrite the cache file if something changed (maps added, updated or removed)
            if(numMapsReparsed > 0 || currentEntries.Count != storedEntries.Count)
            {
                _mapCacheStore.Save(currentEntries.Values);
            }

            return mapList.Values;
        }

        /// <summary>
        /// Opens the map and computes everything about it that is worth storing between runs
        /// </summary>
        private MapCacheEntry CreateMapCacheEntry(FileInfo file, MapProtection mapProtection)
        {
            MapCacheEntry entry = new MapCacheEntry();
            entry.Path = file.FullName;
            entry.Length = file.Length;
            entry.LastWriteTimeUtcTicks = file.LastWriteTimeUtc.Ticks;

            using(StormLibWrapper.MpqArchive archive = new StormLibWrapper.MpqArchive(file.FullName))
            {
                string galaxyScriptCode = GetGalaxyScriptCode(archive);
                MapInfo mapInfo = GetMapInfo(file, archive, galaxyScriptCode);
                if(mapInfo != null)
                {
                    entry.HasMapInfo = true;
                    entry.Name = mapInfo.Name;
                    entry.AuthorName = mapInfo.AuthorName;
                    entry.IsProtected = mapProtection.IsMapProtected(archive);

                    //Use the galaxyscript-code we already loaded to find the bank-names
                    //(See GetBankNamesFromCode() for more info)
                    entry.BankNames = _bankInfoLoader.GetBankNamesFromCode(galaxyScriptCode);
                }
            }
            return entry;
        }

        /// <summary>
        /// Creates the MapInfo for the given map from its (possibly stored) cache entry.
        /// MapInfo.BankInfos is not set here.
        /// </summary>
        private static MapInfo CreateMapInfo(FileInfo file, MapCacheEntry entry)
        {
            if(!entry.HasMapInfo)
                return null;

            MapInfo mapInfo = new MapInfo();
            mapInfo.CachePath = file.FullName;
            mapInfo.DateCreated = file.LastWriteTime;
            mapInfo.Name = entry.Name;
            mapInfo.AuthorName = entry.AuthorName;
            mapInfo.IsProtected = entry.IsProtected;
            return mapInfo;
        }

        /// <summary>
        /// Returns a MapInfo object representing the given map.  Tries to grab the info from the GalaxyScript file if possible;
        /// otherwise attempts to read it from the DocumentHeader file.
        /// 
        /// MapInfo.IsProtected and MapInfo.BankInfos are not set here, because they are more expensive to compute, but not
        /// always necessary
        /// </summary>
        private MapInfo GetMapInfo(FileInfo file, StormLibWrapper.MpqArchive archive, string galaxyScriptCode)
        {
            MapInfo mapInfo = GetMapInfoFromGalaxyScript(file, galaxyScriptCode);
            if(mapInfo == null)
            {
                //Use the document-header name in cases where we can't find it in the galaxyscript file
                //Usually this name is uglier (and is sometimes filled with garbage??), so we only want to use it if we have to
                mapInfo = GetMapInfoFromDocumentHeader(file, archive);
            }
            // Non-ascii strings contain "?" in the Galaxyscript name
            else if(String.IsNullOrEmpty(mapInfo.Name) || mapInfo.Name.Contains("?"))
            {
                MapInfo headerMapInfo = GetMapInfoFromDocumentHeader(file, archive);
                mapInfo.Name = (headerMapInfo != null ? headerMapInfo.Name : "(unknown)");
            }

            if(mapInfo != null && String.IsNullOrEmpty(mapInfo.AuthorName))
            {
                mapInfo.AuthorName = "(unknown)";
            }

            return mapInfo;
        }

        private MapInfo GetMapInfoFromGalaxyScript(FileInfo file, string galaxyScriptCode)
        {
            string[] lines = galaxyScriptCode.Substring(0, Math.Min(1000, galaxyScriptCode.Length)).Split('\n');
            if (lines.Length < 6)
            {
                return null;
            }

            MapInfo mapInfo = new MapInfo();
            mapInfo.CachePath = file.FullName;
            mapInfo.DateCreated = file.LastWriteTime;
            mapInfo.Name = lines[4].StartsWith("// Name:") ? lines[4].Substring(10).Trim() : null;
            mapInfo.AuthorName = (lines[5].StartsWith("// Author:") ? lines[5].Substring(10).Trim() : null);
            return mapInfo;
        }

        /// <summary>
        /// We can't always rely on the Galaxyscript code for the information we need; some maps remove it.
        /// In those cases, we need to try to parse it from the less reliable DocumentHeader file
        /// </summary>
        private MapInfo GetMapInfoFromDocumentHeader(FileInfo file, StormLibWrapper.MpqArchive archive)
        {
            string documentHeader = GetDocumentHeader(archive);

            Match match = MAP_NAME_REGEX.Match(documentHeader);
            if(!match.Success)
                return null;

            MapInfo mapInfo = new MapInfo();
            mapInfo.CachePath = file.FullName;
            mapInfo.DateCreated = file.LastWriteTime;
            mapInfo.Name = match.Groups[1].Value;
            mapInfo.AuthorName = "(Unknown)";
            return mapInfo;
        }

        private string GetGalaxyScriptCode(StormLibWrapper.MpqArchive archive)
        {
            using(StormLibWrapper.MpqInternalFile galaxyScriptFile = archive.OpenFile("MapScript.galaxy"))
            {
                return galaxyScriptFile.ReadFile();
            }
        }

        private string GetDocumentHeader(StormLibWrapper.MpqArchive archive)
        {
            using(StormLibWrapper.MpqInternalFile documentHeaderFile = archive.OpenFile("DocumentHeader"))
            {
                return documentHeaderFile.ReadFile();
            }
        }

        /// <summary>
        /// Adds the map-info to the list, checking that a newer version of the map has not
        /// already been added
        /// </summary>
        private static bool CheckForDuplicatesAndAdd(MapInfo mapInfo, SortedList<string, MapInfo> mapList)
        {
            //Need a single key to reference into the list, but want to include
            //both map-name and author-name.  Just concatenate them with an "@" symbol or something.
            string key = mapInfo.Name + "@" + mapInfo.AuthorName;

            if(mapList.ContainsKey(key))
            {
                MapInfo potentialDuplicate = mapList[key];
                if(potentialDuplicate.DateCreated >= mapInfo.DateCreated)
                    return false;
                mapList.Remove(key);
            }

            mapList.Add(key, mapInfo);
            return true;
        }

        public void ExtractGalaxyScriptFileTo(MapInfo mapInfo, string extractToPath)
        {
            using(StormLibWrapper.MpqArchive mapArchive = new StormLibWrapper.MpqArchive(mapInfo.CachePath))
            {
                mapArchive.ExtractFile("MapScript.galaxy", extractToPath);
            }
        }
    }
}
using System;
using System.Collections;
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

            //Load the maps in parallel!
            Parallel.ForEach(mapFiles,
                //Initialization
                () => new SortedList<string, MapInfo>(),
    
                //Loop body
                (file, loopState, mapListThread) =>
                {
                    string galaxyScriptCode = GetGalaxyScriptCode(file);
                    MapInfo mapInfo = GetMapInfo(file, galaxyScriptCode);
                    if(mapInfo != null)
                    {
                        bool mapAdded = CheckForDuplicatesAndAdd(mapInfo, mapListThread);
                        if(mapAdded)
                        {
                            //Only do other expensive stuff if map was not a duplicate
                            mapInfo.IsProtected = mapProtection.IsMapProtected(file.FullName);

                            //Use the galaxyscript-code we already loaded to find the bank-names
                            //(See GetBanksFromCode() for more info)
                            mapInfo.BankInfos = _bankInfoLoader.GetBanksFromCode(galaxyScriptCode);
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

            return mapList.Values;
        }

        /// <summary>
        /// Returns a MapInfo object representing the given map.  Tries to grab the info from the GalaxyScript file if possible;
        /// otherwise attempts to read it from the DocumentHeader file.
        /// 
        /// MapInfo.IsProtected and MapInfo.BankInfos are not set here, because they are more expensive to compute, but not
        /// always necessary
        /// </summary>
        private MapInfo GetMapInfo(FileInfo file, string galaxyScriptCode)
        {
            string[] lines = galaxyScriptCode.Substring(0, Math.Min(1000, galaxyScriptCode.Length)).Split('\n');
            if(lines.Length >= 6 && lines[4].StartsWith("// Name:"))
            {
                string mapName = lines[4].Substring(10).Trim();
                if(!string.IsNullOrEmpty(mapName))
                {
                    //Hack:  The map-name and author name are listed as comments in the galaxy-script file
                    //Open that file, find the comments, and read them
                    MapInfo mapInfo = new MapInfo();
                    mapInfo.CachePath = file.FullName;
                    mapInfo.DateCreated = file.LastWriteTime;
                    mapInfo.Name = mapName;
                    mapInfo.AuthorName = (lines[5].StartsWith("// Author:") ? lines[5].Substring(10).Trim() : "(Unknown)");
                    return mapInfo;
                }
            }

            //Use the document-header name in cases where we can't find it in the galaxyscript file
            //Usually this name is uglier (and is sometimes filled with garbage??), so we only want to use it if we have to
            return GetMapInfoFromDocumentHeader(file);
        }

        /// <summary>
        /// We can't always rely on the Galaxyscript code for the information we need; some maps remove it.
        /// In those cases, we need to try to parse it from the less reliable DocumentHeader file
        /// </summary>
        private MapInfo GetMapInfoFromDocumentHeader(FileInfo file)
        {
            string documentHeader = GetDocumentHeader(file);

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

        private string GetGalaxyScriptCode(FileInfo mpqFile)
        {
            using(StormLibWrapper.MpqArchive archive = new StormLibWrapper.MpqArchive(mpqFile.FullName))
            {
                using(StormLibWrapper.MpqInternalFile galaxyScriptFile = archive.OpenFile("MapScript.galaxy"))
                {
                    return galaxyScriptFile.ReadFile();
                }
            }
        }

        private string GetDocumentHeader(FileInfo mpqFile)
        {
            using(StormLibWrapper.MpqArchive archive = new StormLibWrapper.MpqArchive(mpqFile.FullName))
            {
                using(StormLibWrapper.MpqInternalFile documentHeaderFile = archive.OpenFile("DocumentHeader"))
                {
                    return documentHeaderFile.ReadFile();
                }
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
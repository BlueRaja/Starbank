using System;
using System.Collections.Generic;
using System.IO;

namespace StarBank
{
    /// <summary>
    /// The expensive-to-compute information about a single map file in the Battle.net cache.
    /// Stored on disk between runs so unchanged maps don't need to be decompressed and parsed again.
    /// </summary>
    public class MapCacheEntry
    {
        public string Path;
        public long Length;
        public long LastWriteTimeUtcTicks;

        /// <summary>
        /// False if neither the galaxyscript nor the DocumentHeader contained a map name
        /// (so the file is not shown in the list)
        /// </summary>
        public bool HasMapInfo;
        public string Name;
        public string AuthorName;
        public bool IsProtected;
        public List<string> BankNames = new List<string>();

        /// <summary>
        /// Returns true if this entry was created from the given file in its current state
        /// </summary>
        public bool IsUpToDate(FileInfo file)
        {
            return Length == file.Length && LastWriteTimeUtcTicks == file.LastWriteTimeUtc.Ticks;
        }
    }

    /// <summary>
    /// Loads and saves MapCacheEntries to a file in StarBank's data folder (see StarBankDataFolder)
    /// </summary>
    public class MapCacheStore
    {
        private const string CACHE_FILE_NAME = "MapCache.dat";
        private const string FILE_MAGIC = "StarBankMapCache";

        //Increase this whenever the way map info is computed changes, so old cache files get thrown away
        private const int FORMAT_VERSION = 1;

        private readonly string _cacheFilePath = Path.Combine(StarBankDataFolder.Location, CACHE_FILE_NAME);

        /// <summary>
        /// Returns the stored entries, keyed by map path.  Returns an empty dictionary if there is no
        /// cache file or it can't be read (in which case all maps are simply loaded again)
        /// </summary>
        public Dictionary<string, MapCacheEntry> Load()
        {
            if(File.Exists(_cacheFilePath))
            {
                try
                {
                    return ReadCacheFile(_cacheFilePath);
                }
                catch(Exception)
                {
                    //Corrupt or outdated cache file; all maps will be loaded again
                }
            }
            return new Dictionary<string, MapCacheEntry>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Saves the given entries, replacing whatever was stored before.  Failing to save is not fatal.
        /// </summary>
        public void Save(ICollection<MapCacheEntry> entries)
        {
            try
            {
                WriteCacheFile(_cacheFilePath, entries);
            }
            catch(Exception)
            {
                //The maps will simply be loaded again next time
            }
        }

        private static Dictionary<string, MapCacheEntry> ReadCacheFile(string cacheFilePath)
        {
            Dictionary<string, MapCacheEntry> entries = new Dictionary<string, MapCacheEntry>(StringComparer.OrdinalIgnoreCase);
            using(BinaryReader reader = new BinaryReader(File.OpenRead(cacheFilePath)))
            {
                if(reader.ReadString() != FILE_MAGIC || reader.ReadInt32() != FORMAT_VERSION)
                    throw new InvalidDataException("Unknown map cache format");

                int numEntries = reader.ReadInt32();
                for(int i = 0; i < numEntries; i++)
                {
                    MapCacheEntry entry = new MapCacheEntry();
                    entry.Path = reader.ReadString();
                    entry.Length = reader.ReadInt64();
                    entry.LastWriteTimeUtcTicks = reader.ReadInt64();
                    entry.HasMapInfo = reader.ReadBoolean();
                    entry.Name = reader.ReadString();
                    entry.AuthorName = reader.ReadString();
                    entry.IsProtected = reader.ReadBoolean();
                    int numBankNames = reader.ReadInt32();
                    for(int j = 0; j < numBankNames; j++)
                    {
                        entry.BankNames.Add(reader.ReadString());
                    }
                    entries[entry.Path] = entry;
                }
            }
            return entries;
        }

        private static void WriteCacheFile(string cacheFilePath, ICollection<MapCacheEntry> entries)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cacheFilePath));

            //Write to a temp file first, so a crash halfway through never leaves a broken cache file behind
            string tempFilePath = cacheFilePath + ".tmp";
            using(BinaryWriter writer = new BinaryWriter(File.Create(tempFilePath)))
            {
                writer.Write(FILE_MAGIC);
                writer.Write(FORMAT_VERSION);
                writer.Write(entries.Count);
                foreach(MapCacheEntry entry in entries)
                {
                    writer.Write(entry.Path);
                    writer.Write(entry.Length);
                    writer.Write(entry.LastWriteTimeUtcTicks);
                    writer.Write(entry.HasMapInfo);
                    writer.Write(entry.Name ?? "");
                    writer.Write(entry.AuthorName ?? "");
                    writer.Write(entry.IsProtected);
                    writer.Write(entry.BankNames.Count);
                    foreach(string bankName in entry.BankNames)
                    {
                        writer.Write(bankName);
                    }
                }
            }

            if(File.Exists(cacheFilePath))
                File.Replace(tempFilePath, cacheFilePath, null);
            else
                File.Move(tempFilePath, cacheFilePath);
        }
    }
}

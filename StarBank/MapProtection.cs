using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarBank
{
    public class MapProtection
    {
        private const string FILE_COMPONENTLIST = "ComponentList.SC2Components";
        private const string FILE_TRIGGERS = "Triggers";
        private const string FILE_TRIGGERSVERSION = "Triggers.version";
        public bool IsMapProtected(string fileName)
        {
            using(StormLibWrapper.MpqArchive mapArchive = new StormLibWrapper.MpqArchive(fileName))
            {
                return IsMapProtected(mapArchive);
            }
        }

        public bool IsMapProtected(StormLibWrapper.MpqArchive mapArchive)
        {
            return !mapArchive.FileExists(FILE_COMPONENTLIST)
                       || !mapArchive.FileExists(FILE_TRIGGERS)
                       || !mapArchive.FileExists(FILE_TRIGGERSVERSION);
        }

        public void UnprotectMap(string fileName)
        {
            using(StormLibWrapper.MpqArchive mapArchive = new StormLibWrapper.MpqArchive(fileName, false))
            {
                if(IsMapProtected(mapArchive))
                {
                    UnprotectMap(mapArchive);
                }
            }
        }

        private void UnprotectMap(StormLibWrapper.MpqArchive mapArchive)
        {
            if(!IsMapProtected(mapArchive))
                throw new MapNotProtectedException(); //TODO: Handle thrown exception
            if(!mapArchive.FileExists(FILE_COMPONENTLIST))
                mapArchive.CreateFile(FILE_COMPONENTLIST, Properties.Resources.ComponentList);
            if(!mapArchive.FileExists(FILE_TRIGGERS))
                mapArchive.CreateFile(FILE_TRIGGERS, Properties.Resources.Triggers);
            if(!mapArchive.FileExists(FILE_TRIGGERSVERSION))
                mapArchive.CreateFile(FILE_TRIGGERSVERSION, Properties.Resources.Triggersversion);
        }
    }

    public class MapNotProtectedException : Exception 
    {}
}

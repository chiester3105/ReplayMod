using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuclearOption.SceneLoading;

namespace ReplayMod.Data
{
    public struct Header
    {
        public int appId;
        public byte major;
        public byte minor;
        public byte patch;
        public MapKey.KeyType keyType;
        public string mapPrefabName;
        public DateTime recordingDate;
        public double duration;
        public long eventCount;

        public static Header Read(BinaryReader br)
        {
            var appId = br.ReadInt32();
            var major = br.ReadByte();
            var minor = br.ReadByte();
            var patch = br.ReadByte();
            var keyType = (MapKey.KeyType)br.ReadByte();
            var mapPrefabName = br.ReadString();
            var recordingDate = new DateTime(br.ReadInt64());
            br.ReadInt32();
            var duration = br.ReadDouble();
            var eventCount = br.ReadInt64();

            return new Header
            {
                appId = appId,
                major = major,
                minor = minor,
                patch = patch,
                keyType = keyType,
                mapPrefabName = mapPrefabName,
                recordingDate = recordingDate,
                duration = duration,
                eventCount = eventCount,
            };
        }
    }
}

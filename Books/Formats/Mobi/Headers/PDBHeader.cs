using System;
using System.Collections.Generic;
using System.IO;
using ExtensionMethods;

namespace Formats.Mobi.Headers
{
    public class PDBHeader
    {
        private static readonly byte[] nullTwo = new byte[2];
        private static readonly byte[] nullFour = new byte[4];

        public int offset = 0x0;
        public readonly int baseLength = 0x4E;

        private string _title;
        public string title
        {
            get => _title;
            set
            {
                _title = value.Length > 0x20 ? value.Substring(0x0, 0x20) : value + new byte[0x20 - value.Length].Decode();

            }
        }
        public ushort attributes;
        public ushort version;
        public uint createdDate;
        public uint modifiedDate;
        public uint backupDate;
        public uint modificationNum;
        public uint appInfoId;
        public uint sortInfoID;
        public string type;
        public string creator;
        public uint uniqueIDseed;
        public uint nextRecordListID;
        public ushort recordCount;
        public uint[] records;

        /// <summary>
        /// Contains basic metadata for mobi including locations of other headers.
        /// </summary>
        public PDBHeader() { }

        public void FillDefault()
        {
            uint timestamp = (uint)Utils.Metadata.TimeStamp();
            title = "";
            attributes = 0;
            version = 1;
            createdDate = timestamp;
            modifiedDate = timestamp;
            backupDate = 0;
            modificationNum = 0;
            appInfoId = 0;
            sortInfoID = 0;
            type = "BOOK";
            creator = "MOBI";
            uniqueIDseed = (uint)Utils.Metadata.RandomNumber();
            nextRecordListID = 0;
            recordCount = 0;
            records = new uint[0];
        }

        public void Parse(BinaryReader reader)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = reader.ReadBytes(baseLength);  

            title = buffer.SubArray(0x0, 0x20).Decode();
            attributes = Utils.BigEndian.ToUInt16(buffer, 0x20);
            version = Utils.BigEndian.ToUInt16(buffer, 0x22);
            createdDate = Utils.BigEndian.ToUInt32(buffer, 0x24);
            modifiedDate = Utils.BigEndian.ToUInt32(buffer, 0x28);
            backupDate = Utils.BigEndian.ToUInt32(buffer, 0x2C);
            modificationNum = Utils.BigEndian.ToUInt32(buffer, 0x30);
            appInfoId = Utils.BigEndian.ToUInt32(buffer, 0x34);
            sortInfoID = Utils.BigEndian.ToUInt32(buffer, 0x38);
            type = buffer.SubArray(0x3C, 0x4).Decode();
            creator = buffer.SubArray(0x40, 0x4).Decode();
            uniqueIDseed = Utils.BigEndian.ToUInt32(buffer, 0x44);
            nextRecordListID = Utils.BigEndian.ToUInt32(buffer, 0x48);
            recordCount = Utils.BigEndian.ToUInt16(buffer, 0x4C);

            records = new uint[recordCount];

            reader.BaseStream.Seek(0x4E, SeekOrigin.Begin);
            byte[] rawBuffer = reader.ReadBytes(0x8 * recordCount);

            for (var i = 0; i < recordCount; i++)
            {
                records[i] = Utils.BigEndian.ToUInt32(rawBuffer, i * 0x8);
            }
        }

        public byte[] Dump()
        {
            List<byte> output = new List<byte>();
            output.AddRange(title.Encode());

            output.AddRange(Utils.BigEndian.GetBytes(attributes));
            output.AddRange(Utils.BigEndian.GetBytes(version));
            output.AddRange(Utils.BigEndian.GetBytes(createdDate));
            output.AddRange(Utils.BigEndian.GetBytes(modifiedDate));
            output.AddRange(Utils.BigEndian.GetBytes(backupDate));

            output.AddRange(Utils.BigEndian.GetBytes(modificationNum));
            output.AddRange(Utils.BigEndian.GetBytes(appInfoId));
            output.AddRange(Utils.BigEndian.GetBytes(sortInfoID));
            output.AddRange(type.Encode());

            output.AddRange(creator.Encode());
            output.AddRange(Utils.BigEndian.GetBytes(uniqueIDseed));
            output.AddRange(Utils.BigEndian.GetBytes(nextRecordListID));
            output.AddRange(Utils.BigEndian.GetBytes((ushort)records.Length));

            for (int i = 0; i < records.Length; i++)
            {
                output.AddRange(Utils.BigEndian.GetBytes(records[i])); // offset
                output.AddRange(Utils.BigEndian.GetBytes((i * 2) & 0x00FFFFFF)); // attr + uid
            }

            return output.ToArray();
        }

        public void Write(BinaryWriter writer)
        {
            writer.BaseStream.Seek(this.offset, SeekOrigin.Begin);
            writer.Write(Dump());
        }

        public uint recordLength(uint recordNum)
        {
            return records[recordNum + 1] - records[recordNum];
        }

        public void Print()
        {
            Console.WriteLine($@"
PDBHeader:
    title: {title}
    attributes: {attributes}
    version: {version}
    created: {createdDate}
    modified: {modifiedDate}
    backup: {backupDate}
    modnum: {modificationNum}
    appInfoId: {appInfoId}
    sortInfoID: {sortInfoID}
    type: {type}
    creator: {creator}
    uniqueIDseed: {uniqueIDseed}
    nextRecordListID: {nextRecordListID}
    recordCount: {recordCount}
    Records:");
            for (int i = 0; i < records.Length; i++)
            {
                Console.WriteLine($"{i * 2}: ({records[i]}, 0)");
            }
        }
    }

}

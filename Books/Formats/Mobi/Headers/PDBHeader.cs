using System;
using System.Collections.Generic;
using System.IO;
using ExtensionMethods;

namespace Formats.Mobi.Headers
{
    public class PDBHeader
    {
        public readonly int offset = 0x0;
        public readonly int length = 0x4E;

        public string title;
        public short attributes;
        public short version;
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

        public void Parse(BinaryReader reader)
        {

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = reader.ReadBytes(this.length);

            Utils.BitConverter.LittleEndian = false;

            title = buffer.SubArray(0x0, 0x20).Decode();
            attributes = Utils.BitConverter.ToInt16(buffer, 0x20);
            version = Utils.BitConverter.ToInt16(buffer, 0x22);
            createdDate = Utils.BitConverter.ToUInt32(buffer, 0x24);
            modifiedDate = Utils.BitConverter.ToUInt32(buffer, 0x28);
            backupDate = Utils.BitConverter.ToUInt32(buffer, 0x2C);
            modificationNum = Utils.BitConverter.ToUInt32(buffer, 0x30);
            appInfoId = Utils.BitConverter.ToUInt32(buffer, 0x34);
            sortInfoID = Utils.BitConverter.ToUInt32(buffer, 0x38);
            type = buffer.SubArray(0x3C, 0x4).Decode();
            creator = buffer.SubArray(0x40, 0x4).Decode();
            uniqueIDseed = Utils.BitConverter.ToUInt32(buffer, 0x44);
            nextRecordListID = Utils.BitConverter.ToUInt32(buffer, 0x48);
            recordCount = Utils.BitConverter.ToUInt16(buffer, 0x4C);

            records = new uint[recordCount];

            reader.BaseStream.Seek(0x4E, SeekOrigin.Begin);
            byte[] rawBuffer = reader.ReadBytes(0x8 * recordCount);

            Utils.BitConverter.LittleEndian = false;

            for (var i = 0; i < recordCount; i++)
            {
                records[i] = Utils.BitConverter.ToUInt32(rawBuffer, i * 0x8);
            }
        }

        public byte[] Dump()
        {
            Utils.BitConverter.LittleEndian = false;

            List<byte> output = new List<byte>();
            output.AddRange(title.Encode());
            output.AddRange(Utils.BitConverter.GetBytes(attributes));
            output.AddRange(Utils.BitConverter.GetBytes(version));
            output.AddRange(Utils.BitConverter.GetBytes(createdDate));
            output.AddRange(Utils.BitConverter.GetBytes(modifiedDate));
            output.AddRange(Utils.BitConverter.GetBytes(backupDate));
            output.AddRange(Utils.BitConverter.GetBytes(modificationNum));
            output.AddRange(Utils.BitConverter.GetBytes(appInfoId));
            output.AddRange(Utils.BitConverter.GetBytes(sortInfoID));
            output.AddRange(type.Encode());
            output.AddRange(creator.Encode());
            output.AddRange(Utils.BitConverter.GetBytes(uniqueIDseed));
            output.AddRange(Utils.BitConverter.GetBytes(nextRecordListID));
            output.AddRange(Utils.BitConverter.GetBytes((ushort)records.Length));

            for (int i = 0; i < records.Length; i++)
            {
                output.AddRange(Utils.BitConverter.GetBytes(records[i])); // offset
                output.AddRange(Utils.BitConverter.GetBytes((i * 2) & 0x00FFFFFF)); // attr + uid
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

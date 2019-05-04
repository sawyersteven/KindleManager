using System.Collections.Generic;
using ExtensionMethods;

namespace Formats.Mobi.Records
{
    public class INDX
    {
        public const int indxLength = 192;

        private static readonly byte[] nullFour = new byte[4];

        public string magic = "INDX";
        public uint length = indxLength;
        public byte[] unused = nullFour;
        public byte[] unused2 = nullFour;

        public uint type = 0;
        public uint idxtOffset = 0;
        public uint recordCount = 0;
        public uint encoding = 65001;

        public uint languageCode = 0xFFFFFFFF;
        public uint recordEntryCount = 0;
        public uint ordtOffset = 0;
        public uint ligtOffset = 0;

        public uint ligtEntryCount = 0;
        public uint cncxRecordCount = 0;
        public byte[] unused3 = new byte[108];

        public uint ordtType = 0;
        public uint ordtEntryCount = 0;
        public uint ordt1Offset = 0;

        public uint ordt2Offset = 0;
        public uint tagxOffset = 0;
        public byte[] unused4 = nullFour; 
        public byte[] unused5 = nullFour;

        public byte[] Dump()
        {
            List<byte> record = new List<byte>();

            record.AddRange(magic.Encode());
            record.AddRange(Utils.BigEndian.GetBytes(length));
            record.AddRange(unused);
            record.AddRange(unused2);

            record.AddRange(Utils.BigEndian.GetBytes(type));
            record.AddRange(Utils.BigEndian.GetBytes(idxtOffset));
            record.AddRange(Utils.BigEndian.GetBytes(recordCount));
            record.AddRange(Utils.BigEndian.GetBytes(encoding));

            record.AddRange(Utils.BigEndian.GetBytes(languageCode));
            record.AddRange(Utils.BigEndian.GetBytes(recordEntryCount));
            record.AddRange(Utils.BigEndian.GetBytes(ordtOffset));
            record.AddRange(Utils.BigEndian.GetBytes(ligtOffset));

            record.AddRange(Utils.BigEndian.GetBytes(ligtEntryCount));
            record.AddRange(Utils.BigEndian.GetBytes(cncxRecordCount));
            record.AddRange(unused3);

            record.AddRange(Utils.BigEndian.GetBytes(ordtType));
            record.AddRange(Utils.BigEndian.GetBytes(ordtEntryCount));
            record.AddRange(Utils.BigEndian.GetBytes(ordt1Offset));

            record.AddRange(Utils.BigEndian.GetBytes(ordt2Offset));
            record.AddRange(Utils.BigEndian.GetBytes(tagxOffset));
            record.AddRange(unused4);
            record.AddRange(unused5);

            return record.ToArray();
        }
    }
}

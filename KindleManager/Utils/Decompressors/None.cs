namespace Utils.Decompressors
{
    class None : IDecompressor
    {
        public byte[] Decompress(byte[] buffer)
        {
            return buffer;
        }
    }
}

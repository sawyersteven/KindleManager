namespace Utils.Decompressors
{
    public class None : IDecompressor
    {
        public byte[] Decompress(byte[] buffer) => buffer;
    }
}

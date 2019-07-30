namespace Utils.Decompressors
{
    interface IDecompressor
    {
        byte[] Decompress(byte[] buffer);
    }
}

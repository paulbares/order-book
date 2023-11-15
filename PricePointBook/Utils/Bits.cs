namespace PricePointBook.Utils;

public class Bits
{
    
    public static long Pack(int i1, int i2)
    {
        long packed1 = (long)i1 << 32;
        long packed2 = i2 & 0xFFFFFFFFL;
        return packed1 | packed2;
    }

    public static int Unpack1(long packed)
    {
        return (int)(packed >>> 32);
    }

    public static int Unpack2(long packed)
    {
        return (int)(packed & 0xFFFFFFFFL);
    }
}
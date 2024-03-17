﻿namespace Gorgon.BCnEncoder.Shared;

internal static class ByteHelper
{
    public static byte ClampToByte(int i)
    {
        if (i < 0)
        {
            i = 0;
        }

        if (i > 255)
        {
            i = 255;
        }

        return (byte)i;
    }

    public static byte ClampToByte(float f)
        => ClampToByte((int)f);

    public static byte Extract1(ulong source, int index)
    {
        const ulong mask = 0b1UL;
        return (byte)((source >> index) & mask);
    }

    public static ulong Store1(ulong dest, int index, byte value)
    {
        const ulong mask = 0b1UL;
        dest &= ~(mask << index);
        dest |= (value & mask) << index;
        return dest;
    }

    public static byte Extract2(ulong source, int index)
    {
        const ulong mask = 0b11UL;
        return (byte)((source >> index) & mask);
    }

    public static ulong Store2(ulong dest, int index, byte value)
    {
        const ulong mask = 0b11UL;
        dest &= ~(mask << index);
        dest |= (value & mask) << index;
        return dest;
    }

    public static byte Extract4(ulong source, int index)
    {
        const ulong mask = 0b1111UL;
        return (byte)((source >> index) & mask);
    }

    public static ulong Store4(ulong dest, int index, byte value)
    {
        const ulong mask = 0b1111UL;
        dest &= ~(mask << index);
        dest |= (value & mask) << index;
        return dest;
    }

    public static byte Extract6(ulong source, int index)
    {
        const ulong mask = 0b11_1111UL;
        return (byte)((source >> index) & mask);
    }

    public static ulong Store6(ulong dest, int index, byte value)
    {
        const ulong mask = 0b11_1111UL;
        dest &= ~(mask << index);
        dest |= (value & mask) << index;
        return dest;
    }

    public static ulong Extract(ulong source, int index, int bitCount)
    {
        unchecked
        {
            ulong mask = (0b1UL << bitCount) - 1;
            return ((source >> index) & mask);
        }
    }

    public static ulong Store(ulong dest, int index, int bitCount, ulong value)
    {
        unchecked
        {
            ulong mask = (0b1UL << bitCount) - 1;
            dest &= ~(mask << index);
            dest |= (value & mask) << index;
            return dest;
        }
    }

    public static ulong ExtractFrom128(ulong low, ulong high, int index, int bitCount)
    {
        if (index + bitCount <= 64)
        { // Extract from low
            return Extract(low, index, bitCount);
        }
        else if (index >= 64)
        { // Extract from high
            return Extract(high, index - 64, bitCount);
        }
        else
        { //handle boundary case
            int lowIndex = index;
            int lowBitCount = 64 - index;
            int highBitCount = bitCount - lowBitCount;
            int highIndex = 0;

            ulong value = Extract(low, lowIndex, lowBitCount);
            ulong hVal = Extract(high, highIndex, highBitCount);
            value = Store(value, lowBitCount, highBitCount, hVal);
            return value;
        }
    }

    public static (ulong, ulong) StoreTo128(ulong low, ulong high, int index, int bitCount, ulong value)
    {
        if (index + bitCount <= 64)
        { // Store to low
            return (Store(low, index, bitCount, value), high);
        }
        else if (index >= 64)
        { // Store to high
            return (low, Store(high, index - 64, bitCount, value));
        }
        else
        { //handle boundary case
            int lowIndex = index;
            int lowBitCount = 64 - index;
            int highBitCount = bitCount - lowBitCount;
            int highIndex = 0;

            ulong l = Store(low, lowIndex, lowBitCount, value);
            value >>= lowBitCount;
            ulong h = Store(high, highIndex, highBitCount, value);
            return (l, h);
        }
    }
}

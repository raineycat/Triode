namespace SGLib;

using System;

public sealed class LZF
{
    public int Compress(byte[] input, int inputLength, byte[] output, int outputLength)
    {
        Array.Clear(this.HashTable, 0, 16384);
        uint num = 0U;
        uint num2 = 0U;
        uint num3 = (uint)(((int)input[(int)((UIntPtr)num)] << 8) | (int)input[(int)((UIntPtr)(num + 1U))]);
        int num4 = 0;
        for (;;)
        {
            if ((ulong)num < (ulong)((long)(inputLength - 2)))
            {
                num3 = (num3 << 8) | (uint)input[(int)((UIntPtr)(num + 2U))];
                long num5 = (long)((ulong)(((num3 ^ (num3 << 5)) >> (int)(10U - num3 * 5U)) & 16383U));
                long num6 = this.HashTable[(int)(checked((IntPtr)num5))];
                this.HashTable[(int)(checked((IntPtr)num5))] = (long)((ulong)num);
                long num7;
                if ((num7 = (long)((ulong)num - (ulong)num6 - 1UL)) < 8192L &&
                    (ulong)(num + 4U) < (ulong)((long)inputLength) && num6 > 0L &&
                    input[(int)(checked((IntPtr)num6))] == input[(int)((UIntPtr)num)] &&
                    input[(int)(checked((IntPtr)(unchecked(num6 + 1L))))] == input[(int)((UIntPtr)(num + 1U))] &&
                    input[(int)(checked((IntPtr)(unchecked(num6 + 2L))))] == input[(int)((UIntPtr)(num + 2U))])
                {
                    uint num8 = 2U;
                    uint num9 = (uint)(inputLength - (int)num - (int)num8);
                    num9 = ((num9 > 264U) ? 264U : num9);
                    if ((ulong)num2 + (ulong)((long)num4) + 1UL + 3UL >= (ulong)((long)outputLength))
                    {
                        break;
                    }

                    do
                    {
                        num8 += 1U;
                    } while (num8 < num9 && input[(int)(checked((IntPtr)(unchecked(num6 + (long)((ulong)num8)))))] ==
                             input[(int)((UIntPtr)(num + num8))]);

                    if (num4 != 0)
                    {
                        output[(int)((UIntPtr)(num2++))] = (byte)(num4 - 1);
                        num4 = -num4;
                        do
                        {
                            output[(int)((UIntPtr)(num2++))] =
                                input[(int)(checked((IntPtr)(unchecked((ulong)num + (ulong)((long)num4)))))];
                        } while (++num4 != 0);
                    }

                    num8 -= 2U;
                    num += 1U;
                    if (num8 < 7U)
                    {
                        output[(int)((UIntPtr)(num2++))] = (byte)((num7 >> 8) + (long)((ulong)((ulong)num8 << 5)));
                    }
                    else
                    {
                        output[(int)((UIntPtr)(num2++))] = (byte)((num7 >> 8) + 224L);
                        output[(int)((UIntPtr)(num2++))] = (byte)(num8 - 7U);
                    }

                    output[(int)((UIntPtr)(num2++))] = (byte)num7;
                    num += num8 - 1U;
                    num3 = (uint)(((int)input[(int)((UIntPtr)num)] << 8) | (int)input[(int)((UIntPtr)(num + 1U))]);
                    num3 = (num3 << 8) | (uint)input[(int)((UIntPtr)(num + 2U))];
                    this.HashTable[(int)((UIntPtr)(((num3 ^ (num3 << 5)) >> (int)(10U - num3 * 5U)) & 16383U))] =
                        (long)((ulong)num);
                    num += 1U;
                    num3 = (num3 << 8) | (uint)input[(int)((UIntPtr)(num + 2U))];
                    this.HashTable[(int)((UIntPtr)(((num3 ^ (num3 << 5)) >> (int)(10U - num3 * 5U)) & 16383U))] =
                        (long)((ulong)num);
                    num += 1U;
                    continue;
                }
            }
            else if ((ulong)num == (ulong)((long)inputLength))
            {
                goto IL_026E;
            }

            num4++;
            num += 1U;
            if ((long)num4 == 32L)
            {
                if ((ulong)(num2 + 1U + 32U) >= (ulong)((long)outputLength))
                {
                    return 0;
                }

                output[(int)((UIntPtr)(num2++))] = 31;
                num4 = -num4;
                do
                {
                    output[(int)((UIntPtr)(num2++))] =
                        input[(int)(checked((IntPtr)(unchecked((ulong)num + (ulong)((long)num4)))))];
                } while (++num4 != 0);
            }
        }

        return 0;
        IL_026E:
        if (num4 != 0)
        {
            if ((ulong)num2 + (ulong)((long)num4) + 1UL >= (ulong)((long)outputLength))
            {
                return 0;
            }

            output[(int)((UIntPtr)(num2++))] = (byte)(num4 - 1);
            num4 = -num4;
            do
            {
                output[(int)((UIntPtr)(num2++))] =
                    input[(int)(checked((IntPtr)(unchecked((ulong)num + (ulong)((long)num4)))))];
            } while (++num4 != 0);
        }

        return (int)num2;
    }

    public unsafe int Decompress(byte[] input, int inputLength, byte[] output, int outputLength)
    {
        fixed (byte* ptr = input)
        {
            fixed (byte* ptr2 = output)
            {
                return this.Decompress(ptr, inputLength, ptr2, outputLength);
            }
        }
    }

    public unsafe int Decompress(byte* input, int inputLength, byte* output, int outputLength)
    {
        uint num = 0U;
        uint num2 = 0U;
        for (;;)
        {
            uint num3 = (uint)input[num++];
            if (num3 < 32U)
            {
                num3 += 1U;
                if ((ulong)(num2 + num3) > (ulong)((long)outputLength))
                {
                    break;
                }

                do
                {
                    output[num2++] = input[num++];
                } while ((num3 -= 1U) != 0U);
            }
            else
            {
                uint num4 = num3 >> 5;
                int num5 = (int)(num2 - ((num3 & 31U) << 8) - 1U);
                if (num4 == 7U)
                {
                    num4 += (uint)input[num++];
                }

                num5 -= (int)input[num++];
                if ((ulong)(num2 + num4 + 2U) > (ulong)((long)outputLength))
                {
                    return 0;
                }

                if (num5 < 0)
                {
                    return 0;
                }

                output[num2++] = output[num5++];
                output[num2++] = output[num5++];
                do
                {
                    output[num2++] = output[num5++];
                } while ((num4 -= 1U) != 0U);
            }

            if ((ulong)num >= (ulong)((long)inputLength))
            {
                return (int)num2;
            }
        }

        return 0;
    }

    private readonly long[] HashTable = new long[16384];
}
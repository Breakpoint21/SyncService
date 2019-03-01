using System;
using System.Text;

namespace SyncService.Services.Sync
{
    public static class Extensions
    {
        public static string ToHashString(this byte[] hash)
        {
            var stringBuilder = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                stringBuilder.AppendFormat("{0:x2}", b);
            }

            return stringBuilder.ToString();
        }

        private const byte CarryZero = 0x00;
        private const byte CarryOne = 0x01;

        public static void AddHash(this Span<byte> hash, Span<byte> other)
        {
            if (hash.Length != other.Length) throw new InvalidOperationException("The length of both array must be the same!");
            byte carry = 0x00;
            for (int i = hash.Length - 1; i >= 0; i--)
            {
                if (i == hash.Length - 1)
                {
                    var i1 = hash[i] + other[i];
                    carry = i1 > byte.MaxValue ? CarryOne : CarryZero;
                    hash[i] = (byte)(i1 % 256);
                }
                else
                {
                    var i1 = hash[i] + other[i] + carry;
                    carry = i1 > byte.MaxValue ? CarryOne : CarryZero;
                    hash[i] = (byte)(i1 % 256);
                }
            }
        }
        public static void AddHash(this byte[] hash, Span<byte> other)
        {
            if(hash.Length != other.Length) throw new InvalidOperationException("The length of both array must be the same!");
            byte carry = 0x00;
            for (int i = hash.Length - 1; i >= 0; i--)
            {
                if (i == hash.Length - 1)
                {
                    var i1 = hash[i] + other[i];
                    carry = i1 > byte.MaxValue ? CarryOne : CarryZero;
                    hash[i] = (byte)(i1 % 256);
                }
                else
                {
                    var i1 = hash[i] + other[i] + carry;
                    carry = i1 > byte.MaxValue ? CarryOne : CarryZero;
                    hash[i] = (byte)(i1 % 256);
                }
            }
        }
    }
}
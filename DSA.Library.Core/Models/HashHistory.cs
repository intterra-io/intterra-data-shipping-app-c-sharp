using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Lib.Core.Models
{
    public class HashHistory
    {
        public string Name { get; set; }
        public byte[][] HashData { get; set; }

        public HashHistory(string name)
        {
            Name = name;
        }

        public void AppendHashes(byte[][] hashes)
        {
            HashData = hashes;
        }
    }


    public static class DataExtensions
    {
        public static bool SafeEquals(this byte[] strA, byte[] strB)
        {
            int length = strA.Length;
            if (length != strB.Length)
            {
                return false;
            }
            for (int i = 0; i < length; i++)
            {
                if (strA[i] != strB[i]) return false;
            }
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Lib.Models
{
    public class HashHistory
    {
        public byte[][] Incidents { get; set; }

        public byte[][] Units { get; set; }

        public void AppendIcidentHashes(byte[][] hashes)
        {
            if (Incidents == null)
            {
                Incidents = hashes;
                return;
            }

            hashes.CopyTo(Incidents, Incidents.Length - 1);
        }

        public void AppendUnitHashes(byte[][] hashes)
        {
            if (Units == null)
            {
                Units = hashes;
                return;
            }

            hashes.CopyTo(Units, Units.Length - 1);
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

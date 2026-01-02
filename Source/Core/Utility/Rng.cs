using System.Security.Cryptography;

namespace Mario2
{
    public static class Rng
    {
        /// <param name="min">Inclusive lower bounds</param>
        /// <param name="max">Exclusive upper bounds</param>
        public static int GetInt(int min, int max)
        {
            if (min > max)
            {
                return max;
            }


            return RandomNumberGenerator.GetInt32(min, max);
        }

        /// <param name="min">Inclusive lower bounds</param>
        /// <param name="max">Exclusive upper bounds</param>
        public static float GetFloat(float min, float max, float precision = 1000f)
        {
            if (min >= max)
            {
                return max;
            }

            return RandomNumberGenerator.GetInt32((int)(min * precision), (int)(max * precision)) / precision;
        }

        public static void Shuffle<T>(this List<T> list)
        {
            for (int n = list.Count - 1; n > 0; n--)
            {
                int k = GetInt(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}

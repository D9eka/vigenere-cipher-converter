using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lab2.Models.Alphabets;

namespace Lab2.Cipher
{
    public static class VigenereCipher
    {
        private const int DefaultMaxKeyLength = 12;
        private const int TopDivisorCandidates = 6;

        public static (string Result, string Key) Encode(string text, string key, Alphabet alphabet)
        {
            string plain = Preprocess(text, alphabet);
            string procKey = Preprocess(key, alphabet);

            var sb = new StringBuilder(plain.Length);
            int ki = 0;
            for (int i = 0; i < plain.Length; i++)
            {
                int p = plain[i] - alphabet.StartCharIndex;
                int s = procKey[ki] - alphabet.StartCharIndex;
                int c = (p + s) % alphabet.MaxShift + alphabet.StartCharIndex;
                sb.Append((char)c);
                ki = (ki + 1) % procKey.Length;
            }

            return (Group(sb.ToString()), procKey);
        }

        public static (string Result, string Key) Decode(string cipher, string key, Alphabet alphabet)
        {
            string procCipher = Preprocess(cipher, alphabet);
            string procKey = Preprocess(key, alphabet);

            var sb = new StringBuilder(procCipher.Length);
            int ki = 0;
            for (int i = 0; i < procCipher.Length; i++)
            {
                int c = procCipher[i] - alphabet.StartCharIndex;
                int s = procKey[ki] - alphabet.StartCharIndex;
                int p = (c - s + alphabet.MaxShift) % alphabet.MaxShift + alphabet.StartCharIndex;
                sb.Append((char)p);
                ki = (ki + 1) % procKey.Length;
            }

            return (Group(sb.ToString()), procKey);
        }

        public static (string Result, string Key) Hack(string cipher, Alphabet alphabet, int maxKeyLength = DefaultMaxKeyLength)
        {
            string procCipher = Preprocess(cipher, alphabet);
            var candidateLengths = GetCandidateKeyLengths(procCipher, alphabet, maxKeyLength);

            double bestScore = double.MaxValue;
            string bestPlain = string.Empty;
            string bestKey = string.Empty;

            foreach (int keyLen in candidateLengths.Distinct().Where(x => x >= 1 && x <= maxKeyLength))
            {
                var keyChars = new char[keyLen];

                // Для каждой позиции ключа находим оптимальный сдвиг
                for (int pos = 0; pos < keyLen; pos++)
                {
                    string subseq = string.Concat(procCipher.Where((ch, idx) => idx % keyLen == pos));
                    int shift = FindBestShiftBySumSq(subseq, alphabet);
                    keyChars[pos] = (char)(alphabet.StartCharIndex + shift);
                }

                string candidateKey = new string(keyChars);
                string plain = Decode(procCipher, candidateKey, alphabet).Result.Replace(" ", "");
                double score = ComputeSumSq(plain, alphabet);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPlain = plain;
                    bestKey = candidateKey;
                }
            }

            return (Group(bestPlain), bestKey);
        }

        private static string Preprocess(string input, Alphabet alphabet)
        {
            string s = input.ToLower();

            if (alphabet.CharsToReplace != null && alphabet.CharsToReplace.Count > 0)
            {
                foreach (var kv in alphabet.CharsToReplace)
                    s = s.Replace(kv.Key.ToString(), kv.Value.ToString());
            }

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= alphabet.StartCharIndex && c <= alphabet.EndCharIndex)
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private static string Group(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0 && i % 5 == 0)
                    sb.Append(' ');
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private static IEnumerable<int> GetCandidateKeyLengths(string text, Alphabet alphabet, int maxKeyLength)
        {
            var divisorsCount = new Dictionary<int, int>();

            // Метод Казиского — ищем повторяющиеся шаблоны длины 3..5
            for (int patternLen = 3; patternLen <= 5; patternLen++)
            {
                for (int i = 0; i <= text.Length - patternLen; i++)
                {
                    string pattern = text.Substring(i, patternLen);
                    int j = text.IndexOf(pattern, i + patternLen, StringComparison.Ordinal);
                    while (j >= 0)
                    {
                        int dist = j - i;
                        foreach (int div in GetDivisors(dist))
                        {
                            if (div >= 2 && div <= maxKeyLength)
                            {
                                if (!divisorsCount.ContainsKey(div)) divisorsCount[div] = 0;
                                divisorsCount[div]++;
                            }
                        }
                        j = text.IndexOf(pattern, j + patternLen, StringComparison.Ordinal);
                    }
                }
            }

            var topDivisors = divisorsCount
                .OrderByDescending(kv => kv.Value)
                .Take(TopDivisorCandidates)
                .Select(kv => kv.Key)
                .ToList();

            var candidates = new List<int>();
            candidates.AddRange(topDivisors);

            // Расширяем окрестности ±2 для каждого кандидата
            foreach (var d in topDivisors)
            {
                for (int delta = -2; delta <= 2; delta++)
                {
                    int v = d + delta;
                    if (v >= 1 && v <= maxKeyLength && !candidates.Contains(v))
                        candidates.Add(v);
                }
            }

            // Если кандидатов не найдено — используем запасной набор
            if (!candidates.Any())
            {
                for (int i = 2; i <= Math.Min(12, maxKeyLength); i++)
                    candidates.Add(i);
            }

            return candidates.Distinct().OrderBy(x => x);
        }

        private static IEnumerable<int> GetDivisors(int n)
        {
            var res = new List<int>();
            for (int d = 1; d * d <= n; d++)
            {
                if (n % d == 0)
                {
                    res.Add(d);
                    if (d != n / d) res.Add(n / d);
                }
            }
            return res;
        }

        private static int FindBestShiftBySumSq(string subseq, Alphabet alphabet)
        {
            int L = subseq.Length;
            if (L == 0) return 0;

            double bestScore = double.MaxValue;
            int bestShift = 0;
            var langFreq = alphabet.Frequencies;

            for (int shift = 0; shift < alphabet.MaxShift; shift++)
            {
                var counts = new Dictionary<char, int>();
                foreach (char c in subseq)
                {
                    int dec = (c - alphabet.StartCharIndex - shift + alphabet.MaxShift) % alphabet.MaxShift + alphabet.StartCharIndex;
                    char ch = (char)dec;
                    if (!counts.ContainsKey(ch)) counts[ch] = 0;
                    counts[ch]++;
                }

                double chi2 = 0.0;
                foreach (var kv in langFreq)
                {
                    double expectedCount = kv.Value * L;
                    double observedCount = counts.ContainsKey(kv.Key) ? counts[kv.Key] : 0;
                    if (expectedCount > 0)
                        chi2 += (observedCount - expectedCount) * (observedCount - expectedCount) / expectedCount;
                }

                if (chi2 < bestScore)
                {
                    bestScore = chi2;
                    bestShift = shift;
                }
            }

            return bestShift;
        }

        private static double ComputeSumSq(string text, Alphabet alphabet)
        {
            Dictionary<char, int> counts = new();
            int total = 0;

            foreach (char c in text)
            {
                if (alphabet.Frequencies.ContainsKey(c))
                {
                    if (!counts.ContainsKey(c)) counts[c] = 0;
                    counts[c]++;
                    total++;
                }
            }

            Dictionary<char, double> obs = new();
            foreach (var kv in alphabet.Frequencies)
            {
                char ch = kv.Key;
                obs[ch] = counts.ContainsKey(ch) ? (double)counts[ch] / total : 0.0;
            }

            double sumSq = 0.0;
            foreach (var kv in alphabet.Frequencies)
            {
                double exp = kv.Value;
                double ob = obs[kv.Key];
                sumSq += (ob - exp) * (ob - exp);
            }

            return sumSq;
        }
    }
}

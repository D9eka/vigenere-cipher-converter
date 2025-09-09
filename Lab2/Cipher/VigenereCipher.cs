using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lab2.Models.Alphabets;

namespace Lab2.Cipher
{
    public static class VigenereCipher
    {
        private const int DefaultMaxKeyLength = 40;
        private const int TopDivisorCandidates = 6;

        #region Public API

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
                    int shift = FindBestShiftByChiSquared(subseq, alphabet);
                    keyChars[pos] = (char)(alphabet.StartCharIndex + shift);
                }

                string candidateKey = new string(keyChars);
                string plain = Decode(procCipher, candidateKey, alphabet).Result.Replace(" ", "");
                double score = ComputeChiSquareForText(plain, alphabet);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPlain = plain;
                    bestKey = candidateKey;
                }
            }

            return (Group(bestPlain), bestKey);
        }

        #endregion

        #region Preprocess & Helpers

        private static string Preprocess(string input, Alphabet alphabet)
        {
            if (input == null) return string.Empty;
            string s = input.ToLowerInvariant();

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

        #endregion

        #region Kasiski / Friedman

        private static IEnumerable<int> GetCandidateKeyLengths(string text, Alphabet alphabet, int maxKeyLength)
        {
            var divisorsCount = new Dictionary<int, int>();

            // Метод Казиского
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

            // Метод Фридмана
            double ic = ComputeIndexOfCoincidence(text);
            double langIC = 0.055; // Средний IC для русского языка
            double randIC = 1.0 / alphabet.MaxShift;
            int friedmanEstimate = 3;

            if (Math.Abs(ic - randIC) > 1e-9)
            {
                double est = (langIC - randIC) / (ic - randIC);
                if (est > 0 && !double.IsNaN(est) && !double.IsInfinity(est))
                    friedmanEstimate = Math.Max(1, (int)Math.Round(est));
            }

            var candidates = new List<int>();
            candidates.AddRange(topDivisors);

            if (!candidates.Contains(friedmanEstimate) && friedmanEstimate >= 1 && friedmanEstimate <= maxKeyLength)
                candidates.Add(friedmanEstimate);

            foreach (var d in topDivisors.Concat(new[] { friedmanEstimate }))
            {
                for (int delta = -2; delta <= 2; delta++)
                {
                    int v = d + delta;
                    if (v >= 1 && v <= maxKeyLength && !candidates.Contains(v))
                        candidates.Add(v);
                }
            }

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

        private static double ComputeIndexOfCoincidence(string text)
        {
            int N = text.Length;
            if (N <= 1) return 0.0;
            var counts = new Dictionary<char, int>();
            foreach (char c in text)
            {
                if (!counts.ContainsKey(c)) counts[c] = 0;
                counts[c]++;
            }
            double sum = 0;
            foreach (var kv in counts)
                sum += kv.Value * (kv.Value - 1);

            return sum / (N * (N - 1));
        }

        #endregion

        #region χ²

        private static int FindBestShiftByChiSquared(string subseq, Alphabet alphabet)
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

        private static double ComputeChiSquareForText(string text, Alphabet alphabet)
        {
            int N = text.Length;
            if (N == 0) return double.MaxValue;

            var counts = new Dictionary<char, int>();
            foreach (char c in text)
            {
                if (!counts.ContainsKey(c)) counts[c] = 0;
                counts[c]++;
            }

            double chi2 = 0.0;
            foreach (var kv in alphabet.Frequencies)
            {
                double expectedCount = kv.Value * N;
                double observedCount = counts.ContainsKey(kv.Key) ? counts[kv.Key] : 0;
                if (expectedCount > 0)
                    chi2 += (observedCount - expectedCount) * (observedCount - expectedCount) / expectedCount;
            }

            return chi2;
        }

        #endregion
    }
}

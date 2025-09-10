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
        private const int TopDivisorCandidatesCount = 6;

        public static (string Result, string Key) Encrypt(string plaintext, string key, Alphabet alphabet)
        {
            string normalizedText = NormalizeText(plaintext, alphabet);
            string processedKey = NormalizeText(key, alphabet);

            Func<int, int, int> encryptOperation = (textPos, keyShift) => (textPos + keyShift) % alphabet.MaxShift;
            string ciphertext = ProcessText(normalizedText, processedKey, alphabet, encryptOperation);

            return (FormatInGroups(ciphertext), processedKey);
        }

        public static (string Result, string Key) Decrypt(string ciphertext, string key, Alphabet alphabet)
        {
            string normalizedCiphertext = NormalizeText(ciphertext, alphabet);
            string processedKey = NormalizeText(key, alphabet);

            Func<int, int, int> decryptOperation = (textPos, keyShift) => (textPos - keyShift + alphabet.MaxShift) % alphabet.MaxShift;
            string plaintext = ProcessText(normalizedCiphertext, processedKey, alphabet, decryptOperation);

            return (FormatInGroups(plaintext), processedKey);
        }

        private static string ProcessText(string inputText, string key, Alphabet alphabet, Func<int, int, int> shiftOperation)
        {
            var resultBuilder = new StringBuilder(inputText.Length);
            int keyIndex = 0;

            for (int textIndex = 0; textIndex < inputText.Length; textIndex++)
            {
                int textPos = inputText[textIndex] - alphabet.StartCharIndex;
                int keyShift = key[keyIndex] - alphabet.StartCharIndex;
                int resultPos = shiftOperation(textPos, keyShift) + alphabet.StartCharIndex;
                resultBuilder.Append((char)resultPos);
                keyIndex = (keyIndex + 1) % key.Length;
            }

            return resultBuilder.ToString();
        }

        public static (string Result, string Key) Cryptanalyze(string ciphertext, Alphabet alphabet, int maxKeyLength = DefaultMaxKeyLength)
        {
            string normalizedCiphertext = NormalizeText(ciphertext, alphabet);
            var candidateKeyLengths = FindPossibleKeyLengths(normalizedCiphertext, alphabet, maxKeyLength);

            double bestDeviationScore = double.MaxValue;
            string bestPlaintext = string.Empty;
            string bestKey = string.Empty;

            foreach (int keyLength in candidateKeyLengths.Distinct().Where(length => length >= 1 && length <= maxKeyLength))
            {
                var keyCharacters = new char[keyLength];

                for (int keyPosition = 0; keyPosition < keyLength; keyPosition++)
                {
                    string subsequence = string.Concat(normalizedCiphertext.Where((ch, index) => index % keyLength == keyPosition));
                    int bestShift = FindBestShiftUsingChiSquared(subsequence, alphabet);
                    keyCharacters[keyPosition] = (char)(alphabet.StartCharIndex + bestShift);
                }

                string candidateKey = new string(keyCharacters);
                string candidatePlaintext = Decrypt(normalizedCiphertext, candidateKey, alphabet).Result.Replace(" ", "");
                double deviationScore = ComputeFrequencySquaredDeviation(candidatePlaintext, alphabet);

                if (deviationScore < bestDeviationScore)
                {
                    bestDeviationScore = deviationScore;
                    bestPlaintext = candidatePlaintext;
                    bestKey = candidateKey;
                }
            }

            return (FormatInGroups(bestPlaintext), bestKey);
        }

        private static string NormalizeText(string input, Alphabet alphabet)
        {
            string lowercased = input.ToLower();

            if (alphabet.CharsToReplace != null && alphabet.CharsToReplace.Count > 0)
            {
                foreach (var replacement in alphabet.CharsToReplace)
                    lowercased = lowercased.Replace(replacement.Key.ToString(), replacement.Value.ToString());
            }

            var filteredBuilder = new StringBuilder(lowercased.Length);
            for (int i = 0; i < lowercased.Length; i++)
            {
                char currentChar = lowercased[i];
                if (currentChar >= alphabet.StartCharIndex && currentChar <= alphabet.EndCharIndex)
                    filteredBuilder.Append(currentChar);
            }

            return filteredBuilder.ToString();
        }

        private static string FormatInGroups(string text)
        {
            var groupedBuilder = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && i % 5 == 0)
                    groupedBuilder.Append(' ');
                groupedBuilder.Append(text[i]);
            }
            return groupedBuilder.ToString();
        }

        private static IEnumerable<int> FindPossibleKeyLengths(string text, Alphabet alphabet, int maxKeyLength)
        {
            var divisorFrequencies = new Dictionary<int, int>();

            for (int patternLength = 3; patternLength <= 5; patternLength++)
            {
                for (int startIndex = 0; startIndex <= text.Length - patternLength; startIndex++)
                {
                    string pattern = text.Substring(startIndex, patternLength);
                    int nextIndex = text.IndexOf(pattern, startIndex + patternLength, StringComparison.Ordinal);
                    while (nextIndex >= 0)
                    {
                        int distance = nextIndex - startIndex;
                        foreach (int divisor in FindDivisors(distance))
                        {
                            if (divisor >= 2 && divisor <= maxKeyLength)
                            {
                                if (!divisorFrequencies.ContainsKey(divisor)) divisorFrequencies[divisor] = 0;
                                divisorFrequencies[divisor]++;
                            }
                        }
                        nextIndex = text.IndexOf(pattern, nextIndex + patternLength, StringComparison.Ordinal);
                    }
                }
            }

            var topDivisors = divisorFrequencies
                .OrderByDescending(kv => kv.Value)
                .Take(TopDivisorCandidatesCount)
                .Select(kv => kv.Key)
                .ToList();

            var candidates = new List<int>(topDivisors);

            foreach (var divisor in topDivisors)
            {
                for (int delta = -2; delta <= 2; delta++)
                {
                    int candidate = divisor + delta;
                    if (candidate >= 1 && candidate <= maxKeyLength && !candidates.Contains(candidate))
                        candidates.Add(candidate);
                }
            }

            if (!candidates.Any())
            {
                for (int length = 2; length <= Math.Min(12, maxKeyLength); length++)
                    candidates.Add(length);
            }

            return candidates.Distinct().OrderBy(length => length);
        }

        private static IEnumerable<int> FindDivisors(int number)
        {
            var divisors = new List<int>();
            for (int divisor = 1; divisor * divisor <= number; divisor++)
            {
                if (number % divisor == 0)
                {
                    divisors.Add(divisor);
                    if (divisor != number / divisor) divisors.Add(number / divisor);
                }
            }
            return divisors;
        }

        private static int FindBestShiftUsingChiSquared(string subsequence, Alphabet alphabet)
        {
            int length = subsequence.Length;
            if (length == 0) return 0;

            double bestChiSquaredScore = double.MaxValue;
            int bestShift = 0;
            var languageFrequencies = alphabet.Frequencies;

            for (int shift = 0; shift < alphabet.MaxShift; shift++)
            {
                var characterCounts = new Dictionary<char, int>();
                foreach (char cipherChar in subsequence)
                {
                    int decodedPos = (cipherChar - alphabet.StartCharIndex - shift + alphabet.MaxShift) % alphabet.MaxShift + alphabet.StartCharIndex;
                    char decodedChar = (char)decodedPos;
                    if (!characterCounts.ContainsKey(decodedChar)) characterCounts[decodedChar] = 0;
                    characterCounts[decodedChar]++;
                }

                double chiSquared = 0.0;
                foreach (var freqPair in languageFrequencies)
                {
                    double expectedCount = freqPair.Value * length;
                    double observedCount = characterCounts.ContainsKey(freqPair.Key) ? characterCounts[freqPair.Key] : 0;
                    if (expectedCount > 0)
                        chiSquared += (observedCount - expectedCount) * (observedCount - expectedCount) / expectedCount;
                }

                if (chiSquared < bestChiSquaredScore)
                {
                    bestChiSquaredScore = chiSquared;
                    bestShift = shift;
                }
            }

            return bestShift;
        }

        private static double ComputeFrequencySquaredDeviation(string text, Alphabet alphabet)
        {
            var characterCounts = new Dictionary<char, int>();
            int totalCharacters = 0;

            foreach (char currentChar in text)
            {
                if (alphabet.Frequencies.ContainsKey(currentChar))
                {
                    if (!characterCounts.ContainsKey(currentChar)) characterCounts[currentChar] = 0;
                    characterCounts[currentChar]++;
                    totalCharacters++;
                }
            }

            var observedFrequencies = new Dictionary<char, double>();
            foreach (var freqPair in alphabet.Frequencies)
            {
                char ch = freqPair.Key;
                observedFrequencies[ch] = characterCounts.ContainsKey(ch) ? (double)characterCounts[ch] / totalCharacters : 0.0;
            }

            double squaredDeviationSum = 0.0;
            foreach (var freqPair in alphabet.Frequencies)
            {
                double expectedFreq = freqPair.Value;
                double observedFreq = observedFrequencies[freqPair.Key];
                squaredDeviationSum += (observedFreq - expectedFreq) * (observedFreq - expectedFreq);
            }

            return squaredDeviationSum;
        }
    }
}
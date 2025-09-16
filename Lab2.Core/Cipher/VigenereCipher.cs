using Lab2.Core.Models.Alphabets;
using System.Text;

namespace Lab2.Core.Cipher;

public static class VigenereCipher
{
    private const int DefaultMaxKeyLength = 12;
    private const int TopDivisorCandidatesCount = 3;

    private const int MinDivisorDelta = 0;
    private const int MaxDivisorDelta = 0;

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
        StringBuilder resultBuilder = new StringBuilder(inputText.Length);
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
        IEnumerable<int> candidateKeyLengths = FindPossibleKeyLengths(normalizedCiphertext, alphabet, maxKeyLength);

        double bestDeviationScore = double.MaxValue;
        string bestPlaintext = string.Empty;
        string bestKey = string.Empty;

        foreach (int keyLength in candidateKeyLengths)
        {
            char[] keyCharacters = new char[keyLength];

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
            foreach (KeyValuePair<char, char> replacement in alphabet.CharsToReplace)
                lowercased = lowercased.Replace(replacement.Key.ToString(), replacement.Value.ToString());
        }

        StringBuilder filteredBuilder = new StringBuilder(lowercased.Length);
        for (int i = 0; i < lowercased.Length; i++)
        {
            char currentChar = lowercased[i];
            if (currentChar >= alphabet.StartCharIndex && currentChar <= alphabet.EndCharIndex)
                filteredBuilder.Append(currentChar);
        }

        return filteredBuilder.ToString();
    }

    private static IEnumerable<int> FindPossibleKeyLengths(string text, Alphabet alphabet, int maxKeyLength)
    {
        Dictionary<int, int> divisorFrequencies = new Dictionary<int, int>();

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
                            if (!divisorFrequencies.ContainsKey(divisor))
                            {
                                divisorFrequencies[divisor] = 0;
                            }
                            divisorFrequencies[divisor]++;
                        }
                    }
                    nextIndex = text.IndexOf(pattern, nextIndex + patternLength, StringComparison.Ordinal);
                }
            }
        }

        List<int> topDivisors = divisorFrequencies
            .OrderByDescending(kv => kv.Value)
            .Take(TopDivisorCandidatesCount)
            .Select(kv => kv.Key)
            .ToList();

        List<int> candidates = new List<int>(topDivisors);

        foreach (int divisor in topDivisors)
        {
            for (int delta = MinDivisorDelta; delta <= MaxDivisorDelta; delta++)
            {
                int candidate = divisor + delta;
                if (candidate >= 1 && candidate <= maxKeyLength && !candidates.Contains(candidate))
                    candidates.Add(candidate);
            }
        }

        if (!candidates.Any())
        {
            for (int length = 2; length <= maxKeyLength; length++)
                candidates.Add(length);
        }

        return candidates.OrderBy(length => length);
    }

    private static IEnumerable<int> FindDivisors(int number)
    {
        List<int> divisors = new List<int>();
        for (int divisor = 1; divisor * divisor <= number; divisor++)
        {
            if (number % divisor == 0)
            {
                divisors.Add(divisor);
                if (divisor != number / divisor)
                {
                    divisors.Add(number / divisor);
                }
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
        Dictionary<char, double> languageFrequencies = alphabet.Frequencies;

        for (int shift = 0; shift < alphabet.MaxShift; shift++)
        {
            Dictionary<char, int> characterCounts = new Dictionary<char, int>();
            foreach (char cipherChar in subsequence)
            {
                int decodedPos = (cipherChar - alphabet.StartCharIndex - shift + alphabet.MaxShift) % alphabet.MaxShift + alphabet.StartCharIndex;
                char decodedChar = (char)decodedPos;
                if (!characterCounts.ContainsKey(decodedChar)) characterCounts[decodedChar] = 0;
                characterCounts[decodedChar]++;
            }

            double chiSquared = 0.0;
            foreach (KeyValuePair<char, double> freqPair in languageFrequencies)
            {
                double expectedCount = freqPair.Value * length;
                double observedCount = characterCounts.ContainsKey(freqPair.Key) ? characterCounts[freqPair.Key] : 0;
                if (expectedCount > 0)
                    chiSquared += Math.Pow(observedCount - expectedCount, 2) / expectedCount;
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
        Dictionary<char, int> characterCounts = new Dictionary<char, int>();
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

        Dictionary<char, double> observedFrequencies = new Dictionary<char, double>();
        foreach (KeyValuePair<char, double> freqPair in alphabet.Frequencies)
        {
            char ch = freqPair.Key;
            observedFrequencies[ch] = characterCounts.ContainsKey(ch) ? (double)characterCounts[ch] / totalCharacters : 0.0;
        }

        double squaredDeviationSum = 0.0;
        foreach (KeyValuePair<char, double> freqPair in alphabet.Frequencies)
        {
            double expectedFreq = freqPair.Value;
            double observedFreq = observedFrequencies[freqPair.Key];
            squaredDeviationSum += (observedFreq - expectedFreq) * (observedFreq - expectedFreq);
        }

        return squaredDeviationSum;
    }

    private static string FormatInGroups(string text)
    {
        StringBuilder groupedBuilder = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (i > 0 && i % 5 == 0)
                groupedBuilder.Append(' ');
            groupedBuilder.Append(text[i]);
        }
        return groupedBuilder.ToString();
    }
}
using Lab2.Models;
using System.Collections.Generic;

namespace Lab2.Models.Alphabets
{
    public class Alphabet : IUiElement
    {
        public AlphabetType Type { get; private set; }
        public string UiName { get; private set; }
        public int MaxShift { get; private set; }
        public int StartCharIndex { get; private set; }
        public int EndCharIndex { get; private set; }
        public Dictionary<char, char> CharsToReplace { get; private set; }
        public Dictionary<char, double> Frequencies { get; private set; }

        public Alphabet(AlphabetType type, string uiName, int startCharIndex, int endCharIndex, Dictionary<char, char> charsToReplace, double[] frequencies)
        {
            Type = type;
            UiName = uiName;
            StartCharIndex = startCharIndex;
            EndCharIndex = endCharIndex;
            CharsToReplace = charsToReplace;
            MaxShift = EndCharIndex - StartCharIndex + 1;
            Frequencies = InitializeFrequencies(startCharIndex, endCharIndex, frequencies);
        }

        private Dictionary<char, double> InitializeFrequencies(int startCharIndex, int endCharIndex, double[] frequencies)
        {
            Dictionary<char, double> freqDict = new Dictionary<char, double>();
            int charCount = endCharIndex - startCharIndex + 1;

            if (frequencies.Length != charCount)
            {
                throw new System.ArgumentException("Frequencies array length must match the alphabet size.");
            }

            for (int i = 0; i < charCount; i++)
            {
                char c = (char)(startCharIndex + i);
                freqDict[c] = frequencies[i];
            }

            return freqDict;
        }
    }
}

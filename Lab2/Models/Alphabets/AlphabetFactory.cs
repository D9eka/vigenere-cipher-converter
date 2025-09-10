using System.Collections.Generic;

namespace Lab2.Models.Alphabets
{
    public static class AlphabetFactory
    {
        public static Alphabet CreateRussianAlphabet()
        {
            double[] frequencies = {
                0.062, 0.014, 0.038, 0.013, 0.025, 0.072, 0.007, 0.016, 0.062,
                0.010, 0.028, 0.035, 0.026, 0.053, 0.090, 0.023, 0.040, 0.045,
                0.053, 0.021, 0.002, 0.009, 0.003, 0.012, 0.006, 0.003, 0.014,
                0.0016, 0.014, 0.003, 0.006, 0.018
            };

            return new Alphabet(
                AlphabetType.Russian,
                "Русский",
                'а',
                'я',
                new Dictionary<char, char> { { 'ё', 'е' } },
                frequencies
            );
        }

        public static Alphabet CreateEnglishAlphabet()
        {
            double[] frequencies = {
                0.08167, 0.01492, 0.02782, 0.04253, 0.12702, 0.02228, 0.02015, 0.06094,
                0.06966, 0.00153, 0.00772, 0.04025, 0.02406, 0.06749, 0.07507, 0.01929,
                0.00095, 0.05987, 0.06327, 0.09056, 0.02758, 0.00978, 0.02360, 0.00150,
                0.01974, 0.00074
            };

            return new Alphabet(
                AlphabetType.English,
                "English",
                'a',
                'z',
                new Dictionary<char, char>(),
                frequencies
            );
        }
    }
}
using System.Collections.Generic;

namespace Lab2.Models.Alphabets
{
    public static class AlphabetFactory
    {
        public static Alphabet CreateRussianAlphabet()
        {

            return new Alphabet(
                AlphabetType.Russian,
                "Русский",
                'а',
                'я',
                new Dictionary<char, char> { { 'ё', 'е' } },
                new double[] {
                    0.0817, // а
                    0.0149, // б
                    0.0454, // в
                    0.0170, // г
                    0.0298, // д
                    0.0845, // е
                    0.0013, // ж
                    0.0165, // з
                    0.0735, // и
                    0.0121, // й
                    0.0348, // к
                    0.0321, // л
                    0.0290, // м
                    0.0670, // н
                    0.1097, // о
                    0.0281, // п
                    0.0402, // р
                    0.0547, // с
                    0.0626, // т
                    0.0262, // у
                    0.0097, // ф
                    0.0253, // х
                    0.0074, // ц
                    0.0201, // ч
                    0.0035, // ш
                    0.0063, // щ
                    0.0004, // ъ
                    0.0337, // ы
                    0.0042, // ь
                    0.0174, // э
                    0.0320, // ю
                    0.0218  // я
                }
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
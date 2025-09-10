using Lab2.Models.Alphabets;
using System.Linq;

namespace Lab2.Services.Input
{
    public class InputValidator
    {
        public InputValidationResult Validate(string input, Alphabet alphabet)
        {
            if (string.IsNullOrWhiteSpace(input))
                return InputValidationResult.Error("Введите текст для обработки.");

            var validChars = input.Count(c =>
                c >= (char)alphabet.StartCharIndex && c <= (char)alphabet.EndCharIndex
                || alphabet.CharsToReplace.ContainsKey(c));

            if (validChars == 0)
                return InputValidationResult.Error("Все символы не соответствуют выбранному алфавиту");

            var validCharsWithSpace = input.Count(c =>
                c >= (char)alphabet.StartCharIndex && c <= (char)alphabet.EndCharIndex
                || alphabet.CharsToReplace.ContainsKey(c) || c == ' ');

            if (validCharsWithSpace < validChars)
                return InputValidationResult.Warning("Некоторые символы не соответствуют алфавиту и будут пропущены");

            return InputValidationResult.Success();
        }
    }
}

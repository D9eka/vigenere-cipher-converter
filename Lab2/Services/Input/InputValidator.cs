using Lab2.Core.Models.Alphabets;
using System.Linq;
using System.Text;

namespace Lab2.Services.Input;

public class InputValidator
{
    private static readonly char[] _validChars = new char[] { ' ', '\n', '\r', '!', ',', '.', '-', ':', '?', ';' };

    public InputValidationResult Validate(string input, Alphabet alphabet, bool spaceIsValidChar = true)
    {
        if (string.IsNullOrWhiteSpace(input))
            return InputValidationResult.Error("Введите текст для обработки");

        int validChars = input.Count(c => IsValidCharacter(c, alphabet, spaceIsValidChar));

        if (validChars == 0)
            return InputValidationResult.Error("Все символы не соответствуют выбранному алфавиту");

        var invalidChars = input
            .Where(c => !IsValidCharacter(c, alphabet, spaceIsValidChar))
            .Distinct()
            .ToList();

        if (invalidChars.Count != 0)
        {
            StringBuilder errorMessage = new StringBuilder("Недопустимые символы в строке: ");
            errorMessage.AppendJoin(", ", invalidChars.Select(c => $"'{c}'"));
            return InputValidationResult.Error(errorMessage.ToString());
        }

        return InputValidationResult.Success();
    }

    private bool IsValidCharacter(char c, Alphabet alphabet, bool spaceIsValidChar)
    {
        return (c >= (char)alphabet.StartCharIndex && c <= (char)alphabet.EndCharIndex)
               || alphabet.CharsToReplace.ContainsKey(c)
               || (spaceIsValidChar && _validChars.Contains(c));
    }
}
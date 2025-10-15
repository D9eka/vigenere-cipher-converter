using Lab2.Core.Models.Alphabets;
using System.Text;

namespace Lab2.Core.Cipher;

public static class CaesarCipher
{
    public static char Encrypt(char plainchar, Alphabet alphabet, int shiftAmount)
    {
        Func<int, int, int> encryptOperation = (textPos, shift) => (textPos + shift) % alphabet.MaxShift;
        char cipherchar = ShiftText(plainchar, alphabet, shiftAmount, encryptOperation);
        return cipherchar;
    }

    public static string Decrypt(string ciphertext, Alphabet alphabet, int shiftAmount)
    {
        StringBuilder plaintext = new StringBuilder();
        foreach(char c in ciphertext)
        {
            plaintext.Append(Decrypt(c, alphabet, shiftAmount));
        }
        return plaintext.ToString();
    }


    public static char Decrypt(char cipherchar, Alphabet alphabet, int shiftAmount)
    {
        Func<int, int, int> decryptOperation = (textPos, shift) => (textPos - shift + alphabet.MaxShift) % alphabet.MaxShift;
        char plainChar = ShiftText(cipherchar, alphabet, shiftAmount, decryptOperation);
        return plainChar;
    }

    private static char ShiftText(char cipherChar, Alphabet alphabet, int shiftAmount, Func<int, int, int> shiftOperation)
    {
        if (cipherChar >= alphabet.StartCharIndex && cipherChar <= alphabet.EndCharIndex)
        {
            int textPos = cipherChar - alphabet.StartCharIndex;
            int newPos = shiftOperation(textPos, shiftAmount) + alphabet.StartCharIndex;
            return (char)newPos;
        }
        return cipherChar;
    }
}
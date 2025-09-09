using Lab2.Models.Alphabets;
using Lab2.Models.Operations;
using System.Collections.Generic;

namespace Lab2.Services
{
    public interface IDataInstaller
    {
        List<Alphabet> GetAlphabets();
        List<Operation> GetOperations();
    }

    public class DefaultDataInstaller : IDataInstaller
    {
        public List<Alphabet> GetAlphabets() => new()
        {
            AlphabetFactory.CreateRussianAlphabet(),
            AlphabetFactory.CreateEnglishAlphabet()
        };

        public List<Operation> GetOperations() => new()
        {
            new Operation(OperationType.Encode, "Зашифровать"),
            new Operation(OperationType.Decode, "Расшифровать"),
            new Operation(OperationType.Hack, "Взломать")
        };
    }
}

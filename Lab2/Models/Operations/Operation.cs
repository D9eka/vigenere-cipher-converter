namespace Lab2.Models.Operations
{
    public class Operation : IUiElement
    {
        public OperationType Type { get; private set; }
        public string UiName { get; private set; }

        public Operation(OperationType type, string uiName)
        {
            Type = type;
            UiName = uiName;
        }

    }
}

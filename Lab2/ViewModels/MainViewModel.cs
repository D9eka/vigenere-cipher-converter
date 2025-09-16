using Lab2.Cipher;
using Lab2.Models.Alphabets;
using Lab2.Models.Operations;
using Lab2.Services;
using Lab2.Services.Input;
using Lab2.Services.Message;
using Lab2.ViewModels.Base;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Lab2.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly Page _page;
        private readonly IMessageService _messages;
        private readonly IDataInstaller _dataInstaller;
        private readonly InputValidator _validator;
        private readonly IClipboardService _clipboard;

        private Alphabet _selectedAlphabet;
        private Operation _selectedOperation;
        private string _inputKey = string.Empty;
        private string _inputText = string.Empty;
        private string _outputText = string.Empty;
        private string _outputKey = string.Empty;

        public List<Alphabet> Alphabets { get; }
        public List<Operation> Operations { get; }
        public List<int> Shifts { get; private set; }

        public Alphabet SelectedAlphabet
        {
            get => _selectedAlphabet;
            set
            {
                SetField(ref _selectedAlphabet, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public Operation SelectedOperation
        {
            get => _selectedOperation;
            set
            {
                SetField(ref _selectedOperation, value);
                UpdateKeyVisibility();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string InputKey
        {
            get => _inputKey;
            set => SetField(ref _inputKey, value);
        }

        public string InputText
        {
            get => _inputText;
            set
            {
                SetField(ref _inputText, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string OutputText
        {
            get => _outputText;
            set
            {
                SetField(ref _outputText, value);
                OnPropertyChanged(nameof(OutputKeyVisibility));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string OutputKey
        {
            get => _outputKey;
            set
            {
                SetField(ref _outputKey, value);
                OnPropertyChanged(nameof(OutputKeyLabel));
            }
        }

        public string OutputKeyLabel => $"Ключ: {OutputKey}";

        public Visibility KeyVisibility { get; private set; } = Visibility.Visible;

        public Visibility OutputKeyVisibility => !string.IsNullOrWhiteSpace(OutputText) && _selectedOperation.Type != OperationType.Decrypt ? Visibility.Visible : Visibility.Collapsed;

        public string ErrorMessage => _messages.Message;
        public MessageType ErrorType => _messages.Type;
        public Visibility ErrorVisibility => _messages.HasMessage ? Visibility.Visible : Visibility.Collapsed;

        public ICommand PasteCommand { get; }
        public ICommand CalculateCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand CopyKeyCommand { get; }

        public MainViewModel(Page page)
        {
            _page = page;
            _messages = new MessageService();
            _dataInstaller = new DefaultDataInstaller();
            _validator = new InputValidator();
            _clipboard = new ClipboardService();

            Alphabets = _dataInstaller.GetAlphabets();
            Operations = _dataInstaller.GetOperations();

            SelectedAlphabet = Alphabets.First();
            SelectedOperation = Operations.First();

            PasteCommand = new RelayCommand(ExecutePaste);
            CalculateCommand = new RelayCommand(ExecuteCalculate, CanExecuteCalculate);
            CopyCommand = new RelayCommand(ExecuteCopy, CanExecuteCopy);
            CopyKeyCommand = new RelayCommand(ExecuteCopyKey, CanExecuteCopyKey);

            CommandManager.RequerySuggested += (s, e) =>
            {
                ((RelayCommand)CalculateCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CopyCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CopyKeyCommand).RaiseCanExecuteChanged();
            };
        }

        private void RefreshErrorBindings()
        {
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(ErrorType));
            OnPropertyChanged(nameof(ErrorVisibility));
        }

        private void ExecutePaste()
        {
            try
            {
                InputText = _clipboard.Paste();
            }
            catch (Exception ex)
            {
                _messages.ShowError($"Ошибка: {ex.Message}");
                RefreshErrorBindings();
            }
        }

        private void ExecuteCalculate()
        {
            try
            {
                InputValidationResult inputResult = _validator.Validate(InputText.ToLower(), SelectedAlphabet);

                if (!inputResult.IsValid)
                {
                    _messages.ShowError($"Текст для обработки: {inputResult.Message}");
                    OutputText = string.Empty;
                    OutputKey = string.Empty;
                    return;
                }

                InputValidationResult keyResult = _validator.Validate(InputKey.ToLower(), SelectedAlphabet);
                if (!keyResult.IsValid && SelectedOperation.Type != OperationType.Cryptanalyze)
                {
                    _messages.ShowError($"Ключ: {keyResult.Message}");
                    OutputText = string.Empty;
                    OutputKey = string.Empty;
                }
                else
                {
                    OutputText = ApplyCipher();
                    string warningMessage = GenerateWarningMessage(inputResult, keyResult);
                    if (!string.IsNullOrWhiteSpace(warningMessage))
                        _messages.ShowWarning(inputResult.Message);
                    else
                        _messages.Clear();
                }
            }
            catch (Exception ex)
            {
                _messages.ShowError($"Ошибка при обработке: {ex.Message}");
                OutputText = string.Empty;
                OutputKey = string.Empty;
            }
            finally
            {
                RefreshErrorBindings();
            }
        }

        private string GenerateWarningMessage(InputValidationResult inputResult, InputValidationResult keyResult)
        {
            if (SelectedOperation.Type != OperationType.Cryptanalyze &&
                inputResult.Type == MessageType.Warning && keyResult.Type == MessageType.Warning)
            {
                return $"Текст для обработки и ключ: {inputResult.Message}";
            }
            if (!inputResult.IsValid)
            {
                return $"Текст для обработки: {inputResult.Message}";
            }
            if (!keyResult.IsValid && SelectedOperation.Type != OperationType.Cryptanalyze)
            {
                return $"Ключ: {keyResult.Message}";
            }
            return string.Empty;
        }

        private string ApplyCipher()
        {
            switch (_selectedOperation.Type)
            {
                case OperationType.Decrypt:
                    (string Result, string Key) result = VigenereCipher.Decrypt(InputText, InputKey, SelectedAlphabet);
                    OutputKey = result.Item2;
                    return result.Item1;

                case OperationType.Encrypt:
                    result = VigenereCipher.Encrypt(InputText, InputKey, SelectedAlphabet);
                    OutputKey = result.Item2;
                    return result.Item1;

                case OperationType.Cryptanalyze:
                    result = VigenereCipher.Cryptanalyze(InputText, SelectedAlphabet);
                    OutputKey = result.Item2;
                    return result.Item1;

                default:
                    throw new InvalidOperationException("Неизвестный тип операции");
            }
        }

        private bool CanExecuteCalculate() => !string.IsNullOrWhiteSpace(InputText);

        private void ExecuteCopy()
        {
            try
            {
                _clipboard.Copy(OutputText);
            }
            catch (Exception ex)
            {
                _messages.ShowError($"Ошибка при копировании: {ex.Message}");
                RefreshErrorBindings();
            }
        }

        private bool CanExecuteCopy() => !string.IsNullOrWhiteSpace(OutputText);

        private void ExecuteCopyKey()
        {
            try
            {
                _clipboard.Copy(OutputKey);
            }
            catch (Exception ex)
            {
                _messages.ShowError($"Ошибка при копировании ключа: {ex.Message}");
                RefreshErrorBindings();
            }
        }

        private bool CanExecuteCopyKey() => !string.IsNullOrWhiteSpace(OutputKey);

        private void UpdateKeyVisibility()
        {
            KeyVisibility = SelectedOperation.Type != OperationType.Cryptanalyze
                ? Visibility.Visible
                : Visibility.Collapsed;
            OnPropertyChanged(nameof(KeyVisibility));
            VisualStateManager.GoToState(_page, KeyVisibility == Visibility.Visible ? "KeyTextBoxVisible" : "KeyTextBoxHidden", true);
        }
    }
}
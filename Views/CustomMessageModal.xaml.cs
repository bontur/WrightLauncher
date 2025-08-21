using System.Windows;
using System.Windows.Media;
using WrightLauncher.Services;

namespace WrightLauncher.Views
{
    public partial class CustomMessageModal : Window
    {
        public enum MessageType
        {
            Information,
            Warning,
            Error,
            Success,
            Question
        }

        public enum MessageResult
        {
            OK,
            Cancel,
            Yes,
            No
        }

        public MessageResult Result { get; private set; } = MessageResult.Cancel;

        public CustomMessageModal()
        {
            InitializeComponent();
        }

        public static MessageResult Show(string message, string title = "", MessageType type = MessageType.Information, bool showCancel = false)
        {
            var modal = new CustomMessageModal();
            if (string.IsNullOrEmpty(title))
                title = LocalizationService.Instance.Translate("MessageModalDefaultTitle");
            modal.SetupModal(message, title, type, showCancel);
            modal.ShowDialog();
            return modal.Result;
        }

        private void SetupModal(string message, string title, MessageType type, bool showCancel)
        {
            TitleText.Text = title;
            MessageText.Text = message;
            
            switch (type)
            {
                case MessageType.Information:
                    IconContainer.Fill = (SolidColorBrush)FindResource("AccentBrush");
                    break;
                case MessageType.Warning:
                    IconContainer.Fill = (SolidColorBrush)FindResource("WarningBrush");
                    break;
                case MessageType.Error:
                    IconContainer.Fill = (SolidColorBrush)FindResource("ErrorBrush");
                    break;
                case MessageType.Success:
                    IconContainer.Fill = (SolidColorBrush)FindResource("SuccessBrush");
                    break;
                case MessageType.Question:
                    IconContainer.Fill = (SolidColorBrush)FindResource("AccentBrush");
                    OkButton.Content = LocalizationService.Instance.Translate("MessageModalYesButton");
                    CancelButton.Content = LocalizationService.Instance.Translate("MessageModalNoButton");
                    showCancel = true;
                    break;
            }

            if (showCancel)
            {
                CancelButton.Visibility = Visibility.Visible;
            }
            else
            {
                CancelButton.Visibility = Visibility.Collapsed;
            }

            AdjustWindowSize(message);
        }

        private void AdjustWindowSize(string message)
        {
            if (message.Length > 200)
            {
                this.Height = 300;
                this.Width = 500;
            }
            else if (message.Length > 100)
            {
                this.Height = 250;
                this.Width = 450;
            }
            else
            {
                this.Height = 200;
                this.Width = 400;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var yesText = LocalizationService.Instance.Translate("MessageModalYesButton");
            Result = OkButton.Content.ToString() == yesText ? MessageResult.Yes : MessageResult.OK;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var noText = LocalizationService.Instance.Translate("MessageModalNoButton");
            Result = CancelButton.Content.ToString() == noText ? MessageResult.No : MessageResult.Cancel;
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageResult.Cancel;
            this.Close();
        }

        public static MessageResult ShowInformation(string message, string title = "")
        {
            if (string.IsNullOrEmpty(title))
                title = LocalizationService.Instance.Translate("MessageModalInfoTitle");
            return Show(message, title, MessageType.Information, false);
        }

        public static MessageResult ShowWarning(string message, string title = "")
        {
            if (string.IsNullOrEmpty(title))
                title = LocalizationService.Instance.Translate("MessageModalWarningTitle");
            return Show(message, title, MessageType.Warning, false);
        }

        public static MessageResult ShowError(string message, string title = "")
        {
            if (string.IsNullOrEmpty(title))
                title = LocalizationService.Instance.Translate("MessageModalErrorTitle");
            return Show(message, title, MessageType.Error, false);
        }

        public static MessageResult ShowSuccess(string message, string title = "")
        {
            if (string.IsNullOrEmpty(title))
                title = LocalizationService.Instance.Translate("MessageModalSuccessTitle");
            return Show(message, title, MessageType.Success, false);
        }

        public static MessageResult ShowQuestion(string message, string title = "")
        {
            if (string.IsNullOrEmpty(title))
                title = LocalizationService.Instance.Translate("MessageModalQuestionTitle");
            return Show(message, title, MessageType.Question, true);
        }
    }
}


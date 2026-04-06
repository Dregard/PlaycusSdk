namespace Unity.Usercentrics
{
    [System.Serializable]
    public class ATTLocalizationMessage
    {
        public Language Language;
        public string PopupMessage = "";
        public string ManualIsoCode;

        public ATTLocalizationMessage(Language language)
        {
            Language = language;
        }
    }
}

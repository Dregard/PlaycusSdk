#if UNITY_IOS
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Unity.Usercentrics
{
    public class ATTPostProcessor : MonoBehaviour
    {
        private const string DefaultStringsPlistFileName = "InfoPlist.strings";
        private const string InfoPlistFileName = "Info.plist";
        private const string MessageKey = "NSUserTrackingUsageDescription";
        private const string EnglishIsoCode = "en";

        private string EnglishMessage;
        private string BuildPath;
        private string ProjectPath;
        private PBXProject XcodeProject = new PBXProject();
        private List<ATTLocalizationMessage> LocalizationMessages;

        public ATTPostProcessor(string buildPath, string englishMessage)
        {
            this.BuildPath = buildPath;
            this.EnglishMessage = englishMessage;
            this.ProjectPath = PBXProject.GetPBXProjectPath(this.BuildPath);
            this.XcodeProject.ReadFromFile(this.ProjectPath);
            this.LocalizationMessages = AppTrackingTransparency.m_LocalizationMessages;
        }

        [PostProcessBuild]
        public static void UpdatePlistFile(BuildTarget buildTarget, string buildPath)
        {
            var englishMessage = AppTrackingTransparency.m_EnglishDefaultMessage;
            var isEnabled = englishMessage != null && englishMessage.Trim().Length != 0;
            if (!isEnabled)
            {
                return;
            }

            new ATTPostProcessor(buildPath, englishMessage).Run();
        }

        private void Run()
        {
            WriteDefaultMessageToRootInfoPlist();
            CreateLanguagesLocalizations();
        }

        private void WriteDefaultMessageToRootInfoPlist()
        {
            var plistPath = Path.Combine(this.BuildPath, ATTPostProcessor.InfoPlistFileName);

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            PlistElementDict rootDict = plist.root;
            rootDict.SetString(ATTPostProcessor.MessageKey, this.EnglishMessage);

            File.WriteAllText(plistPath, plist.WriteToString());
        }

        private void CreateLocalization(string isoCode, string message)
        {
            if (message == null || message.Trim().Length == 0)
            {
                return;
            }

            var languageDirectoryName = string.Format("{0}.lproj", isoCode);

            string languageDirectoryPath = Path.Combine(this.BuildPath, languageDirectoryName);
            Directory.CreateDirectory(languageDirectoryPath);

            string stringsPlistFilePath = Path.Combine(languageDirectoryPath, ATTPostProcessor.DefaultStringsPlistFileName);
            File.WriteAllText(stringsPlistFilePath, $"\"{ATTPostProcessor.MessageKey}\" = \"{message}\";");

            string stringsPlistContent = File.ReadAllText(stringsPlistFilePath);
            File.WriteAllText(stringsPlistFilePath, stringsPlistContent);

            string relativePath = Path.Combine(languageDirectoryName, ATTPostProcessor.DefaultStringsPlistFileName);
            this.XcodeProject.AddLocaleVariantFile(ATTPostProcessor.DefaultStringsPlistFileName, isoCode, relativePath);

            this.XcodeProject.WriteToFile(this.ProjectPath);
        }

        private void CreateLanguagesLocalizations()
        {
            CreateLocalization(ATTPostProcessor.EnglishIsoCode, this.EnglishMessage);

            foreach (var localizationMessage in this.LocalizationMessages)
            {
                var language = localizationMessage.Language;
                var popupMessage = localizationMessage.PopupMessage;
                string isoCode;

                if (language != Language.Other)
                {
                    isoCode = GetIsoCode(language);
                }
                else
                {
                    isoCode = localizationMessage.ManualIsoCode;
                }

                CreateLocalization(isoCode, popupMessage);
            }
        }

        private string GetIsoCode(Language language)
        {
            FieldInfo fieldInfo = language.GetType().GetField(language.ToString());
            IsoCodeAttribute[] attributes = fieldInfo.GetCustomAttributes(typeof(IsoCodeAttribute), false) as IsoCodeAttribute[];

            if (attributes.Length > 0)
            {
                return attributes[0].Code;
            }
            return string.Empty;
        }
    }
}
#endif

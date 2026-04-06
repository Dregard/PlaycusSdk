using System;
using System.Collections.Generic;

namespace Unity.Usercentrics
{
    [Serializable]
    public class FirstLayerStyleSettings
    {
        public UsercentricsLayout layout;
        public HeaderImageSettings headerImage;
        public TitleSettings title;
        public MessageSettings message;
        public ButtonLayout buttonLayout;
        public string backgroundColor;
        public float cornerRadius;
        public string overlayColor;
        public float overlayAlpha;

        public FirstLayerStyleSettings(UsercentricsLayout layout)
        {
            this.layout = layout;
        }

        public FirstLayerStyleSettings(UsercentricsLayout layout,
                                       HeaderImageSettings headerImage = null,
                                       TitleSettings title = null,
                                       MessageSettings message = null,
                                       ButtonLayout buttonLayout = null,
                                       string backgroundColor = "",
                                       float cornerRadius = 0,
                                       string overlayColor = "",
                                       float overlayAlpha = 1f)
        {
            this.layout = layout;
            this.headerImage = headerImage;
            this.title = title;
            this.message = message;
            this.buttonLayout = buttonLayout;
            this.backgroundColor = backgroundColor;
            this.cornerRadius = cornerRadius;
            this.overlayColor = overlayColor;
            this.overlayAlpha = overlayAlpha;
        }
    }

    [Serializable]
    public enum UsercentricsLayout
    {
        Undefined,
        Full,
        Sheet,
        PopupBottom,
        PopupCenter
    }

    [Serializable]
    public class HeaderImageSettings
    {
        public HeaderImageType imageType;
        public string imageUrl;
        public SectionAlignment alignment;
        public float height;

        public static HeaderImageSettings Undefined()
        {
            return new HeaderImageSettings(imageType: HeaderImageType.Undefined);
        }
        
        public static HeaderImageSettings Extended(string imageUrl)
        {
            return new HeaderImageSettings(imageType: HeaderImageType.Extended, imageUrl: imageUrl);
        }

        public static HeaderImageSettings Hidden()
        {
            return new HeaderImageSettings(imageType: HeaderImageType.Hidden);
        }

        public static HeaderImageSettings Custom(string imageUrl, SectionAlignment alignment, float height = 0f)
        {
            return new HeaderImageSettings(imageType: HeaderImageType.Custom, imageUrl: imageUrl, alignment: alignment, height: height);
        }

        private HeaderImageSettings(HeaderImageType imageType, string imageUrl = "")
        {
            this.imageType = imageType;
            this.imageUrl = imageUrl;
        }

        private HeaderImageSettings(HeaderImageType imageType, string imageUrl, SectionAlignment alignment, float height = 0f)
        {
            this.imageType = imageType;
            this.imageUrl = imageUrl;
            this.alignment = alignment;
            this.height = height;
        }
    }

    [Serializable]
    public enum HeaderImageType
    {
        Undefined,
        Extended,
        Custom,
        Hidden
    }

    [Serializable]
    public class TitleSettings
    {
//        public Typeface font;
        public SectionAlignment alignment;
        public float textSize;
        public string textColor;

        public TitleSettings(SectionAlignment alignment, float textSize = 0f, string textColor = "")
        {
            this.alignment = alignment;
            this.textSize = textSize;
            this.textColor = textColor;
        }
    }

    [Serializable]
    public class MessageSettings
    {
//        public Typeface font;
        public float textSize;
        public SectionAlignment alignment;
        public string textColor;
        public string linkTextColor;
        public bool underlineLink;

        public MessageSettings(float textSize, SectionAlignment alignment, string textColor, string linkTextColor, bool underlineLink)
        {
            this.textSize = textSize;
            this.alignment = alignment;
            this.textColor = textColor;
            this.linkTextColor = linkTextColor;
            this.underlineLink = underlineLink;
        }
    }

    [Serializable]
    public enum SectionAlignment
    {
        Undefined,
        Start,
        Center,
        End
    }

    [Serializable]
    public class ButtonLayout
    {
        public ButtonLayoutType type;
        public List<ButtonSettings> buttons;
        public List<ButtonSettingsRow> gridButtons;

        public static ButtonLayout Undefined()
        {
            return new ButtonLayout(type: ButtonLayoutType.Undefined, buttons: new List<ButtonSettings>());
        }
        
        public static ButtonLayout Grid(List<ButtonSettingsRow> buttons)
        {
            return new ButtonLayout(gridButtons: buttons);
        }

        public static ButtonLayout Column(List<ButtonSettings> buttons)
        {
            return new ButtonLayout(type: ButtonLayoutType.Column, buttons: buttons);
        }

        public static ButtonLayout Row(List<ButtonSettings> buttons)
        {
            return new ButtonLayout(type: ButtonLayoutType.Row, buttons: buttons);
        }

        private ButtonLayout(ButtonLayoutType type, List<ButtonSettings> buttons)
        {
            this.type = type;
            this.buttons = buttons;
        }

        private ButtonLayout(List<ButtonSettingsRow> gridButtons)
        {
            this.type = ButtonLayoutType.Grid;
            this.gridButtons = gridButtons;
        }
    }

    [Serializable]
    public enum ButtonLayoutType
    {
        Undefined,
        Column,
        Row,
        Grid
    }

    [Serializable]
    public class ButtonSettingsRow
    {
        public List<ButtonSettings> buttons;

        public ButtonSettingsRow(List<ButtonSettings> buttons)
        {
            this.buttons = buttons;
        }
    }


    [Serializable]
    public class ButtonSettings
    {
        public ButtonType type;
//        public Typeface font;
        public float textSize;
        public string textColor;
        public string backgroundColor;
        public float cornerRadius;
        public bool isAllCaps;

        public ButtonSettings(ButtonType type)
        {
            this.type = type;
        }

        public ButtonSettings(ButtonType type, float textSize = 0f, string textColor = "", string backgroundColor = "", float cornerRadius = 0, bool isAllCaps = false)
        {
            this.type = type;
            this.textSize = textSize;
            this.textColor = textColor;
            this.backgroundColor = backgroundColor;
            this.cornerRadius = cornerRadius;
            this.isAllCaps = isAllCaps;
        }
    }

    [Serializable]
    public enum ButtonType {
        AcceptAll,
        DenyAll,
        More,
        Save
    }
}
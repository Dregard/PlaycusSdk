namespace Unity.Usercentrics
{
    public enum Language
    {
        [IsoCode("ar")]
        Arabic,

        [IsoCode("ca")]
        Catalan,

        [IsoCode("zh-HK")]
        ChineseHongKong,

        [IsoCode("zh-Hans")]
        ChineseSimplified,

        [IsoCode("zh-Hant")]
        ChineseTraditional,

        [IsoCode("hr")]
        Croatian,

        [IsoCode("cs")]
        Czech,

        [IsoCode("da")]
        Danish,

        [IsoCode("nl")]
        Dutch,

        [IsoCode("en-AU")]
        EnglishAustralia,

        [IsoCode("en-IN")]
        EnglishIndia,

        [IsoCode("en-GB")]
        EnglishUnitedKingdom,

        [IsoCode("fi")]
        Finnish,

        [IsoCode("fr")]
        French,

        [IsoCode("fr-CA")]
        FrenchCanada,

        [IsoCode("de")]
        German,

        [IsoCode("el")]
        Greek,

        [IsoCode("he")]
        Hebrew,

        [IsoCode("hi")]
        Hindi,

        [IsoCode("hu")]
        Hungarian,

        [IsoCode("id")]
        Indonesian,

        [IsoCode("it")]
        Italian,

        [IsoCode("ja")]
        Japanese,

        [IsoCode("ko")]
        Korean,

        [IsoCode("ms")]
        Malay,

        [IsoCode("nb")]
        NorwegianBokmal,

        [IsoCode("pl")]
        Polish,

        [IsoCode("pt-PT")]
        PortuguesePortugal,

        [IsoCode("pt-BR")]
        PortugueseBrazil,

        [IsoCode("ro")]
        Romanian,

        [IsoCode("ru")]
        Russian,

        [IsoCode("sk")]
        Slovak,

        [IsoCode("es")]
        Spanish,

        [IsoCode("es-419")]
        SpanishLatinAmerica,

        [IsoCode("sv")]
        Swedish,

        [IsoCode("th")]
        Thai,

        [IsoCode("tr")]
        Turkish,

        [IsoCode("uk")]
        Ukrainian,

        [IsoCode("vi")]
        Vietnamese,

        [IsoCode("other")]
        Other
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class IsoCodeAttribute : System.Attribute
    {
        public string Code { get; }

        public IsoCodeAttribute(string code)
        {
            Code = code;
        }
    }
}

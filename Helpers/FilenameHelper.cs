using POIViewerMap.Resources.Strings;
using System.Globalization;

namespace POIViewerMap.Helpers;

/// <summary>
/// Class <c>FilenameHelper</c>
/// </summary>
public class FilenameHelper
{    
    /// <summary>
    /// Method <c>GetCountryCodeFromTranslatedCountry</c>.
    /// For app country picker in system language
    /// the country param identifies the country.
    /// </summary>
    /// <returns>
    /// Country in the system language for display
    /// </returns>
    public static string GetCountryCodeFromTranslatedCountry(string country)
    {
        return country switch
        {
            _ when country.Equals(AppResource.CountryAlbania) => "al",
            _ when country.Equals(AppResource.CountryAndorra) => "ad",
            _ when country.Equals(AppResource.CountryArmenia) => "am",
            _ when country.Equals(AppResource.CountryAustria) => "at",
            _ when country.Equals(AppResource.CountryBelarus) => "by",
            _ when country.Equals(AppResource.CountryBelgium) => "be",
            _ when country.Equals(AppResource.CountryBosniaHerzegovina) => "ba",
            _ when country.Equals(AppResource.CountryBulgaria) => "bg",
            _ when country.Equals(AppResource.CountryCroatia) => "hr",
            _ when country.Equals(AppResource.CountryCzechRepublic) => "cz",
            _ when country.Equals(AppResource.CountryDenmark) => "dk",
            _ when country.Equals(AppResource.CountryEstonia) => "ee",
            _ when country.Equals(AppResource.CountryFinland) => "fi",
            _ when country.Equals(AppResource.CountryFrance) => "fr",
            _ when country.Equals(AppResource.CountryGeorgia) => "ge",
            _ when country.Equals(AppResource.CountryGermany) => "de",
            _ when country.Equals(AppResource.CountryGreece) => "gr",
            _ when country.Equals(AppResource.CountryHungary) => "hu",
            _ when country.Equals(AppResource.CountryIceland) => "is",
            _ when country.Equals(AppResource.CountryIreland) => "ie",
            _ when country.Equals(AppResource.CountryIsleOfMan) => "im",
            _ when country.Equals(AppResource.CountryItaly) => "it",
            _ when country.Equals(AppResource.CountryLatvia) => "lv",
            _ when country.Equals(AppResource.CountryLiechtenstein) => "li",
            _ when country.Equals(AppResource.CountryLithuania) => "lt",
            _ when country.Equals(AppResource.CountryLuxembourg) => "lu",
            _ when country.Equals(AppResource.CountryMoldova) => "md",
            _ when country.Equals(AppResource.CountryMonaco) => "mc",
            _ when country.Equals(AppResource.CountryMontenegro) => "me",
            _ when country.Equals(AppResource.CountryNetherlands) => "nl",
            _ when country.Equals(AppResource.CountryNorthMacedonia) => "mk",
            _ when country.Equals(AppResource.CountryNorway) => "no",
            _ when country.Equals(AppResource.CountryPoland) => "pl",
            _ when country.Equals(AppResource.CountryPortugal) => "pt",
            _ when country.Equals(AppResource.CountryRomania) => "ro",
            _ when country.Equals(AppResource.CountryRussia) => "ru",
            _ when country.Equals(AppResource.CountrySerbia) => "rs",
            _ when country.Equals(AppResource.CountrySlovakia) => "sk",
            _ when country.Equals(AppResource.CountrySlovenia) => "si",
            _ when country.Equals(AppResource.CountrySpain) => "es",
            _ when country.Equals(AppResource.CountrySweden) => "se",
            _ when country.Equals(AppResource.CountrySwitzerland) => "ch",
            _ when country.Equals(AppResource.CountryTurkey) => "tr",
            _ when country.Equals(AppResource.CountryUkraine) => "ua",
            _ when country.Equals(AppResource.CountryUnitedKingdom) => "uk",
            _ => string.Empty,
        };        
    }
}

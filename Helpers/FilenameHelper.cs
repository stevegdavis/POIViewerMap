namespace POIViewerMap.Helpers;

/// <summary>
/// Class <c>FilenameHelper</c>
/// </summary>
public class FilenameHelper
{
    /// <summary>
    /// Method <c>GetCountryName</c> Returns country name from 2 letter country code.
    /// Used by the app country picker.
    /// </summary>
    public static string GetCountryNameFromCountryCode(string countrycode)
    {
        return countrycode switch
        {
            "al" => "Albania",
            "ad" => "Andorra",
            "am" => "Armenia",
            "at" => "Austria",
            "by" => "Belarus",
            "be" => "Belgium",
            "ba" => "Bosnia-Herzegovina",
            "bg" => "Bulgaria",
            "hr" => "Croatia",
            "cy" => "Cyprus",
            "cz" => "Czech Republic",
            "dk" => "Denmark",
            "ee" => "Estonia",
            "fi" => "Finland",
            "fr" => "France",
            "ge" => "Georgia",
            "de" => "Germany",
            "gr" => "Greece",
            "hu" => "Hungary",
            "is" => "Iceland",
            "ie" => "Ireland and Northern Ireland",
            "im" => "Isle of Man",
            "it" => "Italy",
            "lv" => "Latvia",
            "li" => "Liechtenstein",
            "lt" => "Lithuania",
            "lu" => "Luxembourg",
            "md" => "Moldova",
            "mc" => "Monaco",
            "me" => "Montenegro",
            "nl" => "Netherlands",
            "mk" => "North Macedonia",
            "no" => "Norway",
            "pl" => "Poland",
            "pt" => "Portugal",
            "ro" => "Romania",
            "ru" => "Russia",
            "rs" => "Serbia",
            "sk" => "Slovakia",
            "si" => "Slovenia",
            "es" => "Spain",
            "se" => "Sweden",
            "ch" => "Switzerland",
            "tr" => "Turkey",
            "ua" => "Ukraine",
            "uk" => "United Kingdom",
            _ => "Unknown",
        };
    }
    /// <summary>
    /// Method <c>GetCountryCodeFromCountry</c>.
    /// For both https://download.geofabrik.de/europe.html and app country picker
    /// the country code is used to identify the country.
    /// </summary>
    /// <returns>
    /// 2 letter country code
    /// </returns>
    public static string GetCountryCodeFromCountry(string country)
    {
        return country switch
        {
            "albania" => "al",  // for geofabrik
            "Albania" => "al",  // for app country picker
            "andorra" => "ad",
            "Andorra" => "ad",
            "austria" => "at",
            "belarus" => "by",
            "Belarus" => "by",
            "belgium" => "be",
            "Belgium" => "be",
            "bosnia-herzegovina" => "ba",
            "Bosnia-Herzegovina" => "ba",
            "bulgaria" => "bg",
            "Bulgaria" => "bg",
            "croatia" => "hr",
            "Croatia" => "hr",
            "czech-republic" => "cz",
            "Czech Republic" => "cz",
            "denmark" => "dk",
            "Denmark" => "dk",
            "estonia" => "ee",
            "Estonia" => "ee",
            "finland" => "fi",
            "Finland" => "fi",
            "france" => "fr",
            "France" => "fr",
            "georgia" => "ge",
            "Georgia" => "ge",
            "germany" => "de",
            "Germany" => "de",
            "greece" => "gr",
            "Greece" => "gr",
            "hungary" => "hu",
            "Hungary" => "hu",
            "iceland" => "is",
            "Iceland" => "is",
            "ireland-and-northern-ireland" => "ie",
            "Ireland and Northern Ireland" => "ie",
            "isle-of-man" => "im",
            "Isle of Man" => "im",
            "italy" => "it",
            "Italy" => "it",
            "latvia" => "lv",
            "Latvia" => "lv",
            "liechtenstein" => "li",
            "Liechtenstein" => "li",
            "lithuania" => "lt",
            "Lithuania" => "lt",
            "luxembourg" => "lu",
            "Luxembourg" => "lu",
            "moldova" => "md",
            "Moldova" => "md",
            "monaco" => "mc",
            "Monaco" => "mc",
            "montenegro" => "me",
            "Montenegro" => "me",
            "netherlands" => "nl",
            "Netherlands" => "nl",
            "norway" => "no",
            "Norway" => "no",
            "poland" => "pl",
            "Poland" => "pl",
            "portugal" => "pt",
            "Portugal" => "pt",
            "romania" => "ro",
            "Romania" => "ro",
            "russia" => "ru",
            "Russia" => "ru",
            "serbia" => "rs",
            "Serbia" => "rs",
            "slovakia" => "sk",
            "Slovakia" => "sk",
            "slovenia" => "si",
            "Slovenia" => "si",
            "spain" => "es",
            "Spain" => "es",
            "sweden" => "se",
            "Sweden" => "se",
            "switzerland" => "ch",
            "Switzerland" => "ch",
            "turkey" => "tr",
            "Turkey" => "tr",
            "ukraine" => "ua",
            "Ukraine" => "ua",
            "united-kingdom" => "uk",
            "United Kingdom" => "uk",
            _ => "Unknown",
        };
    }    
}

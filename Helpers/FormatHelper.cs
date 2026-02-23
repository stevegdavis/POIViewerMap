using POIBinaryFormatLib;
using POIViewerMap.Resources.Strings;

namespace POIViewerMap.Helpers;
/// <summary>
/// Class <c>FormatHelper</c>
/// </summary>
public class FormatHelper
{
    /// <summary>
    /// Method <c>FormatDistance</c>.
    /// For app country picker in system language
    /// the country param identifies the country.
    /// </summary>
    /// <param name="distance">double to be formatted into a string for display</param>
    /// <returns>
    /// Formatted string
    /// </returns>
    public static string FormatDistance(double distance)
    {
        var distanceStr = $" {String.Format("{0:0.00}", distance)}km";
        if (distance < 1)
            distanceStr = $" {String.Format("{0:0}", distance * 1000)} {AppResource.Meters}";
        return distanceStr;
    }
    /// <summary>
    /// Method <c>GetSelectedIndexFromPOIType</c>
    /// </summary>
    /// <param name="poi"></param>
    /// <returns>int</returns>
    public static int GetSelectedIndexFromPOIType(POIType poi)
    {
        return poi switch
        {
            POIType.DrinkingWater => 0,
            POIType.Campsite => 1,
            POIType.BicycleShop => 2,
            POIType.BicycleRepairStation => 3,
            POIType.Supermarket => 4,
            POIType.ConvenienceStore => 5,
            POIType.ChargingStation => 6,
            POIType.ATM => 7,
            POIType.Toilet => 8,
            POIType.Cafe => 9,
            POIType.Bakery => 10,
            POIType.PicnicTable => 11,
            POIType.TrainStation => 12,
            POIType.VendingMachine => 13,
            POIType.Laundry => 14,
            _ => 0,
        };
    }
    /// <summary>
    /// <c>GetPOIType</c>
    /// </summary>
    /// <param name="poi"></param>
    /// <returns>POIType</returns>
    public static POIType GetPOIType(int poi)
    {
        return poi switch
        {
            0 => POIType.DrinkingWater,
            1 => POIType.Campsite,
            2 => POIType.BicycleShop,
            3 => POIType.BicycleRepairStation,
            4 => POIType.Supermarket,
            5 => POIType.ConvenienceStore,
            6 => POIType.ChargingStation,
            7 => POIType.ATM,
            8 => POIType.Toilet,
            9 => POIType.Cafe,
            10 => POIType.Bakery,
            11 => POIType.PicnicTable,
            12 => POIType.TrainStation,
            13 => POIType.VendingMachine,
            14 => POIType.Laundry,
            _ => POIType.DrinkingWater,
        };
    }
    /// <summary>
    /// <c>GetSelectedIndexFromRadius</c>
    /// </summary>
    /// <param name="radius"></param>
    /// <returns>Index</returns>
    public static int GetSelectedIndexFromRadius(int radius)
    {
        return radius switch
        {
            5 => 0,
            10 => 1,
            20 => 2,
            50 => 3,
            75 => 4,
            100 => 5,
            _ => 0,//5km
        };
    }
    /// <summary>
    /// <c>GetSubTitleLang</c>
    /// </summary>
    /// <param name="subtitle"></param>    
    /// <returns>Language string in a supported language</returns>
    public static string GetSubTitleLang(string subtitle)
    {
        return subtitle.Replace("Open:", AppResource.PinLabelSubtitleOpen).
            Replace("Website:", AppResource.PinLabelSubtitleWebsite).
            Replace("Refill Here", AppResource.PinLabelSubtitleRefill).
            Replace("Services:", AppResource.PinLabelSubtitleServicesText).
            Replace("Tools", AppResource.PinLabelSubtitleTools).
            Replace("Pump", AppResource.PinLabelSubtitlePump).
            Replace("Stand", AppResource.PinLabelSubtitleStand).
            Replace("Unknown", AppResource.PinLabelSubtitleUnknown).
            Replace("Type:", AppResource.PinLabelSubtitleTypeText).
            Replace("Bicycle Tubes", AppResource.PinLabelSubtitleBicycleTubeText).
            Replace("Telephone:", AppResource.PinLabelSubtitleTelephoneText).
            Replace("Access:", AppResource.PinLabelSubtitleAccessText).
            Replace("Permissive", AppResource.PinLabelSubtitlePermissiveText).
            Replace("Private", AppResource.PinLabelSubtitlePrivateText).
            Replace("Diet:", AppResource.PinLabelSubtitleDietText).
            Replace("Vegan Only", AppResource.PinLabelSubtitleVeganOnlyText).
            Replace("Vegan Option", AppResource.PinLabelSubtitleVeganOptionText).
            Replace("Socket:", AppResource.PinLabelSubtitleSocketText).
            Replace("Lockable:", AppResource.PinLabelSubtitleLockableText).
            Replace("Yes", AppResource.PinLabelYes).
            Replace("No", AppResource.PinLabelNo).
            Replace("Monday", AppResource.PinLabelMo).
            Replace("Tuesday", AppResource.PinLabelTu).
            Replace("Wednesday", AppResource.PinLabelWe).
            Replace("Thursday", AppResource.PinLabelTh).
            Replace("Friday", AppResource.PinLabelFr).
            Replace("Saturday", AppResource.PinLabelSa).
            Replace("Sunday", AppResource.PinLabelSu).
            Replace("Closed", AppResource.PinLabelOff).
            Replace("Customers", AppResource.PinLabelSubtitleCustomersText);
    }
    public static int GetRadiusType(int selectedIndex)
    {
        return selectedIndex switch
        {
            0 => 5,// Km
            1 => 10,
            2 => 20,
            3 => 50,
            4 => 75,
            5 => 100,
            _ => 5,
        };
    }
    /// <summary>
    /// <c>TranslatedCountryName</c>
    /// </summary>
    /// <param name="countrycode"></param>
    /// <returns>Translated country name from 2 letter country code</returns>
    public static string TranslateCountryName(string countrycode)
    {
        return countrycode switch
        {
            "ad" => AppResource.CountryAndorra,
            "am" => AppResource.CountryArmenia,
            "al" => AppResource.CountryAlbania,
            "at" => AppResource.CountryAustria,
            "by" => AppResource.CountryBelarus,
            "be" => AppResource.CountryBelgium,
            "ba" => AppResource.CountryBosniaHerzegovina,
            "bg" => AppResource.CountryBulgaria,
            "hr" => AppResource.CountryCroatia,
            "cz" => AppResource.CountryCzechRepublic,
            "dk" => AppResource.CountryDenmark,
            "ee" => AppResource.CountryEstonia,
            "fi" => AppResource.CountryFinland,
            "fr" => AppResource.CountryFrance,
            "ge" => AppResource.CountryGeorgia,
            "de" => AppResource.CountryGermany,
            "gr" => AppResource.CountryGreece,
            "hu" => AppResource.CountryHungary,
            "is" => AppResource.CountryIceland,
            "ie" => AppResource.CountryIreland,
            "im" => AppResource.CountryIsleOfMan,
            "it" => AppResource.CountryItaly,
            "lv" => AppResource.CountryLatvia,
            "li" => AppResource.CountryLiechtenstein,
            "lt" => AppResource.CountryLithuania,
            "lu" => AppResource.CountryLuxembourg,
            "md" => AppResource.CountryMoldova,
            "mc" => AppResource.CountryMonaco,
            "me" => AppResource.CountryMontenegro,
            "nl" => AppResource.CountryNetherlands,
            "mk" => AppResource.CountryNorthMacedonia,
            "no" => AppResource.CountryNorway,
            "pl" => AppResource.CountryPoland,
            "pt" => AppResource.CountryPortugal,
            "ro" => AppResource.CountryRomania,
            "ru" => AppResource.CountryRussia,
            "rs" => AppResource.CountrySerbia,
            "sk" => AppResource.CountrySlovakia,
            "si" => AppResource.CountrySlovenia,
            "es" => AppResource.CountrySpain,
            "se" => AppResource.CountrySweden,
            "ch" => AppResource.CountrySwitzerland,
            "tr" => AppResource.CountryTurkey,
            "ua" => AppResource.CountryUkraine,
            "uk" => AppResource.CountryUnitedKingdom,
            _ => string.Empty,
        };
    }
    /// <summary>
    /// Method <c>GetCountryCodeFromReverseGeocode ID</c>.
    /// Country code is used to identify the country.
    /// </summary>
    /// <returns>
    /// 2 letter country code
    /// </returns>
    public static string GetCountryCodeFromReverseGeocode(string geocode)
    {
        return geocode switch
        {
            "ALB" => "al",  // for geofabrik download
            "AND" => "ad",
            "AUT" => "at",
            "BLR" => "by",
            "BEL" => "be",
            "BIH" => "ba",
            "BGR" => "bg",
            "HRV" => "hr",
            "CZE" => "cz",
            "DNK" => "dk",
            "EST" => "ee",
            "FIN" => "fi",
            "FRA" => "fr",
            "GEO" => "ge",
            "DEU" => "de",
            "GRC" => "gr",
            "HUN" => "hu",
            "ISL" => "is",
            "IRL" => "ie",
            "isle-of-man" => "im",
            "ITA" => "it",
            "LVA" => "lv",
            "LTU" => "lt",
            "LUX" => "lu",
            "LIE" => "li",
            "MDA" => "md",
            "monaco" => "mc",
            "MNE" => "me",
            "NLD" => "nl",
            "NOR" => "no",
            "POL" => "pl",
            "PRT" => "pt",
            "ROU" => "ro",
            "RUS" => "ru",
            "SRB" => "rs",
            "SVK" => "sk",
            "SVN" => "si",
            "ESP" => "es",
            "SWE" => "se",
            "CHE" => "ch",
            "TUR" => "tr",
            "UKR" => "ua",
            "GBR" => "uk",
            _ => "Unknown",
        };
    }    
    public static string GetEmbeddedResourceForPOI(POIType type)
    {
        // Map POI types to embedded SVG resource IDs in Resources/Images
        return type switch
        {
            POIType.DrinkingWater => "embedded://POIViewerMap.Resources.Images.water-drop.svg",
            POIType.Campsite => "embedded://POIViewerMap.Resources.Images.campsite.svg",
            POIType.BicycleShop => "embedded://POIViewerMap.Resources.Images.bicycle.svg",
            POIType.BicycleRepairStation => "embedded://POIViewerMap.Resources.Images.bicyclerepairstation.svg",
            POIType.Supermarket => "embedded://POIViewerMap.Resources.Images.supermarket.svg",
            POIType.ConvenienceStore => "embedded://POIViewerMap.Resources.Images.shopping-basket.svg",
            POIType.ChargingStation => "embedded://POIViewerMap.Resources.Images.charging-station.svg",
            POIType.ATM => "embedded://POIViewerMap.Resources.Images.atm.svg",
            POIType.Toilet => "embedded://POIViewerMap.Resources.Images.toilet.svg",
            POIType.Cafe => "embedded://POIViewerMap.Resources.Images.cafe.svg",
            POIType.Bakery => "embedded://POIViewerMap.Resources.Images.bakery.svg",
            POIType.PicnicTable => "embedded://POIViewerMap.Resources.Images.picnic-table.svg",
            POIType.TrainStation => "embedded://POIViewerMap.Resources.Images.train.svg",
            POIType.VendingMachine => "embedded://POIViewerMap.Resources.Images.vending-machine.svg",
            POIType.Laundry => "embedded://POIViewerMap.Resources.Images.laundry.svg",
            _ => string.Empty,
        };
    }
}

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
            POIType.ATM => 5,
            POIType.Toilet => 6,
            POIType.Cafe => 7,
            POIType.Bakery => 8,
            POIType.PicnicTable => 9,
            POIType.TrainStation => 10,
            POIType.VendingMachine => 11,
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
            5 => POIType.ATM,
            6 => POIType.Toilet,
            7 => POIType.Cafe,
            8 => POIType.Bakery,
            9 => POIType.PicnicTable,
            10 => POIType.TrainStation,
            11 => POIType.VendingMachine,
            12 => POIType.Laundry,
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
            Replace("Vegan Option", AppResource.PinLabelSubtitleVeganOptionText);
    }
    /// <summary>
    /// <c>GetTitleLang</c>
    /// </summary>
    /// <param name="data">Data type</param>
    /// <param name="v"></param>
    /// <returns>String for display</returns>
    public static string GetTitleLang(POIData data, bool v)
    {
        var Title = string.Empty;
        switch (data.POI)
        {
            case POIType.DrinkingWater:
                Title = AppResource.OptionsPOIPickerDrinkingWaterText;
                break;
            case POIType.Campsite:
                Title = AppResource.OptionsPOIPickerCampsiteText;
                break;
            case POIType.BicycleShop:
                Title = AppResource.OptionsPOIPickerBicycleShopText;
                break;
            case POIType.BicycleRepairStation:
                Title = AppResource.OptionsPOIPickerBicycleRepairStationText;
                break;
            case POIType.Supermarket:
                Title = AppResource.OptionsPOIPickerSupermarketText;
                break; ;
            case POIType.ATM:
                Title = AppResource.OptionsPOIPickerATMText;
                break;
            case POIType.Toilet:
                Title = AppResource.OptionsPOIPickerToiletText;
                break;
            case POIType.Cafe:
                Title = AppResource.OptionsPOIPickerCafeText;
                break;
            case POIType.Bakery:
                Title = AppResource.OptionsPOIPickerBakeryText;
                break;
            case POIType.PicnicTable:
                Title = AppResource.OptionsPOIPickerPicnicTableText;
                break;
            case POIType.TrainStation:
                Title = AppResource.OptionsPOIPickerTrainStationText;
                break;
            case POIType.VendingMachine:
                Title = AppResource.OptionsPOIPickerVendingMachineText;
                break;
            case POIType.Laundry:
                Title = AppResource.OptionsPOIPickerLaundryText;
                break;
            default:
                Title = string.Empty;
                break;
        }
        return Title += v ? data.Title[data.Title.IndexOf(":")..] : string.Empty;
    }
    /// <summary>
    /// <c>GetRadiusType</c>
    /// </summary>
    /// <param name="selectedIndex"></param>
    /// <returns>Index</returns>
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
}

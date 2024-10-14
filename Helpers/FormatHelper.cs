using POIBinaryFormatLib;
using POIViewerMap.Resources.Strings;

namespace POIViewerMap.Helpers;

public class FormatHelper
{
    public static string FormatDistance(double distance)
    {
        var distanceStr = $" {String.Format("{0:0.00}", distance)}km";
        if (distance < 1)
            distanceStr = $" {String.Format("{0:0}", distance * 1000)} {AppResource.Meters}";
        return distanceStr;
    }
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
    public static string GetSubTitleLang(string subtitle)
    {
        return subtitle.Replace("Open:", AppResource.PinLabelSubtitleOpen).
            Replace("Website:", AppResource.PinLabelSubtitleWebsite).
            Replace("Refill Here", AppResource.PinLabelSubtitleRefill).
            Replace("Services:", AppResource.PinLabelSubtitleServices).
            Replace("Tools", AppResource.PinLabelSubtitleTools).
            Replace("Pump", AppResource.PinLabelSubtitlePump).
            Replace("Unknown", AppResource.PinLabelSubtitleUnknown).
            Replace("Type:", AppResource.PinLabelSubtitleTypeText).
            Replace("Bicycle tubes", AppResource.PinLabelSubtitleBicycleTubeText);
    }
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
}

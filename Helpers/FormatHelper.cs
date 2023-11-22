using POIBinaryFormatLib;
using POIViewerMap.Resources.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public static string GetTitleLang(POIType type)
    {
        var Title = string.Empty;
        switch (type)
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
            default:
                Title = string.Empty;
                break;
        }
        return Title;
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
    public static string GetSubTitleLang(POIData poi)
    {
        var subtitle = string.Empty;
        if (poi.Subtitle.Contains("Website:"))
        {
            subtitle = $"{AppResource.PinLabelSubtitleWebsite} {poi.Subtitle.Substring(poi.Subtitle.IndexOf(":") + 2)}";
        }
        else if (poi.Subtitle.Contains("Refill Here"))
        {
            subtitle = $"{AppResource.PinLabelSubtitleRefill}";
        }
        if (poi.Subtitle.Contains("Services"))
        {
            subtitle = $"{AppResource.PinLabelSubtitleServices} ";
            if (poi.Subtitle.Contains("Tools"))
                subtitle = $"{subtitle}{AppResource.PinLabelSubtitleTools}";
            if (poi.Subtitle.Contains("Pump"))
            {
                subtitle = $"{subtitle}{(poi.Subtitle.Contains("Tools") ? "," : string.Empty)}{AppResource.PinLabelSubtitlePump}";
            }
            if (poi.Subtitle.Contains("Open"))
            {
                subtitle = $"{subtitle}\r{AppResource.PinLabelSubtitleOpen} {poi.Subtitle[(poi.Subtitle.LastIndexOf(":") + 2)..]}";
            }
            if (poi.Subtitle.Contains("Unknown"))
            {
                subtitle = $"{subtitle}{AppResource.PinLabelSubtitleUnknown}";
            }
        }
        else if (poi.Subtitle.Contains("Open:"))
        {
            subtitle = $"{AppResource.PinLabelSubtitleOpen} {poi.Subtitle.Substring(poi.Subtitle.IndexOf(":") + 2)}";
        }
        return subtitle;
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

using POIBinaryFormatLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace POIViewerMap.Helpers;

internal class AppIconHelper
{
    public static string drinkingwaterStr = null;
    public static string campsiteStr = null;
    public static string bicycleshopStr = null;
    public static string supermarketStr = null;
    public static string bicyclerepairstationStr = null;
    public static string atmStr = null;
    public static string toiletStr = null;
    public static string cupStr = null;
    public static string bakeryStr = null;
    public static string picnictableStr = null;
    public static string trainstationStr = null;
    public static string vendingmachineStr = null;
    internal static void InitializeIcons()
    {
        var assembly = typeof(App).GetTypeInfo().Assembly;
        var assemblyName = assembly.GetName().Name;
        using var drinkingwater = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.waterlightblue.svg");
        if (drinkingwater != null)
        {
            using StreamReader reader = new(drinkingwater!);
            drinkingwaterStr = reader.ReadToEnd();
        }
        using var campsite = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.camping.svg");
        if (campsite != null)
        {
            using StreamReader reader = new(campsite!);
            campsiteStr = reader.ReadToEnd();
        }
        using var bicycleshop = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.bicycle.svg");
        if (bicycleshop != null)
        {
            using StreamReader reader = new(bicycleshop!);
            bicycleshopStr = reader.ReadToEnd();
        }
        using var bicyclerepairstation = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.spanner.svg");
        if (bicyclerepairstation != null)
        {
            using StreamReader reader = new(bicyclerepairstation!);
            bicyclerepairstationStr = reader.ReadToEnd();
        }
        using var supermarket = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.shopping-cart.svg");
        if (supermarket != null)
        {
            using StreamReader reader = new(supermarket!);
            supermarketStr = reader.ReadToEnd();
        }
        using var atm = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.atm.svg");
        if (atm != null)
        {
            using StreamReader reader = new(atm!);
            atmStr = reader.ReadToEnd();
        }
        using var toilet = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.toilet.svg");
        if (toilet != null)
        {
            using StreamReader reader = new(toilet!);
            toiletStr = reader.ReadToEnd();
        }
        using var cup = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.coffee-cup.svg");
        if (cup != null)
        {
            using StreamReader reader = new(cup!);
            cupStr = reader.ReadToEnd();
        }
        using var bakery = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.cupcake.svg");
        if (bakery != null)
        {
            using StreamReader reader = new(bakery!);
            bakeryStr = reader.ReadToEnd();
        }
        using var picnictable = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.picnic-table.svg");
        if (picnictable != null)
        {
            using StreamReader reader = new(picnictable!);
            picnictableStr = reader.ReadToEnd();
        }
        using var trainstation = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.train.svg");
        if (trainstation != null)
        {
            using StreamReader reader = new(trainstation!);
            trainstationStr = reader.ReadToEnd();
        }
        using var vendingmachine = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.vending-machine.svg");
        if (vendingmachine != null)
        {
            using StreamReader reader = new(vendingmachine!);
            vendingmachineStr = reader.ReadToEnd();
        }
    }
    internal static string GetPOIIcon(POIData poi)
    {
        switch (poi.POI)
        {
            case POIType.DrinkingWater: return drinkingwaterStr;
            case POIType.Campsite: return campsiteStr;
            case POIType.BicycleShop: return bicycleshopStr;
            case POIType.BicycleRepairStation: return bicyclerepairstationStr;
            case POIType.Supermarket: return supermarketStr;
            case POIType.ATM: return atmStr;
            case POIType.Toilet: return toiletStr;
            case POIType.Cafe: return cupStr;
            case POIType.Bakery: return bakeryStr;
            case POIType.PicnicTable: return picnictableStr;
            case POIType.TrainStation: return trainstationStr;
            case POIType.VendingMachine: return vendingmachineStr;
            case POIType.Unknown:
                break;
        }
        return string.Empty;
    }
}

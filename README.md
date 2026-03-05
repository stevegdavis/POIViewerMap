# POIViewerMap  
Point Of Interest Viewer Map is a .NET MAUI app for Android devices (iOS work in progress). It allows the user to choose a POI from a fixed list and display them on a map within a search radius from the users current location. 
POI's are oriented towards bicycle touring and hiking.  
The app also supports import of a GPX route for displaying on the map.  

**Map**  
Open Street Map - Online only  

**Options Panel**  
* Import POI's for Country from remote server
* POI (To View)
* Search Radius (Kilometers - distances shown are in a straight line)
* POI's Found (Visible on map)
* Import GPX Route
* Route (Imported GPX file name)
* Center Map On My Position (Uses more battery power)

 **Countries Covered**
* Albania
* Andorra
* Austria
* Belarus
* Belgium
* Bosnia Herzegovina
* Bulgaia  
* Croatia
* Czech Republic
* Denmark
* Estonia
* Finland
* France
* Georgia
* Germany
* Greece 
* Hungary  
* Iceland
* Ireland  
* Italy
* Latvia
* Liechenstien
* Lithuania  
* Luxembourg  
* Moldova
* Netherlands
* Norway
* Poland
* Portugal
* Romainia  
* Serbia
* Slovakia
* Slovenia
* Spain
* Sweden
* Switzerland
* Turkey
* Ukraine
* United Kingdom

**POI's Supported**   
* <img src="Resources/Images/waterdrop.svg" align="center" width="20" height="20"/> Drinking Water Point  
* <img src="Resources/Images/campsite.svg" align="center" width="20"/> Campsite  
* <img src="Resources/Images/bicycle.svg" align="center" width="20"/> Bicycle Shop  
* <img src="Resources/Images/spanner.svg" align="center" width="20"/> Bicycle Repair Station  
* <img src="Resources/Images/supermarket.svg" align="center" width="20"/> Supermarket
* <img src="Resources/Images/shoppingbasket.svg" align="center" width="20"/> Convenience Store
* <img src="Resources/Images/chargingstation.svg" align="center" width="20"/> Charging Station (Mobile only)  
* <img src="Resources/Images/atm.svg" align="center" width="20"/> ATM (Cash Machine)  
* <img src="Resources/Images/toilet.svg" align="center" width="20"/> Toilet  
* <img src="Resources/Images/cafe.svg" align="center" width="20"/> Cafe  
* <img src="Resources/Images/bakery.svg" align="center" width="20"/> Bakery  
* <img src="Resources/Images/picnictable.svg" align="center" width="20"/> Picnic Table  
* <img src="Resources/Images/train.svg" align="center" width="20"/> Train Station (Excluding Tourist, Metro, Subway and Tram)  
* <img src="Resources/Images/vendingmachine.svg" align="center" width="20"/> Vending Machine (Bread, Bicycle Tubes and Electronics only)
* <img src="Resources/Images/laundry.svg" align="center" width="20"/> Laundry  

**Search Radiuses Supported**  
* 1Km
* 5km  
* 10km  
* 20km

## App Languages 
* English
* French (Français)
* German (Deutsch)
* Italian (Italiano)
* Polish (Polski) 
* Netherlands (Nederlands)

## App Language Translations
https://hnd.www.deepl.com/  

## Screenshots  
**Android**  
<img src="Screenshots/Android/OptionsPanel.jpg" width="200" />  <img src="Screenshots/Android/DrinkingWaterPoint.jpg" width="200" />  <img src="Screenshots/Android/BakeryWithOptionsPanel.jpg" width="200" /> <img src="Screenshots/Android/POIsPicker.jpg" width="200" />  

## OS Minimum Requirments  
* Android 8
* iOS (TBC)
## Libraries Used  
* ReactiveUI https://www.reactiveui.net/  
* Flurl.Http https://flurl.dev/   
* RolandK.Formats.Gpx https://github.com/RolandKoenig/RolandK.Formats.Gpx  
* Mapsui https://github.com/Mapsui/Mapsui 
* protobuf-net https://github.com/protobuf-net/protobuf-net
* Syncfusion.Maui.Toolkit https://www.syncfusion.com/net-maui-toolkit
* POIBinaryFormatLib - used to deserialize POI binary objects from the country files https://www.nuget.org/packages/POIBinaryFormatLib/ 
* ReverseGeocodeLib - Used to detect the country at map center 
## OSM Conversion Software Used
osmium https://osmcode.org/osmium-tool/
## Open Street Map Data
https://download.geofabrik.de/
## Road Map  
* Work on iOS devices

## Licence
Copyright © 2025 SDSDevelopment

> This program is free software: you can redistribute it and/or modify
> it under the terms of the GNU General Public License as published by
> the Free Software Foundation, either version 3 of the License, or
> (at your option) any later version.
> 
> This program is distributed in the hope that it will be useful,
> but WITHOUT ANY WARRANTY; without even the implied warranty of
> MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
> GNU General Public License for more details.
> 
> You should have received a copy of the GNU General Public License
> along with this program.  If not, see <https://www.gnu.org/licenses/>.
> 
> The author of this app cannot guarantee the accuracy of any POI that appears on the map. Discretion is required at all times whilst using the app.
 The data is extracted from [OpenStreetMap](https://www.openstreetmap.org), [ODbl](https://opendatacommons.org/licenses/odbl/) and relies on the many OSM contributors. Particular attention should be taken in regards to Drinking Water Points as there is no guarantee that any shown on the map are fit for drinking and the user must seek local knowledge.

Icons from: https://www.svgrepo.com/

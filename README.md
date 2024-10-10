# POIViewerMap  
Point Of Interest Viewer Map is a .NET MAUI app for Android devices (iOS work in progress). It allows the user to choose a POI from a fixed list and display them on a map. 
It is orientated towards cycling and hiking.  

**Options:**  
* Import POIs for Country
* POI (To View)
* Search Radius (Kilometers)
* POIs Found (Visible On Map)
* Import GPX Route
* Route (Name If Imported)
* Center Map On My Position (Option)

 **Countries Supported:**
* Austria  
* Belguim  
* Bosnia-Herzegovina  
* Bulgaia  
* Croatia  
* Czech Republic  
* Estonia  
* France  
* Hungary  
* Iceland  
* Ireland and NI  
* Latvia  
* Lithuania  
* Luxembourg  
* Montenegro  
* Netherlands  
* Romainia  
* Serbia  
* Slovenia  
* Switzerland  
* UK  

**POIs Supported:**   
* <img src="Resources/Images/waterlightblue.svg" align="center" width="20" height="20"/> Drinking Water Point  
* <img src="Resources/Images/campsite.svg" align="center" width="20"/> Campsite  
* <img src="Resources/Images/bicycle.svg" align="center" width="20"/> Bicycle Shop  
* <img src="Resources/Images/spanner.svg" align="center" width="20"/> Bicycle Repair Station  
* <img src="Resources/Images/shopping-basket.svg" align="center" width="20"/> Supermarket  
* <img src="Resources/Images/atm.svg" align="center" width="20"/> ATM (Cash Machine)  
* <img src="Resources/Images/toilet.svg" align="center" width="20"/> Toilet  
* <img src="Resources/Images/coffee-cup.svg" align="center" width="20"/> Cafe  
* <img src="Resources/Images/cupcake.svg" align="center" width="20"/> Bakery  
* <img src="Resources/Images/picnic-table.svg" align="center" width="20"/> Picnic Table  
* <img src="Resources/Images/train.svg" align="center" width="20"/> Train Station  
* <img src="Resources/Images/vending-machine.svg" align="center" width="20"/> Vending Machine (Bread, Bicycle Tubes and Electronics only)  

**Search Radius Supported:**  
* 5km  
* 10km  
* 20km  
* 75km  
* 100km  
## App Languages (Translations by DeepL)
* English
* French (Français)
* German (Deutsch)
* Polish (Polski)
* Italian (Italiano)
* Latvian (Latviešu)
## Screenshots  
**Andriod**  
<img src="Screenshots/Android/OptionsPanel.jpg" width="200" />  <img src="Screenshots/Android/DrinkingWaterPoint.jpg" width="200" />  <img src="Screenshots/Android/BakeryWithOptionsPanel.jpg" width="200" />  

## Minimum Requirments  
* Android 8
* iOS (Work in progress)
## Libraries Used  
* ReactiveUI https://www.reactiveui.net/  
* Flurl.Http (v3.2.4 only)  https://flurl.dev/   
* RolandK.Formats.Gpx https://github.com/RolandKoenig/RolandK.Formats.Gpx  
* Mapsui (v4.1.1 only) https://github.com/Mapsui/Mapsui
* POIBinaryFormatLib - used to deserialize POI binary objects https://www.nuget.org/packages/POIBinaryFormatLib/  
## Licence
Copyright © 2024 SDSDevelopment

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
 The data is extracted from [OpenStreetMap](https://www.openstreetmap.org) and relies on the many OSM contributors. Particular attention should be taken in regards to Drinking Water Points as there is no guarantee that any shown on the map are fit for drinking and the user must seek local knowledge.

Icons from: https://www.svgrepo.com/

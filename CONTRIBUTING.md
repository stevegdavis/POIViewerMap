<!-- Improved repository description for better SEO -->

# POIViewerMap - Point of Interest Viewer for Bicycle Touring & Hiking

An open-source .NET MAUI cross-platform mobile app for discovering and navigating to points of interest across Europe. Perfect for bicycle touring, hiking, and outdoor adventures.

## 🌟 Features at a Glance

- 🗺️ **Interactive Mapping** - Powered by OpenStreetMap
- 🚴 **Cyclist & Hiker Friendly** - 15+ POI categories including water points, campsites, bike shops
- 🌍 **37+ European Countries** - Comprehensive coverage from UK to Ukraine
- 📍 **GPS Navigation** - Real-time location tracking and route planning
- 🛣️ **GPX Route Support** - Import and display your custom routes
- 🌐 **Multi-Language** - English, French, German, Italian, Polish, Dutch
- 📱 **Cross-Platform** - Native Android & iOS apps from single codebase

## 📋 Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Quick Start](#quick-start)
- [System Requirements](#system-requirements)
- [Technologies](#technologies)
- [Contributing](#contributing)
- [License](#license)

## Overview

POIViewerMap is designed for outdoor enthusiasts who need reliable access to essential services and amenities while traveling through European countries. Whether you're on a long-distance bicycle tour or hiking expedition, this app helps you find drinking water, campsites, repair shops, restaurants, and other critical points of interest.

## Key Features

### Comprehensive POI Categories
- Drinking Water Points
- Campsites & Accommodation
- Bicycle Shops & Repair Stations
- Supermarkets & Convenience Stores
- Restaurants, Cafes & Bakeries
- ATMs & Financial Services
- Restrooms & Hygiene Facilities
- Charging Stations
- Train Stations
- Laundry Services
- Picnic Areas
- Vending Machines

### Geographic Coverage
Austria, Belgium, Bulgaria, Croatia, Czech Republic, Denmark, Estonia, Finland, France, Georgia, Germany, Greece, Hungary, Iceland, Ireland, Italy, Latvia, Liechtenstein, Lithuania, Luxembourg, Moldova, Netherlands, Norway, Poland, Portugal, Romania, Serbia, Slovakia, Slovenia, Spain, Sweden, Switzerland, Turkey, Ukraine, United Kingdom, and more.

### Mobile Features
- Search by radius: 1km, 5km, 10km, 30km
- Center map on your GPS location
- Filter POIs by category and country
- Import and display GPX routes
- Responsive design for all screen sizes

## Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/stevegdavis/POIViewerMap.git
   ```

2. **Open in Visual Studio**
   - Visual Studio 2022 or later
   - .NET MAUI workload installed

3. **Build and Run**
   ```bash
   dotnet build
   dotnet maui run -f android
   dotnet maui run -f ios
   ```

## System Requirements

| Platform | Minimum Version |
|----------|-----------------|
| Android | 8.0 (API 26) |
| iOS | 15.0 |

**Development Requirements:**
- Visual Studio 2022 Community Edition or higher
- .NET 8 SDK or later
- Android SDK (for Android development)
- Xcode (for iOS development on macOS)

## Technologies

### Framework & Core
- **.NET MAUI** - Cross-platform mobile framework
- **C#** - Primary language
- **ReactiveUI** - MVVM and reactive programming patterns

### Mapping & Location
- **Mapsui** - Interactive mapping library
- **OpenStreetMap** - Map data provider
- **ReverseGeocodeLib** - Country/region detection

### Data & Serialization
- **protobuf-net** - Efficient data serialization
- **POIBinaryFormatLib** - POI data processing
- **RolandK.Formats.Gpx** - GPX route parsing

### UI & Networking
- **Syncfusion.Maui.Toolkit** - Professional UI controls
- **Flurl.Http** - Fluent HTTP client

### Data Sources
- OpenStreetMap (ODbL License)
- Geofabrik OSM Downloads
- Osmium Tool for data processing

## Contributing

We welcome contributions! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/YourFeature`)
3. Commit changes (`git commit -m 'Add YourFeature'`)
4. Push to branch (`git push origin feature/YourFeature`)
5. Open a Pull Request

### Development Guidelines
- Follow C# coding conventions
- Add unit tests for new features
- Update README.md for significant changes
- Use meaningful commit messages

## Roadmap

- [ ] Offline map caching
- [ ] Custom POI categories
- [ ] Community-contributed POIs
- [ ] Route difficulty ratings
- [ ] Weather integration
- [ ] Multi-day trip planning

## Issues & Bug Reports

Found a bug? Please [open an issue](https://github.com/stevegdavis/POIViewerMap/issues) with:
- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Device/OS information

## License

**GNU General Public License v3.0**

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

See [LICENSE](./LICENSE) file for details.

### Data Attribution
Map data © [OpenStreetMap](https://openstreetmap.org) contributors, licensed under [ODbL](https://opendatacommons.org/licenses/odbl/).

### Disclaimer
The author cannot guarantee the accuracy of any POI displayed in this app. Always verify information independently while traveling.

---

**Built with ❤️ for outdoor adventurers worldwide**

[GitHub](https://github.com/stevegdavis/POIViewerMap) • [Issues](https://github.com/stevegdavis/POIViewerMap/issues) • [Discussions](https://github.com/stevegdavis/POIViewerMap/discussions)

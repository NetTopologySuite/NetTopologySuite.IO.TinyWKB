# NetTopologySuite.IO.TinyWKB
An IO module for NTS to read and write geometries in the [Tiny Well-Known-Binary](https://github.com/TWKB/Specification/blob/master/twkb.md) format.

| License | GitHub Actions | NuGet |
| ------- | ------ | ----- |
| [![License](https://img.shields.io/github/license/NetTopologySuite/NetTopologySuite.IO.TinyWKB.svg)](https://github.com/NetTopologySuite/NetTopologySuite.IO.TinyWKB/blob/master/LICENSE.md) | [![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2FNetTopologySuite%2FNetTopologySuite.IO.TinyWKB%2Fbadge&style=flat)](https://actions-badge.atrox.dev/NetTopologySuite/NetTopologySuite.IO.TinyWKB/goto) | [![NuGet](https://img.shields.io/nuget/v/NetTopologySuite.IO.TinyWKB.svg)](https://www.nuget.org/packages/NetTopologySuite.IO.TinyWKB/) |

## Usage

### Reading
Read a geometry like this. A `TinyWkbReader` is reusable.

``` csharp
/*
 bytes ... Array of bytes (byte[]) with twkb data from somewhere.
           You can also pass a Stream or a BinaryReader
 */
var geometryReader = new TinyWkbReader();
var geometry = geometryReader.Read(bytes);
```
#### Reading id-lists
For `MULTI`-geometries or `GEOMETRYCOLLECTION`s the TWKB specification supports 
storing an identifier (`System.Int64`) for each contained geometry. There are 3 
possible ways to obtain or handle these:
* The identifier is stored in the `Geometry.UserData` object (default).
* The caller subscribes to the `TinyWkbReader.IdentifiersProvided` event.   
The event's arguments provide access to the geometry that has been read along with the list of identifiers.
* Call the `TinyWkbReader.Read(byte[], out IList<long> idList)` overload.
```C#
var geometryReader = new TinyWkbReader();
var geometry = geometryReader.Read(bytes, out var idList);
```


### Writing
Write geometries like this. A `TinyWkbWriter` is reusable.

``` csharp
/*
 Create the writer. All constructor arguments are optional, the
 default values are displayed.
 */
var geometryWriter = new TinyWkbWriter(
    precisionXY: 7,    // Number of decimal places for x- and y-ordinate values.
    emitZ: true,       // Emit z-ordinate values if geometry has them
    precisionZ: 7,     // number of decimal digits for z-ordinates
    emitM: true,       // Emit m-ordinate values if geometry has them
    precisionM: 7,     // number of decimal digits for m-ordinates
    emitSize: false,   // Emit the size of the geometry definition 
    emitBoundingBox: true, 
                       // Emit the bounding box of the geometry for all dimensions 
    emitIdList: false  // Emit a list of identifiers, one for every geometry in a
                       //   MULTI-geometry or GEOMETRYCOLLECTION
);

/*
 geometry ... Geometry from somewhere.

 There are overloads for Write that take a Stream or
 BinaryWriter as argument.
 */
var bytes = geometryWriter.Write(geometry);
```
#### Writing id-lists
As noted in _[Reading id-lists]_ TWKB has the concept of identifiers for `MULTI`-geometries or 
`GEOMETRYCOLLECTION`s. Writing identifiers is supported analogous to the reading mechanism:
* The identifier is taken from `Geometry.UserData`. If that does not work, a new `System.Int64` 
value is created and used.
* The caller subscribes to the `TinyWkbWriter.IdentifiersRequested` event, and fills
adds the identifiers to the event's arguments.
* Call one of the `TinyWkbWriter.Write` overloads that takes an `IList<System.Int64> idList` argument.


# NetTopologySuite.IO.TinyWKB
An IO module for NTS to read and write geometries in the [Tiny Well-Known-Binary](https://github.com/TWKB/Specification/blob/master/twkb.md) format.

| License | Travis | NuGet | MyGet (pre-release) |
| ------- | ------ | ----- | ------------------- |
| [![License](https://img.shields.io/github/license/NetTopologySuite/NetTopologySuite.IO.TinyWKB.svg)](https://github.com/NetTopologySuite/NetTopologySuite.IO.TinyWKB/blob/master/LICENSE.md) | [![Travis](https://travis-ci.org/NetTopologySuite/NetTopologySuite.IO.TinyWKB.svg?branch=master)](https://travis-ci.org/NetTopologySuite/NetTopologySuite.IO.TinyWKB) | [![NuGet](https://img.shields.io/nuget/v/NetTopologySuite.IO.TinyWKB.svg)](https://www.nuget.org/packages/NetTopologySuite.IO.TinyWKB/) | [![MyGet](https://img.shields.io/myget/nettopologysuite/vpre/NetTopologySuite.IO.TinyWKB.svg?style=flat)](https://myget.org/feed/nettopologysuite/package/nuget/NetTopologySuite.IO.TinyWKB) |

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

### Writing
Write geometries like this. A `TinyWkbWriter` is reusable.

``` csharp
/*
 Create the writer. All constructor arguments are optional, the
 default values are displayed.
 */
var geometryWriter = new TinyWkbWriter(
    precisionXY: 7     // Number of decimal places for x- and y-ordinate values.
    emitZ: false       // Emit z-ordinate values if geometry has them
    precisionZ: 3      // number of decimal digits for z-ordinates
    emitM: false,      // Emit m-ordinate values if geometry has them
    precisionM: 3,     // number of decimal digits for z-ordinates
    emitSize: false    // Emit the size of the geometry definition 
    emitBoundingBox: true, 
                       // Emit the bounding box of the geometry for all dimensions 
    emitIdList: false  // Emit the size of the geometry definition 
);

/*
 geometry ... Geometry from somewhere.

 There are overloads for Write that take a Stream or
 BinaryWriter as argument.
 */
var bytes = geometryWriter.Write(geometry);
```

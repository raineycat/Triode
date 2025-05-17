# Triode

An editor for [Transistor](https://store.steampowered.com/app/237930/Transistor/) and hopefully* other games using GSGE.

###### * if I get said games, or someone with them wishes to contribute

## Project structure:
- `SGLib`: A .NET library for interacting with GSGE and XNA content files
- `SGPackageReader`: A command line tool for inspecting manifest and package files
- `Triode.WPF`: A work in progress GUI editor for package files

## What is GSGE?
GSGE is the internal name (C# namespace) for the engine developed by Supergiant Games.
(don't ask me what it stands for, I'm assuming it means **G-something Supergiant Games Engine**)

It's based on the XNA framework by Microsoft, and has been used in Bastion, Transistor and Pyre.
However, the engine was rewritten in C++ for Hades and Hades II, albeit keeping many similarities in the content files.

## File format info:

### XNB:
- XNB files store processed content that was used with the Microsoft XNA framework
- The format is old and quite well documented, so was trivial to re-implement
- See `SGLib/XNB/XnbFIle.cs` for the parser code I wrote for this project
- XNB files sometimes compress textures using DXT1/3/5 so this project uses LibSquishNet to decompress them
- GSGE Packages (below) embed XNB files to store things like textures

### Bink
- The engine makes heavy use of Bink video (a popular but proprietary video codec) for animated elements
- Since there are many tools out there to view and convert these files (VLC media player, RAD's own software, etc), I haven't written a decoder for them
- At least for Transistor, they are stored in `\Content\Movies\*.bik`, not inside package files
- Packages can however reference them, as explored below

### GSGE Manifests:
- GSGE manifests (*.pkg_manifest) store the names of files embedded in their accompanying package files
- See `SGLib/PackageManifest.cs` for the parsing code I wrote
- All numbers are encoded in **big-endian**
- Both manifests and packages are made up of 'chunks', which have one byte indicating the type and then arbitrary data following this
- The chunk type bytes are:
  - `0xBE`: Indicates the end of a chunk
  - `0xCC`: Includes another package/manifest
  - `0xDE`: Provides information about a texture atlas
  - `0xEE`: Provides information about a Bink video atlas
  - `0xFF`: Indicates the end of the file

### GSGE Packages:
- GSGE packages (*.pkg) store the actual asset data referenced by their manifests
- In transistor at least, I have only seen this used for images/texture atlases
- Package files can optionally be compressed using LZF
- Package files contain XNB files which themselves contain the actual image data
- Packages generally follow the same structure as manifests, containing chunks of data
- They have two additional chunk types though:
  - `0xAD`: The chunk contains a texture file (stored in an XNB container)
  - `0xBB`: References a Bink video file

One interesting thing I've noticed is that large packages can be split into multiple segments.
I'm not fully sure why this happens, but it could be something to do with the en
For example, the file `GUI.pkg` in Transistor contains multiple segments that get decompressed and loaded individually.

### Package versions:
- Package files from transistor use version **5**
- Other games use different version numbers, however I haven't been able to fully investigate this yet

## SGPackageReader:
- SGPackageReader is a command line tool using SGLib
- It can read the data from package manifests and extract some* assets from package files

###### * so far it can handle texture (atlases) and references to Bink files, in the future it might be updated to split texture atlases

#### Usage:

`SGPackageReader print-manifest --manifest-path <file.pkg_manifest> --print-items`
Prints out a list of texture files the manifest references. 
You can optionally specify `--print-atlas-entries` to show the individual texture pieces that get packed together into atlases.

`SGPackageReader dump-assets --package-path <file.pkg>`
Dumps the asset files in a package to disk.
You can specify `--output-folder <folder>` to output to a different folder (`./package-dump` by default).
Images are saved as PNG files, while Bink files have their (relative to the game folder) paths printed out.

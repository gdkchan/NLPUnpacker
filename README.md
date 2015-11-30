# NLPUnpacker
New Love+ for 3DS img.bin unpacker

This is a unpacker for the img.bin container used in New Love Plus for the Nintendo 3DS. It can:
- Extract uncompressed files
- Automatically decompress and extract compressed files
- Convert "SERI" encoded files into XML (they was originally YAML files, but got a custom binary encoding)

Note that its still experimental, so its not guaranteed to work perfectly.

You need DotNetZip to compile this (just add a reference to "Ionic.Zlib.dll").

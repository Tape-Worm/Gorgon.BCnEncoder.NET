# BCnEncoder.NET
A BCn / DXT encoding libary modified for use with Gorgon's image functionality.

Note that this project is ONLY for use in Gorgon and has been __HEAVILY__ modified from the original version to meet that requirement. Only use this if you are feeling incredibly brave.

# What is it?
BCnEncoder.NET is a library for compressing rgba images to different block-compressed formats. It has no native dependencies and is .NET Standard 2.0 compatible.

Supported formats are:
 - BC1 (S3TC DXT1)
 - BC2 (S3TC DXT3)
 - BC3 (S3TC DXT5)
 - BC4 (RGTC1)
 - BC5 (RGTC2)
 - BC7 (BPTC)

# Current state
The current state of this library is in development but quite usable. I'm planning on implementing support for more codecs and 
different algorithms. The current version is capable of encoding and decoding BC1-BC5 and BC7 images in both KTX or DDS formats.

# TO-DO

- [ ] BC6H HDR Encoding

# Contributing
All contributions are welcome. I'll try to respond to bug reports and feature requests as fast as possible, but you can also fix things yourself and submit a pull request. Please note, that by submitting a pull request you accept that your code will be dual licensed under MIT and public domain Unlicense.

# License
This library is dual-licensed under the [Unlicense](https://unlicense.org/), and [MIT](https://opensource.org/licenses/MIT) licenses.

You may use this code under the terms of either license.

Please note, that any dependencies of this project are licensed under their own respective licenses.

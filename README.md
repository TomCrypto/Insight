Iridium
=======

<p align="justify">
The Iridium library aims to bring interactive, high quality, and physically accurate human eye diffraction features to HDR-capable software, such as video games or computer graphics renderers. The library is designed to be unobtrusive yet highly configurable, and takes full advantage of modern graphics hardware. Iridium is tuned for use in DirectX 11 applications and is developed in C#, though the code can be readily ported to other languages.
</p>

<p align="justify">
<b>Please note</b>: at the moment, the code is a direct port of a previously developed prototype (to set up the repository structure) and does <b>not</b> reflect the design goals of this project. The "sample" in its intended form does not exist yet, though you can have fun with the prototype as it is now (it will soon be replaced). Stand by.
</p>

Build Instructions
------------------

<p align="justify">
The included C# solution contains two projects, the library itself and an accompanying sample. The latter depends on the former, but if you are using Visual Studio 2012 this should be largely transparent. To compile the library and try out the sample, simply build the solution inside the source folder, and run the sample project.
</p>

<p align="justify">
Both the library and the sample are powered by the <a href="http://sharpdx.org/" title="SharpDX Home Page">SharpDX</a> project, and require DirectX 11. The SharpDX binaries are served via NuGet as part of the build process, however should this not work for any reason, you can always download the latest SharpDX release and adjust assembly references in both projects as needed.
</p>

Sample Program
--------------

<p align="justify">
The provided sample is intended to showcase the library. It features many real-time configuration options, and lets the user navigate a simple HDR environment while observing the diffraction effects rendered by Iridium.
</p>

<p align="center">
(insert image here)
</p>

Documentation
-------------

<p align="justify">
A complete API documentation is provided for the Iridium library, along with some theoretical background helpful in understanding how to correctly use the library, and is available in the Portable Document Format (PDF).
</p>

Compatibility
-------------

<p align="justify">
Iridium runs under any platform which supports DirectX 11 - in other words, Windows Vista and up. However, the API exposed by the library is fairly generic, and alternative implementations may choose to follow it.
</p>
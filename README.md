Insight
=======

<p align="justify">
The Insight library aims to bring interactive, high quality, and physically accurate human eye diffraction features to HDR-capable software, such as video games or computer graphics renderers. The library is designed to be unobtrusive yet highly configurable, and takes full advantage of modern graphics hardware. Insight is tuned for use in DirectX 11 applications and is developed in C#, though the code can be readily ported to other languages.
</p>

Implementation Status
---------------------

The library is currently a work in progress and is **not** ready for use.

#### Aperture Simulation

✘ No aperture generation capabilities yet (hardcoded static aperture)

#### Diffraction Spectrum

✔ Can generate diffraction spectra from arbitrary apertures (of the right dimension)

✘ No support for different color systems for wavelength -> RGB conversion (only CIE at the moment)

#### FFT Convolution

✔ Properly (and efficiently) convolves on Medium settings (used for testing)

✔ Supports arbitrary input sizes to some extent (not completely pixel-perfect, and not very optimized)

#### Sample

✔ Has a working pipeline and tonemapping algorithm

✔ First iteration of the test scene (not very pretty but it works)

✘ No configuration options at all (nor any GUI) yet

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
The provided sample is intended to showcase the library. It features many real-time configuration options, and lets the user navigate a simple HDR environment while observing the diffraction effects rendered by Insight.
</p>

<p align="center">
(insert image here)
</p>

Documentation
-------------

<p align="justify">
A complete API documentation is provided for the Insight library, along with some theoretical background helpful in understanding how to correctly use the library, and is available in the Portable Document Format (PDF).
</p>

Compatibility
-------------

<p align="justify">
Insight runs under any platform which supports DirectX 11 - in other words, Windows Vista and up. However, the API exposed by the library is fairly generic, and alternative implementations may choose to follow it.
</p>
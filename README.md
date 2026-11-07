# NIGHT SHIFT <<

## Audio

All sound effects and the music loop (`Assets/02_Audio`) are synthesized from scratch by our own script (square, triangle and noise waves); no third-party samples or libraries are used.

## WebGL build (itch.io)

Unity 6000.3.14f1 + Web Build Support module, build target WebGL, output `Builds/WebGL` (gitignored). `PlayerSettings.WebGL.decompressionFallback` is on because itch.io does not send `Content-Encoding` for the Brotli files. Zip the folder contents so `index.html` is at the zip root (`cd Builds/WebGL && tar -a -cf ../ProjectShift-WebGL.zip index.html Build TemplateData`; PowerShell 5.1 `Compress-Archive` writes backslash paths, avoid it). 2026-11-02: build OK, 13 MB, runs in Edge from a local server.

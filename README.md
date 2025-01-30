<p id="downloads" align="center">
	<img src="https://i.stack.imgur.com/TOfqL.png" height="120px"/>
	<h1 align="center">Xtreme Download Manager</h1>
</p>

<p align="center">
	<a href="https://github.com/subhra74/xdm/workflows/Java%20CI/badge.svg?branch=master"><img src="https://github.com/subhra74/xdm/workflows/Java%20CI/badge.svg?branch=master" alt="Java CI" /></a>
	<a href="https://camo.githubusercontent.com/278e057571a0481121b2d60490ff656fb8736a20/68747470733a2f2f696d672e736869656c64732e696f2f6769746875622f646f776e6c6f6164732f73756268726137342f78646d2f746f74616c2e737667"><img src="https://img.shields.io/github/downloads/subhra74/xdm/total.svg" alt="Github All Releases" /></a>
</p>

[New Experimental Beta version is out](https://github.com/subhra74/xdm-experimental-binaries/tags)


**X**treme **D**ownload **M**anager (XDM) is a powerful tool to increase download speeds up to 500%, save videos from popular video streaming websites, resume broken/dead downloads, schedule and convert downloads.<br>
XDM seamlessly integrates with Google Chrome, Mozilla Firefox Quantum, Opera, Vivaldi and other Chroumium and Firefox based browsers, to take over downloads and saving streaming videos from web. XDM has a built in video converter which lets you convert your downloaded videos to different formats so that you can watch them on your mobile or TV (100+ devices are supported)


## Screenshots

| ![xdm_1][01] | ![xdm_5][05] | ![xdm_3][03] |
| --- | --- | --- |
| ![xdm_7][07] | ![xdm_6][06] | ![xdm_9][09] |
| ![xdm_4][04] | ![xdm_2][02] |  |


## Features
- Download files at maximum possible speed (5-6 times faster than conventional downloaders).
- XDM can save video from numerous video streaming sites.
- Works with all modern browsers on Windows, Linux and Mac OS X. XDM supports [Google Chrome][18], [Chromium][18], [Firefox Quantum][19], [Vivaldi][20], [Edge][21] and many other popular browsers.
- XDM has built in video converter, which lets you convert downloaded video to MP3 and MP4 formats.
- Supports `HTTP`, `HTTPS`, `FTP` as well as video streaming protocols like `MPEG-DASH`, `Apple HLS`, and `Adobe HDS`.
- XDM also supports authentication, proxy servers, cookies, redirection etc.
- Video download, clipboard monitoring, automatic antivirus checking, scheduler, system shutdown on download completion.
- Resumes broken / dead downloads caused by connection problem, power failure or session expiration.
- Works with Windows ISA, auto proxy scripts, proxy servers, NTLM, Kerberos authentication.

## Building from Source

### Prerequisites
- .NET 6.0 SDK or later
- Visual Studio 2022 or later (optional, for IDE support)

### Build Instructions

1. Clone the repository:
```powershell
git clone https://github.com/AdibSadman192/xdm-enhanced.git
cd xdm-enhanced
```

2. Build the solution:
```powershell
cd app/XDM
dotnet build XDM.sln --configuration Release
```

3. Run the application:
```powershell
cd XDM.Wpf/bin/Release/net6.0-windows
./XDM.Wpf.exe
```

### Creating Release Package

1. Build the release package:
```powershell
cd app/XDM
dotnet publish XDM.Wpf/XDM.Wpf.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

2. The packaged files will be available in:
```
app/XDM/XDM.Wpf/bin/Release/net6.0-windows/win-x64/publish/
```

3. To create a release:
   - Go to your GitHub repository
   - Click on "Releases" > "Create a new release"
   - Create a new tag (e.g., v1.0.0)
   - Upload the following files from the publish directory:
     - XDM.Wpf.exe (Main application)
     - Any additional DLLs or dependencies
   - Add release notes describing the changes
   - Click "Publish release"

[//]: #ImageLinks
[01]: https://i.stack.imgur.com/s7ViA.jpg
[02]: https://i.stack.imgur.com/90TQO.jpg
[03]: https://i.stack.imgur.com/V5XF3.jpg
[04]: https://i.stack.imgur.com/aFyH5.png
[05]: https://i.stack.imgur.com/lmAr6.png
[06]: https://i.stack.imgur.com/H4yMj.png
[07]: https://i.stack.imgur.com/8ulBq.png
[08]: https://i.stack.imgur.com/Gfgae.jpg
[09]: https://i.stack.imgur.com/GlVDC.png

[//]: #DownloadLinks
[10]: https://github.com/subhra74/xdm/releases/download/7.2.10/xdmsetup.msi
[11]: https://github.com/subhra74/xdm/releases/download/7.2.10/xdm-setup-7.2.10.tar.xz
[12]: #
[13]: https://github.com/subhra74/xdm/releases/download/7.2.10/xdman.jar
[14]: https://sourceforge.net/projects/xdman/files/xdmsetup-2018.msi/download
[15]: https://sourceforge.net/projects/xdman/files/xdm-2018-x64.tar.xz/download
[16]: https://sourceforge.net/projects/xdman/files/XDMSetup.dmg/download
[17]: http://xdman.sourceforge.net/xdman.jar
[100]: https://github.com/subhra74/xdm/releases/download/7.2.11/xdm-setup.msi
[101]: https://github.com/subhra74/xdm/releases/download/7.2.11/xdm-setup-7.2.11.tar.xz
[102]: https://github.com/subhra74/xdm/releases/download/7.2.11/xdman.jar

[//]: #AddonLinks
[18]: https://chrome.google.com/webstore/detail/xtreme-download-manager/dkckaoghoiffdbomfbbodbbgmhjblecj
[19]: https://addons.mozilla.org/en-US/firefox/addon/xdm-browser-monitor/
[20]: #
[21]: https://sourceforge.net/p/xdman/blog/2018/01/xdm-integration-with-microsoft-edge/

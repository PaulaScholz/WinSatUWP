# Desktop Bridge COM Client Self-Elevation Sample

##Developer Incubation and Learning (DIaL)

This mixed UWP and Win32 sample shows how to:

-  Launch a fullTrust background Win32 process from a UWP application that calls COM, with elevation when required, through Desktop Bridge
-  Exchange complex object data through Desktop Bridge using JSON
-  Translate native Win32 HBITMAPs returned by Windows APIs to a format transmissible through Desktop Bridge and use them in UWP UI
-  Windows System Assessment Tool (WinSAT) ratings are the demonstration vehicle for the sample.

There are 8 xCopy commands that copy the Win32 apps into the correct Appx folders.  Normally, we would use Visual Studio macros to set these directory names, but those macros have been broken for some time now, causing many people no end of problems, so the paths are hard-coded for the dev locations on my laptop.

You’ll need to ensure the paths are correct for your dev setup.  Right-click on the WinSatPackage project and go to Build Events tab.  Then, edit the Post-build event command line to change these directories to reflect your setup.

xcopy /y /s "C:\dev\WinSatUWP\WinSatUWP\WinSatInfo\obj\Debug\Interop.WINSATLib.dll" "C:\dev\WinSatUWP\WinSatUWP\WinSatPackage\bin\x64\Debug\AppX\WinSatInfo\"
xcopy /y /s "C:\dev\WinSatUWP\WinSatUWP\WinSatInfo\obj\Debug\Interop.WINSATLib.dll" "C:\dev\WinSatUWP\WinSatUWP\WinSatPackage\bin\x86\Debug\AppX\WinSatInfo\"
xcopy /y /s "C:\dev\WinSatUWP\WinSatUWP\WinSatInfo\obj\Debug\Interop.WINSATLib.dll" "C:\dev\WinSatUWP\WinSatUWP\WinSatPackage\bin\x64\Debug\AppX\WinSatRunAssessment\"
xcopy /y /s "C:\dev\WinSatUWP\WinSatUWP\WinSatInfo\obj\Debug\Interop.WINSATLib.dll" "C:\dev\WinSatUWP\WinSatUWP\WinSatPackage\bin\x86\Debug\AppX\WinSatRunAssessment\"
xcopy /y /s "C:\dev\WinSatUWP\WinSatUWP\WinSatInfo\obj\Release\Interop.WINSATLib.dll" "C:\dev\WinSatUWP\WinSatUWP\WinSatPackage\bin\x64\Release\AppX\WinSatInfo\"
xcopy /y /s "C:\dev\WinSatUWP\WinSatUWP\WinSatInfo\obj\Release\Interop.WINSATLib.dll" "C:\dev\WinSatUWP\WinSatUWP\WinSatPackage\bin\x86\Release\AppX\WinSatInfo\"
xcopy /y /s "C:\dev\WinSatUWP\WinSatUWP\WinSatInfo\obj\Release\Interop.WINSATLib.dll" "C:\dev\WinSatUWP\WinSatUWP\WinSatPackage\bin\x64\Release\AppX\WinSatRunAssessment\"
xcopy /y /s "C:\dev\WinSatUWP\WinSatUWP\WinSatInfo\obj\Release\Interop.WINSATLib.dll" "C:\dev\WinSatUWP\WinSatUWP\WinSatPackage\bin\x86\Release\AppX\WinSatRunAssessment\"


![architecture diagram](</docimages/WinSatUWP_currentArchitecture.PNG>)

![sample screenshot](</docimages/WinSatUWP_desktopBridge.PNG>)
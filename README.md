# Desktop Bridge COM Client Self-Elevation Sample

## Windows - Developer Incubation and Learning - Paula Scholz

## Introduction

There are times when you may want to call a COM object from your Universal Windows Platform (UWP) application. You might want to access third-party libraries or call some part of Windows available only through the Component Object Model (COM).  But, it is not possible to access arbitrary COM objects from a UWP app.  

If you only want to sideload your application instead of deploying it through the Windows Store, you may call COM objects and third-party libraries through [Brokered Windows Runtime Components for side-loaded Windows Store apps](https://docs.microsoft.com/en-us/windows/communitytoolkit/), but this technique is not allowed for apps deployed through the store.

This article shows how you may call COM interfaces by integrating a UWP application with a "full trust" Win32 process launched through [Desktop Bridge](https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-extend), from which we call COM interfaces. From that "full trust" Win32 process, you may launch another elevated Win32 process that can  call COM interfaces with administrator privilege. All these apps are distributed together in a single [Windows Application Packaging Project](https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-packaging-dot-net) through the Windows Store, or as a sideload.

## Windows System Assessment Tool

We show these techniques by calling a COM server in Windows that provides performance capabilities of the computer using the [Windows System Assessment Tool (WinSAT)](https://docs.microsoft.com/en-us/windows/desktop/WinSAT/windows-system-assessment-tool-portal), in the form of the Windows Experience Index.

First introduced in the Windows Vista operating system, the Windows Experience Index is calculated by WinSAT when your computer is turned on for the first time, during the Machine-Out-Of-Box-Experience (MOOBE) . In Windows Vista, Windows 7, and Windows 8, the Windows Experience Index was displayed by the Performance Information and Tools control panel applet, but that applet was removed from Windows in version 8.1.  However, the WinSAT COM interfaces are still present in Windows 10, though they may be removed in a future release.


![Figure 1 Performance Information and Tools Control Panel Applet](/images/Fig1_perfTool.png)

*Figure 1 - Performance Information and Tools Control Panel Applet (Windows 8)*

The WinSAT COM interfaces may be queried for the results of a previous assessment and may also request a new assessment be run, which requires administrator privilege.

We recreate the important parts of the Performance applet in a UWP application (WinSatUWP.exe), which calls a headless Win32 background app service (WinSatInfo.exe) to get performance data from the WinSAT COM interface, and another elevated Win32 console app (WinSatRunAssessment.exe) that calls WinSAT to run a new performance assessment. This command requires administrator privilege.  The solution layers are shown in Figure 2.

![Figure 2 Solution Layers](/images/Fig2_appLayers.png)

*Figure 2 - WinSatUWP Application Layers*

Using this architecture, we build a UWP application that looks similar to and performs the same function as the old Performance Information and Tools control panel applet.  The UWP application launches a full-trust process to query WinSAT for assessment information, queries WinSAT for a bitmap that depicts the overall assessment, and requests elevation for another Win32 process to command a new assessment.  The application is shown below:

![Figure 3 Windows Experience Index User Interface](/images/Fig3_newUI.png)

*Figure 3 - Windows Experience Index User Interface (WinSatUWP.exe)*

As you can see, it looks a lot like the original control panel applet and has a single UI control, a hyperlink button that commands a new assessment be made.  When clicked, Windows will launch an elevation dialog to acquire administrator privilege for that action.

## The Visual Studio Solution

The Visual Studio solution is shown below.  There are five projects.

![Figure 4 Visual Studio Solution](/images/Fig4_solution.png )

*Figure 4 - Visual Studio solution*

Let's go through these quickly to understand how they fit in the solution layer stack and then drill down individually in greater detail.

From the top down, we first see the WinSatInfo project.  This is the "headless" Win32 App Service layer in the middle of our solution stack (headless meaning it has no user interface and shows no window).  Notice it has only one code file, Program.cs, and also includes *Interop.WINSATLib.dll* which is the COM interop library created by Visual Studio when the WinSAT COM object is imported.  We'll come back to this DLL later because it needs special attention.

Next, we see the WinSatModels project. This project contains a single code file, the data model that is shared between WinSatInfo and WinSatUWP and contains assessment information gathered from WinSAT.  Instances of this class are produced by WinSatInfo when querying WinSAT and consumed by WinSatUWP for display in the user interface.

Then, the solution contains **WinSatPackage**, the start-up project, which is the [Windows Application Packaging Project](https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-packaging-dot-net) for our solution.  It is within the context of this project that our solution will be packaged for sideloading and deployment to the Windows Store.  This is the project where package capabilities, store logos, and configuration are set.

Now we come to WinSatRunAssessment, the application launched by WinSatInfo as elevated with administrator privilege.  This program's only purpose is to call WinSAT with administrator privilege to run a new assessment. While WinSatUWP and WinSatInfo communicate with each other over an [AppServiceConnection](https://docs.microsoft.com/en-us/uwp/api/Windows.ApplicationModel.AppService.AppServiceConnection), WinSatRunAssessment can only communicate with WinSatInfo by its exit code, which is actually sufficient for our purpose.  This application is the bottom one on our solution stack.

Finally, at the top of our solution stack and at the bottom of the Visual Studio solution we find WinSatUWP, the Universal Windows Platform program that provides the user interface, launches WinSatInfo as a "full-trust" Win32 app, receives assessment data from WinSatInfo over an AppServiceConnection, and sends a "run assessment" command to WinSatInfo which then launches WinSatRunAssessment as an elevated process.
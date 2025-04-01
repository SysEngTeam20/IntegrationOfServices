# README - Viewer Scene

## Overview

This README provides information about the **Viewer** Unity scene, located within the `IntegrationOfServices` project at `Assets/Apps/ViewerScene/Viewer.unity`.

This application implements an immersive and interactive virtual touring experience. It is designed to load preconfigured virtual scenes (potentially created using the companion Admin scene) from a server. A key feature is the integrated AI conversational agent, acting as an interactive voice assistant. This AI utilizes Retrieval-Augmented Generation (RAG) capabilities, allowing it to answer viewer questions and address problems effectively. Furthermore, the AI can be provided with additional context or knowledge specific to the scene currently being viewed, enhancing the interactive tour.

## Features

* **Immersive Virtual Tours:** Experience preconfigured 3D environments.
* **Dynamic Scene Loading:** Loads scene configurations from a server.
* **AI Conversational Agent:** Interact with an AI voice assistant within the virtual environment.
* **RAG Capabilities:** The AI can retrieve relevant information to augment its responses.
* **Interactive Assistance:** Ask the AI questions or describe problems for assistance.
* **Context-Aware AI:** The AI can leverage specific knowledge about the current scene.

## Prerequisites

Before you begin, ensure you have the following installed:

1.  **Unity Hub:** Download and install from [https://unity.com/download](https://unity.com/download).
2.  **Unity Editor:**
    * Version: **6000.0.25f1 or later**.
    * Install via Unity Hub.
    * Ensure **Android Build Support** is included during installation, specifically with:
        * `Android SDK & NDK Tools`
        * `OpenJDK`
        *(Note: Android components are essential for the VR build option.)*
3.  **Git:** Required to clone the repository.
4.  **(Optional) Meta Quest Headset:** Required for the Android VR build option (Meta Quest 2, 3, or Pro recommended).
5.  **(Optional) Meta Developer Account:** Required to enable Developer Mode on the Quest headset.

## Setup Instructions

1.  **Clone the Repository:**
    ```bash
    git clone [https://github.com/SysEngTeam20/IntegrationOfServices.git](https://github.com/SysEngTeam20/IntegrationOfServices.git)
    ```
2.  **Open Unity Hub:** Launch the Unity Hub application.
3.  **Add Project:**
    * Navigate to the **Projects** tab.
    * Click the **Add** button (usually in the top right corner).
    * Select **Add project from disk**.
    * Navigate to the cloned repository folder and select the `integrationofservices/unity` subfolder.
    * Click **Open** (or **Add Project**).
4.  **Open Project in Unity:** Select the project in Unity Hub and ensure you open it with the correct Unity Editor version (6000.0.25f1 or later). Unity will import the project, which may take some time.
5.  **Open the Viewer Scene:**
    * Inside the Unity Editor, locate the **Project** window (usually at the bottom).
    * Navigate to the following path: `Assets/Apps/ViewerScene/`
    * Double-click the `Viewer.unity` scene file to open it.

## Building and Running

There are two primary methods for running this scene:

### Option A: Desktop Build (VR Simulator - Recommended for Functionality Testing)

This method builds the application for macOS or Windows and utilizes Unity's built-in tools or compatible third-party simulators to emulate VR interactions without requiring a physical headset connection during runtime (though headset controllers might still be used via Link/AirLink if configured). This currently offers the most complete functionality according to project notes.

1.  **Verify Scene Setup:**
    * Ensure the `Viewer.unity` scene is open.
    * In the **Hierarchy** window, check the following:
        * The `XR Interaction Setup` GameObject should be **enabled** (checkbox ticked in the Inspector).
        * The `XR Origin (XR Rig)` GameObject should be **disabled** (checkbox unticked in the Inspector).
        * *(This is typically the default state required for simulator/desktop testing.)*
2.  **Open Build Settings:**
    * In the Unity Editor top menu, navigate to `File -> Build Settings` (or potentially `File -> Build Profiles`).
3.  **Select Target Platform:**
    * Choose `PC, Mac & Linux Standalone` (or `MacOs/Windows` if shown under Build Profiles).
    * Ensure the **Target Platform** field shows your desired OS (Windows, macOS). If not, select it and click **Switch Platform** (this may take some time).
4.  **Manage Scene List:**
    * In the **Scenes In Build** list:
    * If the `Viewer` scene (`Assets/Apps/ViewerScene/Viewer.unity`) is not listed, click **Add Open Scenes**.
    * Make sure the checkbox next to the `Assets/Apps/ViewerScene/Viewer.unity` scene is **checked**. Uncheck any other scenes if you only want to build this one.
5.  **Build the Application:**
    * Click the **Build** or **Build And Run** button.
    * Choose a name (e.g., `ViewerScene_Desktop`) and location for the build output.
6.  **Run the Build:**
    * Once the build is complete, navigate to the output folder and run the executable. You may need additional setup for your specific VR simulator if you are using one.

### Option B: Android Build (Meta Quest VR)

This method builds the application directly for deployment onto a Meta Quest headset.

1.  **Configure Scene for VR:**
    * Ensure the `Viewer.unity` scene is open.
    * In the **Hierarchy** window:
        * Find the `XR Interaction Setup` GameObject and **disable** it (uncheck the box next to its name in the Inspector).
        * Find the `XR Origin (XR Rig)` GameObject and **enable** it (check the box next to its name in the Inspector).
2.  **Configure Project Settings for VR:**
    * Go to `Edit -> Project Settings`.
    * Select `XR Plug-in Management` from the left-hand list.
    * Go to the **Android** tab (represented by the Android logo).
    * Ensure the **Oculus** (or potentially **Meta XR Feature Group**) checkbox under Plug-in Providers is **checked**.
    * Ensure the **Initialize XR on Startup** checkbox is **checked**.
3.  **Configure Build Settings:**
    * Go to `File -> Build Settings` (or `File -> Build Profiles`).
    * Select **Android** from the Platform list.
    * If Android is not the current platform, click **Switch Platform** (this can take significant time).
    * Ensure `Assets/Apps/ViewerScene/Viewer.unity` is the only scene checked in the **Scenes In Build** list (use **Add Open Scenes** if needed).
4.  **Prepare Meta Quest Device:**
    * **Enable Developer Mode:** You must enable Developer Mode on your headset through the Meta Quest mobile app or developer website. This requires a registered developer account.
    * **Connect Headset:** Connect your Meta Quest headset to your computer using a USB-C cable.
    * **Allow Debugging:** Put on the headset. A prompt should appear asking "Allow USB debugging?". Enable the checkbox for "Always allow from this computer" and then select **Allow**.
5.  **Build and Run on Device:**
    * Back in the Unity **Build Settings** window:
    * Find the **Run Device** dropdown menu. Click **Refresh**. Your connected Meta Quest headset should appear in the list (it might show its serial number or IP address). Select it.
    * Click **Build And Run**.
    * Unity will compile the project into an APK file, transfer it to the connected headset, and attempt to install and launch it. This process can take several minutes. You can monitor progress in the Unity console.

## Troubleshooting

* **Input Not Working (Controllers/Hands in VR, Simulated Input):**
    * If controls within the built application or the editor seem unresponsive, especially relating to VR interactions or simulated input:
    * Go to `Edit -> Project Settings` in the Unity Editor.
    * Select the `Player` category on the left.
    * In the right panel, find the `Other Settings` section.
    * Locate the `Active Input Handling` option.
    * Change its value to **Input System Package (New)**. This is often required for projects using Unity's newer Input System, common in XR setups.
    * Retry running in the editor or rebuild the application.

* **Missing Prefabs or Components (Red Text in Hierarchy/Project):**
    * If you notice object names appearing in red text in the Hierarchy or Project window, or if components seem missing from GameObjects, it might be due to required package samples not being imported.
    * Go to `Window -> Package Manager` in the Unity Editor top menu.
    * In the Package Manager window, ensure you are viewing packages `In Project` or find the `Ubiq` package (you might need to select `My Assets` or another source if it wasn't added directly to the project manifest).
    * Select the `Ubiq` package from the list.
    * In the details panel on the right, look for a `Samples` tab or section.
    * Find the sample named `Demo (XRI)`.
    * Click the **Import** button next to `Demo (XRI)`.
    * Wait for Unity to import the sample assets into your project (usually under an `Assets/Samples/Ubiq/...` folder).
    * This imports required prefabs and assets used by the Viewer scene, resolving issues where objects might appear missing or broken. Check the scene again after import.
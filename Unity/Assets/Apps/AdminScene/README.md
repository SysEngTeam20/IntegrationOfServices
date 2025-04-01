# README - Admin Scene (portalt-admin)

## Overview

This README provides information about the **Admin** Unity scene, located within the `IntegrationOfServices` project at `Assets/Apps/AdminScene/Admin.unity`.

This application implements an interactive Object Placement system. It allows users (presumably administrators or designers) to select and download predefined 3D scene layouts. Each scene comes with a list of associated 3D models. Users can then manipulate these models within the scene – positioning, scaling, and rotating them freely to customize the environment. Finally, the configured scene layout can be saved by uploading and updating the configuration data on a server.

## Features

* **Scene Selection & Loading:** Select and download 3D scenes from a server.
* **Model Library:** Access a list of 3D models associated with the currently loaded scene.
* **Interactive Object Placement:**
    * Place models into the scene.
    * Adjust the position of models.
    * Change the scale of models.
    * Modify the rotation of models.
* **Scene Customization:** Freely arrange objects to create the desired scene layout.
* **Configuration Management:** Save the customized scene configuration by uploading/updating it on the server.

## Prerequisites

Before you begin, ensure you have the following installed:

1.  **Unity Hub:** Download and install from [https://unity.com/download](https://unity.com/download).
2.  **Unity Editor:**
    * Version: **6000.0.25f1 or later**.
    * Install via Unity Hub.
    * Ensure **Android Build Support** is included during installation, specifically with:
        * `Android SDK & NDK Tools`
        * `OpenJDK`
        *(Note: While these Android components are listed as required for the initial setup, the build steps below are for Desktop platforms.)*
3.  **Git:** Required to clone the repository.

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
5.  **Open the Admin Scene:**
    * Inside the Unity Editor, locate the **Project** window (usually at the bottom).
    * Navigate to the following path: `Assets/Apps/AdminScene/`
    * Double-click the `Admin.unity` scene file to open it.

## Building and Running (Desktop - Mac/Windows)

1.  **Open Build Profiles/Settings:**
    * In the Unity Editor top menu, navigate to `File -> Build Profiles -> MacOs/Windows`.
    *(Note: In some standard Unity versions, this might be under `File -> Build Settings`. Follow the path available in your editor.)*
2.  **Select Target Platform:** Ensure `MacOs/Windows` (or `PC, Mac & Linux Standalone`) is selected as the target platform.
3.  **Manage Scene List:**
    * Look for a section listing the scenes in the build. You might need to click **Open Scene List** or similar.
    * If the `Admin` scene (`Apps/AdminScene/Admin`) is not listed, click **Add Open Scenes**.
    * Make sure the checkbox next to the `Apps/AdminScene/Admin` scene is **checked**. Uncheck any other scenes if you only want to build this one.
4.  **Build the Application:**
    * Click the **Build** or **Build And Run** button.
    * A dialog box will appear asking for a build name and location. Choose a name (e.g., `AdminSceneBuild`) and select a folder where the built application will be saved.
5.  **Run the Build:**
    * Once the build process is complete ("Build Complete"), navigate to the folder you selected in the previous step.
    * Run the executable file created by Unity.

## Troubleshooting

* **Input Not Working (Keyboard/Mouse):**
    * If controls within the built application or the editor seem unresponsive, you might need to adjust the input handling settings.
    * Go to `Edit -> Project Settings` in the Unity Editor.
    * Select the `Player` category on the left.
    * In the right panel, find the `Other Settings` section.
    * Locate the `Active Input Handling` option.
    * Change its value to **Both**.
    * Try building or running in the editor again.
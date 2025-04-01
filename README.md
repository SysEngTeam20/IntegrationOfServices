# IntegrationOfServices Repository

## Overview

Welcome to the `IntegrationOfServices` project repository. This project contains the necessary assets and code for a Unity application focused on the creation, administration, and viewing of interactive 3D/VR environments.

The core components allow for:
1.  **Scene Administration:** An interface for selecting base scenes, adding/manipulating 3D models (position, rotation, scale), and saving the final configuration to a server.
2.  **Scene Viewing:** An immersive experience (Desktop Simulator or VR) to load and explore the preconfigured scenes, featuring interaction and an AI voice assistant for enhanced engagement.

## Project Structure

The primary application logic and assets are located within the `/Unity` directory. This directory contains the Unity project itself.

## Key Scenes & Documentation

The Unity project features two main scenes, each with its own dedicated documentation detailing setup, features, build processes, and troubleshooting:

1.  **Admin Scene (`Admin.unity`)**
    * **Purpose:** Used for configuring and customizing the 3D scenes and their object layouts.
    * **Detailed README Location:** [`Unity/Assets/Apps/AdminScene/README.md`](./Unity/Assets/Apps/AdminScene/README.md)
        *(Note: You should save the Admin Scene README content generated earlier into a file named `README.md` at this location.)*

2.  **Viewer Scene (`Viewer.unity`)**
    * **Purpose:** Used for experiencing the configured scenes immersively, either via a desktop simulator or a VR headset, and interacting with an AI assistant.
    * **Detailed README Location:** [`Unity/Assets/Apps/ViewerScene/README.md`](./Unity/Assets/Apps/ViewerScene/README.md)
        *(Note: You should save the Viewer Scene README content generated earlier into a file named `README.md` at this location. Please double-check the directory name is exactly `ViewerScene` as there was a typo "VieweScene" in the path provided previously.)*

## Getting Started

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/SysEngTeam20/IntegrationOfServices.git](https://github.com/SysEngTeam20/IntegrationOfServices.git)
    ```
2.  **Ensure Prerequisites:** Install Unity Hub and the recommended Unity Editor version (6000.0.25f1 or later) with necessary modules (Android Build Support, etc.) as detailed in the scene-specific READMEs.
3.  **Open the Unity Project:** Add and open the `/Unity` subfolder as a project in Unity Hub.
4.  **Consult Scene READMEs:** Navigate to the specific README files linked above for detailed instructions on configuring, building, and running either the Admin or Viewer scene.

---

*This README provides a general overview. For specific instructions, please refer to the linked README files within the respective scene directories.*
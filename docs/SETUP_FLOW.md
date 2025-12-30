# Device Setup Flow

This document outlines the setup process for new devices on the TGP platform. The platform supports Windows, Android, and ChromeOS devices.

## 1. Windows Client Setup

The Windows client provides comprehensive monitoring for Windows PCs and laptops.

1.  **Install the Service**: Run the TGP Windows Client installer as Administrator.
2.  **Required Permissions**:
    *   **Administrator Rights**: Required during installation for service registration.
    *   **Firewall Access**: Allow outbound connections to TGP servers.
3.  **Login**:
    *   The Login window appears after installation.
    *   Enter your username and password.
    *   The client authenticates with the SSO service.
    *   The device registers as a **Windows** device type.
4.  **Activation**:
    *   The monitoring service starts automatically.
    *   User Agent runs in the system tray for notifications.
    *   The device appears in the dashboard as "Windows Device" (or hostname).

## 2. Android App Setup (Enforcer)

The Android app acts as the "Enforcer", ensuring the device policy is respected.

1.  **Install the App**: Download and install `TGP.Client.Android` from Google Play or sideload.
2.  **Grant Permissions**:
    *   **Overlay Permission**: Required to block apps when time is up.
    *   **Accessibility Service**: Required to monitor app usage and prevent uninstalling.
3.  **Login**:
    *   Open the app and enter your username and password.
    *   The app authenticates with the SSO service.
    *   The app registers the device as an **Android** device type.
4.  **Activation**:
    *   Once logged in, the "Enforcement Service" starts.
    *   The device appears in the dashboard as "Android Device" (or model name).

## 3. Chrome Extension Setup (Filter)

The Chrome extension acts as the "Filter", monitoring web traffic and enforcing content restrictions.

1.  **Install the Extension**: Install the TGP Chrome Extension from the store or by loading unpacked.
2.  **Login**:
    *   Click the extension icon.
    *   Enter your username and password.
    *   The extension authenticates with the SSO service.
    *   The extension registers itself as a **ChromeOS** device type.
3.  **Activation**:
    *   The extension begins monitoring web navigation and content.
    *   The extension appears in the dashboard as a *separate* device ("ChromeOS Extension").

## Dashboard Visibility

Devices appear in the user dashboard based on their registration:

| Client Type | Dashboard Name | Capabilities |
|-------------|---------------|--------------|
| Windows | Windows Device (hostname) | Full monitoring, app blocking, screen capture |
| Android | Android Device (model) | App monitoring, time enforcement, blocking |
| Chrome Extension | ChromeOS Extension | Web filtering, content monitoring |

**Note:** A single physical device may appear as multiple entries if multiple clients are installed (e.g., Android + Chrome extension on a Chromebook).

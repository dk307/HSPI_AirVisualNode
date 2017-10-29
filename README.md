Homeseer Ubiquiti mPower PlugIn
=====================================
Overview
--------
This plugin displays integrates Ubiquiti mPower Devices with Homeseer. It can display device values and can control individual power states of ports.
The plugin in written in C# and is based on a sample from http://board.homeseer.com/showthread.php?t=178122.

It uses undocumented websockets interface to connect with device directly and get updates from it. Same websocket is used to control it. This is unlikely to change in future as mPower device development has been abandoned by parent company. But you can still buy these devices.

Compatibility
------------
Tested on the following platforms:
* Windows 10

Devices:
* mPower-Pro (8-port) (Ver 2.1.11)

Since it is same software it should work with other 2 mPower devices.

Installation
-----------
Make sure that dotNet 4.6.2 is installed on machine. [Link](https://support.microsoft.com/en-us/help/3151802/the-.net-framework-4.6.2-web-installer-for-windows)

Place the compiled [executable](https://ci.appveyor.com/project/dk307/hspi-ubiquitimpower/build/artifacts?branch=master) and [config file](https://ci.appveyor.com/project/dk307/hspi-ubiquitimpower/build/artifacts?branch=master) in the HomeSeer installation directory. Restart HomeSeer. HomeSeer will recognize the plugin and will add plugin in disable state to its Plugins. Go to HomeSeer -> PlugIns -> Manage and enable this plugin. 

Open Menu Item PlugIns->Ubiquiti mPower->Configuration. You should see page with Add New Button.

Click on Add New and enter device details. You can set resolution to avoid unnecessary updates to HomeSeer as these devices have high resolution for these values.

![Device Details](/asserts/DeviceDetails.png "Device Details")
 
Click Add. Devices in Homeseer are only created when value is received from device. If you do not see any devices created, enable Debug Logging and check logs.

![Working Devices](/asserts/WorkingDevices.png "Working Devices")

Build State
-----------
[![Build State](https://ci.appveyor.com/api/projects/status/github/dk307/HSPI_UbiquitiMPower?branch=master&svg=true)](https://ci.appveyor.com/project/dk307/HSPI-UbiquitiMPower/build/artifacts?branch=master)

  

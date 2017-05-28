Python API for Empyrion [![Build Status](https://travis-ci.org/huhlig/empyrion-python-api.svg?branch=master)](https://travis-ci.org/huhlig/empyrion-python-api)
=======================

The Empyrion Python API provides a python interface designed to
react to Empyrion Server Events. It provides the following:

* Ability to Register Python Function Callbacks for specific in game events.
* A REST Server capable of triggering python functions events with parameters.
* A local SQLite Database for storing data through server restarts.

How to install
--------------

1. Checkout from git and build in Visual Studio

2. Copy 'Empyrion Python API\Config\epaconfig.yaml' to 'Empyrion - Dedicated Server'

2. Copy contents of 'Empyrion Python API\Scripts' to 'Empyrion - Dedicated Server\Content\Scripts'

3. Copy contents of 'Empyrion Python API\Server Plugin\Lib' to 'Empyrion - Dedicated Server\Content\Python'

4. Copy Listed DLLs from 'Empyrion Python API\Server Plugin\bin\Release' to 'Empyrion - Dedicated Server\EmpyrionDedicated_Data\Managed'
 * IronPython.dll
 * IronPython.Modules.dll
 * IronPython.SQLite.dll
 * Microsoft.Dynamic.dll
 * Microsoft.Scripting.AspNet.dll
 * Microsoft.Scripting.Core.dll
 * Microsoft.Scripting.dll
 * Microsoft.Scripting.Metadata.dll
 
5. Copy 'Empyrion Python API Server Plugin.dll' from 'Empyrion Python API\Server Plugin\bin\Release' to 'Empyrion - Dedicated Server\Content\Mods\EPA'

6. Customize 'Empyrion - Dedicated Server\epaconfig.yaml'

7. Customize 'Empyrion - Dedicated Server\Content\Scripts\loader.py'

8 Restart Dedicated Server
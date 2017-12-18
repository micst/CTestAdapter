# Build, Test, Debug

## Requirements

* CMake 3.9.0 or newer (for C# support)
* Visual Studio 2012/2013/2015/2017 (depending on the version for which you want to build the extension)
  * Visual Studio SDK must be installed. If Visual Studio SDK is not installed an error message will be
    shown which states that some `.targets` file was not found.

## Building

In general CTestAdapter can be build just like any other CMake base project: fire up the CMake
gui, pick your generator and run configure & generate.

For convenience there exist a set or prepared generation scripts which make life a little easier
but force specific build directories.

### Build using the prepared scripts

* Depending on the Visual Studio version you want to target run one of the batch scripts:
  * [SetupVs2012.bat](SetupVs2012.bat)
  * [SetupVs2013.bat](SetupVs2013.bat)
  * [SetupVs2015.bat](SetupVs2015.bat)
  * [SetupVs2017.bat](SetupVs2017.bat)
* There will be binary directories generated where you can find the **CTestAdapter.sln** solution
  * vs11 (for Visual Studio 2012)
  * vs12 (for Visual Studio 2013)
  * vs14 (for Visual Studio 2015)
  * vs15 (for Visual Studio 2017)
* Open the Visual Studio solution:
  * **vs\<vsversion\>/CTestAdapter.sln** (where `<vsversion>` is the version of your Visual Studio)
* Build the solution
* after building, the generated `.vsix` extension will be placed in the binary directory of the
  solution and in the source directory
  * **CTestAdapter\<vsversion\>-\<version\>[.\<revision\>][-dirty]-Debug.vsix** (for debug mode)
  * **CTestAdapter\<vsversion\>-\<version\>[.\<revision\>][-dirty].vsix** (for release mode)
  
### Debug the software

In the generated solution, the **startup project** is already set to **CTestAdapter**, along with all necessary paramters to debug the extension straight away. Just start *CTestAdapter* in the debugger and verify the tests from the sample project are shown and can be executed.

### Troubleshooting debugging

In some situations it can happen, that the built extension cannot be deployed to the experimental hive of Visual Studio. If this happens, the complete experimental configuration can be deleted to ensure a clean environment when starting the debugger.

* Run [ClearExperimentalHive.bat](ClearExperimentalHive.bat) to clear all experimental configurations of all Visual Studio versions

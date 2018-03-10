# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## Unreleased

### Fixed

 - installation error on Visual Studio 2017 update 6 and later

## 3.2.0 - 2018-01-26

### Fixed

 - wrong version in CHANGELOG
 - set active configuration default in test executor if not found 

### Added

 - delete .vsix packages when clearing build directories
 - search for ctest program in CMake cache dir if ctest in CMakeCache.txt is not found

### Changed

 - changed log messages in test discoverer and executor
 
## 3.1.0 - 2018-01-10

### Added

 - CHANGELOG
 - version information in log messages
 - log warning if adapter is not a tagged release build
 - add reference to github project to log

### Fixed

 - cancelling of tests
 - locating add_test() command depending on active Configuration
 - Visual Studio crash when regenerating with CMake
 - correct visualization of file links to CTest log files

## 3.0.0 - 2017-12-18

Initial version of CTestAdapter.

The project originated from [CTestTestAdapter](https://github.com/toeb/CTestTestAdapter).
This version is considered as version 1. Improvements to the original project were made 
in a [separate fork](https://github.com/micst/CTestTestAdapter). This fork is considered
as version 2.

### Added

 - better logging mechanism
   - enabled logging to file
   - added different log levels
 - added option page for configuration
 - introduced semantic versioning

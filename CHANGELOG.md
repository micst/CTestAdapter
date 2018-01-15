# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## Unreleased

### Fixed

 - wrong version in CHANGELOG

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

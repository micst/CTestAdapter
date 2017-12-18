if(REPOSITORY_FOUND)
  return()
endif()

set(PROJECT_NAME CTestAdapter)

set(${PROJECT_NAME}_VERSION_SOURCE "error")
set(${PROJECT_NAME}_VERSION_DIRTY "error")

set(GIT_PATHTYPE "error")
set(GIT_BRANCHNAME "error")
set(GIT_TAGNAME "error")
set(GIT_PATH_VERSION "error")
set(GIT_PATH_VERSION_MAJOR "error")
set(GIT_PATH_VERSION_MINOR "error")
set(GIT_PATH_VERSION_PATCH "error")

if(EXISTS "${CMAKE_SOURCE_DIR}/.git/HEAD")
  set(REPOSITORY_FOUND TRUE)
  find_program(GIT_EXECUTABLE NAMES git git.cmd)
  mark_as_advanced(GIT_EXECUTABLE)
  if(GIT_EXECUTABLE)
    # get current git revision
    execute_process(
      COMMAND ${GIT_EXECUTABLE} rev-parse --verify -q --short=4 HEAD
      OUTPUT_VARIABLE head
      OUTPUT_STRIP_TRAILING_WHITESPACE
      WORKING_DIRECTORY ${CMAKE_SOURCE_DIR})
    if(head)
      set(${PROJECT_NAME}_VERSION_SOURCE "${head}")
      # check if working copy is dirty
      execute_process(
        COMMAND ${GIT_EXECUTABLE} update-index -q --refresh
        WORKING_DIRECTORY ${CMAKE_SOURCE_DIR})
      execute_process(
        COMMAND ${GIT_EXECUTABLE} diff-index --name-only HEAD --
        OUTPUT_VARIABLE dirty
        OUTPUT_STRIP_TRAILING_WHITESPACE
        WORKING_DIRECTORY ${CMAKE_SOURCE_DIR})
      if(dirty)
        set(${PROJECT_NAME}_VERSION_DIRTY TRUE)
        set(${PROJECT_NAME}_VERSION_SOURCE "${${PROJECT_NAME}_VERSION_SOURCE}-dirty")
      else()
        set(${PROJECT_NAME}_VERSION_DIRTY FALSE)
      endif()
      execute_process(
        COMMAND ${GIT_EXECUTABLE} branch --contains HEAD
        OUTPUT_VARIABLE GIT_BRANCHNAME
        OUTPUT_STRIP_TRAILING_WHITESPACE
        WORKING_DIRECTORY ${CMAKE_SOURCE_DIR})
      if(GIT_BRANCHNAME)
        string(REPLACE "\n" ";" GIT_BRANCHNAME "${GIT_BRANCHNAME}")
        foreach(branch ${GIT_BRANCHNAME})
          string(REGEX REPLACE "^\\* (.+)$" "\\1" currentBranch ${branch})
          if(NOT "${currentBranch}" STREQUAL "${branch}")
            set(GIT_BRANCHNAME "${currentBranch}")
            break()
          endif()
        endforeach()
        set(GIT_PATHTYPE "FEATURE")
        if("${GIT_BRANCHNAME}" STREQUAL "master")
          set(GIT_PATHTYPE "MASTER")
        elseif("${GIT_BRANCHNAME}" STREQUAL "release")
          set(GIT_PATHTYPE "RELEASE")
        endif()
      else()
        set(GIT_BRANCHNAME "detached")
        set(GIT_PATHTYPE "detached")
      endif()
      # check if working copy is a tag
      execute_process(
        COMMAND ${GIT_EXECUTABLE} describe --tags --exact-match HEAD --
        OUTPUT_VARIABLE GIT_TAGNAME
        ERROR_VARIABLE error
        RESULT_VARIABLE res
        OUTPUT_STRIP_TRAILING_WHITESPACE
        WORKING_DIRECTORY ${CMAKE_SOURCE_DIR})
      if(NOT ${res} STREQUAL 0)
        set(GIT_TAGNAME "error")
      endif()
      # get date&time of last commit
      execute_process(
        COMMAND ${GIT_EXECUTABLE} show -s --format=%cI HEAD
        OUTPUT_VARIABLE GIT_DATE
        ERROR_VARIABLE error
        OUTPUT_STRIP_TRAILING_WHITESPACE
        WORKING_DIRECTORY ${CMAKE_SOURCE_DIR})
      if(GIT_DATE)
        # compute ${PROJECT_NAME}_VERSION_REVIS as ((month-1)*31)+day
        # Revision is limited to range [0-65534]!
        string(REGEX REPLACE
          "^[0-9][0-9]([0-9]+)-([0-9]+)-([0-9]+)T([0-9]+):([0-9]+):[0-9]+\\+[0-9]+:[0-9]+$"
          "\\1\\2\\3" GIT_DATE ${GIT_DATE})
        math(EXPR day "((${CMAKE_MATCH_2}-1) * 31) + ${CMAKE_MATCH_3}")
        set(${PROJECT_NAME}_VERSION_REVIS "${CMAKE_MATCH_1}${day}")
      endif()
    endif()
  endif()
else()
  return()
endif()

# check if we are building release
set(release_default OFF)
if("${GIT_PATHTYPE}" STREQUAL "RELEASE" AND
   NOT "${GIT_TAGNAME}" STREQUAL "error")
  set(release_default ON)
endif()

# verify release build
option(RELEASE_BUILD ""  ${release_default})
set(RELEASE_ERROR FALSE)
if(RELEASE_BUILD)
  message( 
    "================================================\n"
    "release build detected, we will make sure\n"
    " 1. no local changes exist\n"
    " 2. we are on a release branch/tag\n"
    " 3. the major, minor and patch versions match the tagname\n"
    "================================================")
  set(RELEASE_VALID TRUE)
  if("${GIT_PATHTYPE}" STREQUAL "RELEASE" AND NOT "${GIT_TAGNAME}" STREQUAL "error")
    string(REGEX REPLACE "^v([0-9]+)\\.([0-9]+)\\.([0-9]+)$" "\\1.\\2.\\3" 
      GIT_PATH_VERSION ${GIT_TAGNAME})
    set(GIT_PATH_VERSION ${GIT_PATH_VERSION})
    set(GIT_PATH_VERSION_MAJOR "${CMAKE_MATCH_1}")
    set(GIT_PATH_VERSION_MINOR "${CMAKE_MATCH_2}")
    set(GIT_PATH_VERSION_PATCH "${CMAKE_MATCH_3}")
  endif()
  if(NOT GIT_EXECUTABLE)
    set(RELEASE_VALID FALSE)
    set(RELEASE_ERROR TRUE)
    message(" - no git.exe found, cannot determine source version")
  endif()
  if("${GIT_TAGNAME}" STREQUAL "error")
    set(RELEASE_VALID FALSE)
    set(RELEASE_ERROR TRUE)
    message(" - no git tag found")
  endif()
  if(${PROJECT_NAME}_VERSION_DIRTY)
    set(RELEASE_VALID FALSE)
    set(RELEASE_ERROR TRUE)
    message(" - sources have local changes, cannot do release build")
  endif()
  if(NOT ${GIT_PATHTYPE} STREQUAL "RELEASE")
    set(RELEASE_VALID FALSE)
    set(RELEASE_ERROR TRUE)
    message(" - branchtype is not release, cannot build release version")
  endif()
  if(NOT "${GIT_PATH_VERSION_MAJOR}" STREQUAL "${${PROJECT_NAME}_VERSION_MAJOR}")
    set(RELEASE_VALID FALSE)
    set(RELEASE_ERROR TRUE)
    message(" - branch major version mismatch: ${GIT_PATH_VERSION_MAJOR} vs. ${${PROJECT_NAME}_VERSION_MAJOR}")
  endif()
  if(NOT "${GIT_PATH_VERSION_MINOR}" STREQUAL "${${PROJECT_NAME}_VERSION_MINOR}")
    set(RELEASE_VALID FALSE)
    set(RELEASE_ERROR TRUE)
    message(" - branch minor version mismatch: ${GIT_PATH_VERSION_MINOR} vs. ${${PROJECT_NAME}_VERSION_MINOR}")
  endif()
  if(NOT "${GIT_PATH_VERSION_PATCH}" STREQUAL "${${PROJECT_NAME}_VERSION_PATCH}")
    set(RELEASE_VALID FALSE)
    set(RELEASE_ERROR TRUE)
    message(" - branch patch version mismatch: ${GIT_PATH_VERSION_PATCH} vs. ${${PROJECT_NAME}_VERSION_PATCH}")
  endif()
  message("... done")
endif()

if("${${PROJECT_NAME}_VERSION_SOURCE}" STREQUAL "error")
  set(${PROJECT_NAME}_VERSION_SOURCE "9999")
  set(${PROJECT_NAME}_VERSION_REVIS "9999")
endif()
if(RELEASE_VALID)
  set(${PROJECT_NAME}_VERSION_SOURCE "release")
  set(${PROJECT_NAME}_VERSION_REVIS "0")
endif()

if(GIT_EXECUTABLE)
  message(STATUS "|-----------------------------|")
  message(STATUS "|  extracted git information  |")
  message(STATUS "|-----------------------------|")
  message(STATUS "|  source     ${${PROJECT_NAME}_VERSION_SOURCE}")
  message(STATUS "|  revision   ${${PROJECT_NAME}_VERSION_REVIS}")
  message(STATUS "|  dirty      ${${PROJECT_NAME}_VERSION_DIRTY}")
  message(STATUS "|  branchtype ${GIT_PATHTYPE}")
  message(STATUS "|  branchname ${GIT_BRANCHNAME}")
  message(STATUS "|  tagname    ${GIT_TAGNAME}")
  if(RELEASE_BUILD)
    message(STATUS "|  release    ${GIT_PATH_VERSION}")
    message(STATUS "|  rel maj    ${GIT_PATH_VERSION_MAJOR}")
    message(STATUS "|  rel min    ${GIT_PATH_VERSION_MINOR}")
    message(STATUS "|  rel pat    ${GIT_PATH_VERSION_PATCH}")
  endif()
  message(STATUS "|-----------------------------|")
endif()

if(RELEASE_ERROR AND RELEASE_BUILD)
  message(FATAL_ERROR "error in configuration (see descriptions above)")
endif()
unset(RELEASE_ERROR)

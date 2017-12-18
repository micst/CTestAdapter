set(ADAPTER_NAME "CTestAdapter${VS_VERSION}")

file(GLOB_RECURSE ADAPTER_SOURCES "*.cs")
file(GLOB PNG_SOURCES "Resources/*.png")
file(GLOB ICO_SOURCES "Resources/*.ico")
file(GLOB RESX_SOURCES "*.resx")
file(GLOB VSCT_SOURCES "*.vsct")
set(ORIGINATOR_KEYFILE "${CMAKE_CURRENT_SOURCE_DIR}/Key.snk")

set(ADAPTER_CONTENT_FILES
  ${PNG_SOURCES}
  LICENSE.txt)

set(VSPACKAGE_REFERENCES
  EnvDTE
  EnvDTE80
  EnvDTE90
  EnvDTE100
  Microsoft.CSharp
  Microsoft.VisualStudio.CommandBars
  Microsoft.VisualStudio.CoreUtility
  Microsoft.VisualStudio.OLE.Interop
  Microsoft.VisualStudio.Shell.Interop
  Microsoft.VisualStudio.Shell.Interop.8.0
  Microsoft.VisualStudio.Shell.Interop.9.0
  Microsoft.VisualStudio.Shell.Interop.10.0
  Microsoft.VisualStudio.Shell.Interop.11.0
  Microsoft.VisualStudio.TextManager.Interop
  Microsoft.VisualStudio.TextManager.Interop.8.0
  stdole
  System
  System.Data
  System.Design
  System.Drawing
  System.Windows.Forms
  System.Xml)

if(${VS_VERSION} EQUAL 11)
  list(APPEND VSPACKAGE_REFERENCES
    Microsoft.VisualStudio.Shell.11.0)
elseif(${VS_VERSION} EQUAL 12)
  list(APPEND VSPACKAGE_REFERENCES
    Microsoft.VisualStudio.Shell.12.0
    Microsoft.VisualStudio.Threading
    Microsoft.VisualStudio.Validation
    Microsoft.VisualStudio.Shell.Interop.12.0)
elseif(${VS_VERSION} EQUAL 14)
  list(APPEND VSPACKAGE_REFERENCES
    Microsoft.VisualStudio.Shell.14.0
    Microsoft.VisualStudio.Threading
    Microsoft.VisualStudio.Validation
    Microsoft.VisualStudio.Shell.Interop.12.0
    Microsoft.VisualStudio.Imaging
    Microsoft.VisualStudio.Utilities)
elseif(${VS_VERSION} EQUAL 15)
  list(APPEND VSPACKAGE_REFERENCES
    Microsoft.VisualStudio.Shell.15.0
    Microsoft.VisualStudio.Threading
    Microsoft.VisualStudio.Validation
    Microsoft.VisualStudio.Shell.Interop.12.0
    Microsoft.VisualStudio.Imaging
    Microsoft.VisualStudio.Utilities
    Microsoft.VisualStudio.Shell.Framework
    Microsoft.VisualStudio.Shell.Interop.15.3.DesignTime)
else()
endif()

set(CTestAdapter_REFERENCES
  ${VSPACKAGE_REFERENCES}
  Microsoft.VisualStudio.Shell.Immutable.10.0
  System.ComponentModel.Composition
  System.Core
  System.Management)

set(CTestAdapter_PROJECT_TYPES
  "{82b43b9b-a64c-4715-b499-d71e9ca2bd60}"
  "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")
  
# set testwindow references
set(ADAPTER_TESTWINDOW_REFERENCES
  Microsoft.VisualStudio.TestPlatform.ObjectModel
  Microsoft.VisualStudio.TestWindow.Core
  Microsoft.VisualStudio.TestWindow.Interfaces)

set(VC_TEST_WINDOW_SUBPATH "CommonExtensions/Microsoft/TestWindow")
get_filename_component(devPath "${CMAKE_VS_DEVENV_COMMAND}" DIRECTORY)
set(VC_TEST_WINDOW_PATH "${devPath}/${VC_TEST_WINDOW_SUBPATH}")
if(NOT EXISTS "${VC_TEST_WINDOW_PATH}")
  message(FATAL_ERROR "test window path not found:${VC_TEST_WINDOW_PATH}")
endif()
unset(CTestAdapter_HINT_REFERENCES)
foreach(v ${ADAPTER_TESTWINDOW_REFERENCES})
  list(APPEND CTestAdapter_HINT_REFERENCES VS_DOTNET_REFERENCE_${v})
  list(APPEND CTestAdapter_HINT_REFERENCES "${VC_TEST_WINDOW_PATH}/${v}.dll")
endforeach()

# vsix manifest
set(vsixManifestInputFile
  "${CMAKE_CURRENT_SOURCE_DIR}/source.in.extension.vsixmanifest")
set(vsixManifestOutputFile
  "${CMAKE_CURRENT_BINARY_DIR}/source.extension.vsixmanifest")
configure_file("${vsixManifestInputFile}"
  "${vsixManifestOutputFile}" @ONLY)

# assembly info
set(assemblyInfoInputFile "AssemblyInfo.cs.in")
set(assemblyInfoOutputFile 
  "${CMAKE_CURRENT_BINARY_DIR}/Properties/AssemblyInfo.cs")
configure_file("${assemblyInfoInputFile}"
  "${assemblyInfoOutputFile}" @ONLY)

# vsix content file properties
set_source_files_properties(${ADAPTER_CONTENT_FILES}
  PROPERTIES
  VS_COPY_TO_OUT_DIR "PreserveNewest"
  VS_INCLUDE_IN_VSIX "true"
  VS_TOOL_OVERRIDE "Content")

set_source_files_properties(${VSCT_SOURCES}
  PROPERTIES
  VS_TOOL_OVERRIDE "VSCTCompile"
  VS_CSHARP_ResourceName "Menus.ctmenu")

set_source_files_properties(${RESX_SOURCES}
  PROPERTIES
  VS_CSHARP_MergeWithCTO "true"
  VS_CSHARP_ManifestResourceName "VSPackage")

set_source_files_properties(CTestAdapterOptionPage.cs
  PROPERTIES
  VS_CSHARP_SubType "Code")

add_library(${ADAPTER_NAME} SHARED
  ${VSCT_SOURCES}
  ${RESX_SOURCES}
  ${PNG_SOURCES}
  ${ICO_SOURCES}
  ${VSPACKAGE_SOURCES}
  ${vsixManifestInputFile}
  ${vsixManifestOutputFile}
  ${assemblyInfoOutputFile}
  ${ADAPTER_CONTENT_FILES}
  ${ADAPTER_SOURCES}
  ${ORIGINATOR_KEYFILE})

target_compile_options(${ADAPTER_NAME} PRIVATE
  "/langversion:4"
  "/platform:anycpu")

set_target_properties(${ADAPTER_NAME}
  PROPERTIES
  LINKER_LANGUAGE "CSharp"
  VS_GLOBAL_PROJECT_TYPES "${CTestAdapter_PROJECT_TYPES}"
  VS_GLOBAL_ROOTNAMESPACE "CTestAdapter"
  VS_DOTNET_TARGET_FRAMEWORK_VERSION "${DOTNET_FRAMEWORK_VERSION}"
  VS_DOTNET_REFERENCES "${CTestAdapter_REFERENCES}"
  VS_GLOBAL_MinimumVisualStudioVersion "${MSBUILD_VERSION}"
  VS_GLOBAL_Install "true"
  VS_GLOBAL_InstallFrom "Disk"
  VS_GLOBAL_MapFileExtensions "true"
  VS_GLOBAL_ApplicationRevision ${CTestAdapter_VERSION_REVIS}
  VS_GLOBAL_ApplicationVersion "${CTestAdapter_VERSION}"
  VS_GLOBAL_IsWebBootstrapper "false"
  VS_GLOBAL_UseApplicationTrust "false"
  VS_GLOBAL_BootstrapperEnabled "true"
  VS_GLOBAL_SchemaVersion "2.0"
  VS_GLOBAL_GeneratePkgDefFile "True"
  VS_GLOBAL_CopyBuildOutputToOutputDirectory "True"
  VS_GLOBAL_CopyOutputSymbolsToOutputDirectory "True"
  VS_GLOBAL_DeployExtension "True"
  VS_GLOBAL_IncludeAssemblyInVSIXContainer "true"
  VS_GLOBAL_IncludeDebugSymbolsInLocalVSIXDeployment "true"
  VS_GLOBAL_IncludeDebugSymbolsInVSIXContainer "true"
  VS_GLOBAL_CreateVsixContainer "True"
  VS_GLOBAL_StartArguments "/rootsuffix Exp \"${CTestAdapter_SAMPLE_SOLUTION_FILE}\""
  VS_GLOBAL_StartAction "Program"
  VS_GLOBAL_StartProgram "${DEVENV_EXE}"
  VS_GLOBAL_SignAssembly "true"
  VS_GLOBAL_AssemblyOriginatorKeyFile "${ORIGINATOR_KEYFILE}"
  VS_GLOBAL_UseCodebase "true"
  VS_USER_PROPS "vsix.props"
  VS_DOTNET_REFERENCEPROP_ZERO_CHECK_TAG_Private "False"
  ${CTestAdapter_HINT_REFERENCES})

set(FORCE_TAG ForceIncludeInVSIX)
set(FORCE_VAL true)

foreach(ref ${VSPACKAGE_REFERENCES})  
  set_target_properties(${ADAPTER_NAME}
    PROPERTIES
    VS_DOTNET_REFERENCEPROP_${ref}_TAG_${FORCE_TAG} ${FORCE_VAL})
endforeach()

# find Microsoft.VsDSK.targets and "link" to it
set(VsSdkTargetsFileSubPath
  "Microsoft/VisualStudio/v${MSBUILD_VERSION}/VSSDK/Microsoft.VsSDK.targets")
set(VsSdkTargetsFile "$ENV{ProgramFiles\(x86\)}/MSBuild/${VsSdkTargetsFileSubPath}")
if(${VS_VERSION} STREQUAL "15")
  string(REPLACE 
    "/Common7/IDE/devenv.com" "/MSBuild/${VsSdkTargetsFileSubPath}" 
    VsSdkTargetsFile "${CMAKE_VS_DEVENV_COMMAND}")
endif()
if(NOT EXISTS "${VsSdkTargetsFile}")
  message(FATAL_ERROR ".targets file not found: ${VsSdkTargetsFile}\n"
    "Maybe Visual Studio SDK is missing. Please  check if you have "
    "the Visual Studio SDK installed.")
endif()
file(TO_CMAKE_PATH "${VsSdkTargetsFile}" VsSdkTargetsFile)
target_link_libraries(${ADAPTER_NAME} "${VsSdkTargetsFile}")

# add custom command to copy result somewhere
option(COPY_VSIX_TO_SOURCE "" ON)
if(COPY_VSIX_TO_SOURCE)
  set(TARGET_COPY_NAME "${ADAPTER_NAME}")
  string(APPEND TARGET_COPY_NAME "-${CTestAdapter_VERSION}")
  if(${CTestAdapter_VERSION_DIRTY})
    string(APPEND TARGET_COPY_NAME "-dirty")
  endif()
  string(APPEND TARGET_COPY_NAME "$<$<CONFIG:Debug>:-debug>")
  string(APPEND TARGET_COPY_NAME ".vsix")
  add_custom_command(TARGET ${ADAPTER_NAME} POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
      "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${ADAPTER_NAME}.vsix" 
      "${CMAKE_BINARY_DIR}/${TARGET_COPY_NAME}"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
      "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${ADAPTER_NAME}.vsix" 
      "${CMAKE_SOURCE_DIR}/${TARGET_COPY_NAME}")
endif()

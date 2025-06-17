cmake_minimum_required(VERSION 3.10)
project(cmmLaunchDll LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(SOURCE source/ExecController.cpp source/main.cpp)
set ( CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON )

if(CMAKE_BUILD_TYPE STREQUAL "Debug")
	add_compile_definitions(DEBUG)
	add_executable(${PROJECT_NAME} ${SOURCE})
else()
	add_compile_definitions(RELEASE)
	add_library(${PROJECT_NAME} SHARED ${SOURCE})
endif()
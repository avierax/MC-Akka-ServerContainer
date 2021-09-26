#!/bin/env bash
dotnet publish -c release /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -r linux-x64 --self-contained

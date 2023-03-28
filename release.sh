#!/bin/bash
set -e
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
if [ $# -eq 0 ]
then
  echo "No arguments supplied"
  exit 1
fi
VERSION=$1
cd $DIR
set -x
dotnet msbuild /t:Restore /p:Version=$VERSION
rm -rf build
mkdir build
for d in CoreRPC CoreRPC.AspNetCore src/CoreRPC.JsonLikeBinaryReaderWriter src/CoreRPC.JsonLikeBinarySerializer
do
	cd $DIR/$d
	rm -rf bin/Release
	dotnet msbuild /t:Pack /p:Configuration=Release /p:Version=$VERSION
	cp bin/Release/*.nupkg $DIR/build
done

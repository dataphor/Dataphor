$dir = Split-Path $MyInvocation.MyCommand.Path
Set-Location ..

# Most of the libraries in Dataphor are runtime dependencies, which won't be built automatically if we target the web api project directly.
# So we first build EVERYTHING, then publish specific projects, then build the docker container that has all of it.
msbuild ./Dataphor/Dataphor.sln /t:Restore /p:Configuration=Release /p:Platform="Any CPU"
msbuild ./Dataphor/Dataphor.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" /maxcpucount:4
msbuild ./Dataphor/Dataphor.sln /t:"Server\D4Runner:Publish" /p:Configuration=Release /p:Platform="Any CPU" /p:PublishProfile=FolderProfile.pubxml
msbuild ./Dataphor/Dataphor.sln /t:"Dataphoria\Dataphoria_Web_API_Core:Publish" /p:Configuration=Release /p:Platform="Any CPU" /p:PublishProfile=FolderProfile.pubxml

docker build --target final -t dataphoriawebapicore:latest -f .\Dataphoria\Dataphoria.Web.API.Core\Dockerfile .
Set-Location $dir
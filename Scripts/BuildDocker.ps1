$dir = Split-Path $MyInvocation.MyCommand.Path
Set-Location ..

docker build --target final -t dataphoriawebapicore:dev -f .\Dataphoria\Dataphoria.Web.API.Core\Dockerfile .


$login = (Get-ECRLoginCommand -Region us-east-1).Command
iex $login
docker tag dataphoriawebapicore:dev 912275679263.dkr.ecr.us-east-1.amazonaws.com/dataphoriawebapicore:dev
docker push 912275679263.dkr.ecr.us-east-1.amazonaws.com/dataphoriawebapicore:dev

Set-Location $dir


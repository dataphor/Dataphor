# This grabs a magic login command that has temporary push credentials to the AWs repositories.
$login = (Get-ECRLoginCommand -Region us-east-1).Command
iex $login

docker tag dataphoriawebapicore:latest 912275679263.dkr.ecr.us-east-1.amazonaws.com/dataphoriawebapicore:latest
docker push 912275679263.dkr.ecr.us-east-1.amazonaws.com/dataphoriawebapicore:latest


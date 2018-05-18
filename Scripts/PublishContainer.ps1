# This grabs a magic login command that has temporary push credentials to the AWs repositories.
$login = (Get-ECRLoginCommand -Region us-west-2).Command
iex $login

docker tag dataphoriawebapicore:latest 912275679263.dkr.ecr.us-west-2.amazonaws.com/dataphoriawebapicore:latest
docker push 912275679263.dkr.ecr.us-west-2.amazonaws.com/dataphoriawebapicore:latest


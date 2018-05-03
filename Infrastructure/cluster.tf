resource "aws_ecr_repository" "dataphor_repo" {
  name = "dataphoriawebapicore"
}

resource "aws_ecs_cluster" "temp_cluster" {
  name = "temp-cluster"
}

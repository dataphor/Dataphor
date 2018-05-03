resource "aws_ecs_service" "datpahor_service" {
  name        = "dataphor-service"
  launch_type = "FARGATE"

  network_configuration {
    subnets          = ["${data.aws_subnet_ids.subnets.ids}"]
    assign_public_ip = true
    security_groups  = ["${aws_security_group.allow_all.id}"]
  }

  # iam_role        = "${aws_iam_role.ecs_service_role.name}"
  cluster         = "${aws_ecs_cluster.temp_cluster.name}"
  task_definition = "${aws_ecs_task_definition.dataphor_definition.arn}"
  desired_count   = 1

  load_balancer {
    target_group_arn = "${aws_lb_target_group.target_group.arn}"
    container_name   = "dataphoriawebapicore"
    container_port   = 80
  }
}

resource "aws_ecs_task_definition" "dataphor_definition" {
  family                   = "dataphoriawebapicore"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = 1024
  memory                   = 2048
  execution_role_arn       = "arn:aws:iam::912275679263:role/ecsTaskExecutionRole"

  container_definitions = <<EOF
  [
    {
        "name": "dataphoriawebapicore",
        "image": "912275679263.dkr.ecr.us-east-1.amazonaws.com/dataphoriawebapicore:dev",
        "portMappings": [
            {
                "containerPort": 80
            }
        ],
        "essential": true
    }
]
EOF
}

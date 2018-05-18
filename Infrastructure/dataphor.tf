resource "aws_ecr_repository" "dataphor_repo" {
  name = "dataphoriawebapicore"
}

resource "aws_ecs_service" "datpahor_service" {
  name        = "dataphor-service"
  launch_type = "FARGATE"

  network_configuration {
    subnets          = ["${data.terraform_remote_state.shared_state.subnet_ids}"]
    assign_public_ip = true
    security_groups  = ["${aws_security_group.allow_all.id}"]
  }

  # iam_role        = "${aws_iam_role.ecs_service_role.name}"
  cluster         = "${data.terraform_remote_state.shared_state.cluster_name}"
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
        "image": "${aws_ecr_repository.dataphor_repo.repository_url}:latest",
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

resource "aws_security_group" "allow_all" {
  vpc_id = "${data.terraform_remote_state.shared_state.vpc_id}"

  ingress {
    protocol    = "-1"
    from_port   = 0
    to_port     = 0
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    protocol    = "icmp"
    from_port   = -1
    to_port     = -1
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    protocol    = "-1"
    from_port   = 0
    to_port     = 0
    cidr_blocks = ["0.0.0.0/0"]
  }
}

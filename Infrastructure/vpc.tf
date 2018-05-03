data "aws_vpc" "default" {
  id = "vpc-5e273c3b" # TODO: Paramterize
}

data "aws_subnet_ids" "subnets" {
  vpc_id = "${data.aws_vpc.default.id}"
}

resource "aws_security_group" "allow_all" {
  vpc_id = "${data.aws_vpc.default.id}"

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

resource "aws_lb" "lb" {
  depends_on      = ["aws_security_group.allow_all"]
  name            = "default-lb"
  security_groups = ["${aws_security_group.allow_all.id}"]
  subnets         = ["${data.aws_subnet_ids.subnets.ids}"]
  internal        = false
}

resource "aws_lb_target_group" "target_group" {
  name        = "target-group"
  port        = "80"
  protocol    = "HTTP"
  vpc_id      = "${data.aws_vpc.default.id}"
  target_type = "ip"

  stickiness {
    type = "lb_cookie"
  }

  health_check {
    matcher = "200-499"
  }
}

resource "aws_lb_listener" "lb_listener" {
  load_balancer_arn = "${aws_lb.lb.arn}"
  port              = "80"
  protocol          = "HTTP"

  default_action {
    target_group_arn = "${aws_lb_target_group.target_group.arn}"
    type             = "forward"
  }
}

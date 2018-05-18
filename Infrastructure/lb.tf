resource "aws_lb_target_group" "target_group" {
  name        = "dataphor-target-group-http"
  port        = "80"
  protocol    = "HTTP"
  vpc_id      = "${data.terraform_remote_state.shared_state.vpc_id}"
  target_type = "ip"

  stickiness {
    type = "lb_cookie"
  }

  health_check {
    matcher = "200-499"
  }
}

resource "aws_lb_listener_rule" "host_based_routing_rule" {
  listener_arn = "${data.terraform_remote_state.shared_state.lb_listener_http_arn}"
  priority     = "100"

  action {
    type             = "forward"
    target_group_arn = "${aws_lb_target_group.target_group.arn}"
  }

  condition {
    field  = "host-header"
    values = ["${aws_route53_record.dataphor_url.fqdn}"]
  }
}

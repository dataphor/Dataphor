data "aws_route53_zone" "cluster_zone" {
  zone_id = "${data.terraform_remote_state.shared_state.zone_id}"
}

resource "aws_route53_record" "dataphor_url" {
  zone_id = "${data.aws_route53_zone.cluster_zone.zone_id}"
  name    = "dataphor.${data.aws_route53_zone.cluster_zone.name}"
  type    = "A"

  alias {
    name                   = "${data.terraform_remote_state.shared_state.lb_dns_name}"
    zone_id                = "${data.terraform_remote_state.shared_state.lb_zone_id}"
    evaluate_target_health = false
  }
}

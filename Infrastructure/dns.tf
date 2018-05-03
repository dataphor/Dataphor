data "aws_route53_zone" "harmoniqhealth" {
  name = "harmoniqhealth.com"
}

resource "aws_route53_record" "dataphor_url" {
  zone_id = "${data.aws_route53_zone.harmoniqhealth.zone_id}"
  name    = "dataphor.harmoniqhealth.com"
  type    = "A"

  alias {
    name                   = "${aws_lb.lb.dns_name}"
    zone_id                = "${aws_lb.lb.zone_id}"
    evaluate_target_health = false
  }
}

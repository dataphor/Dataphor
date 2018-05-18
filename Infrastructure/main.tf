terraform {
  backend "s3" {
    bucket = "dbcg-infrastructure-state"
    key    = "dataphor.tfstate"
    region = "us-west-2"
  }
}

data "terraform_remote_state" "shared_state" {
  backend = "s3"

  config {
    bucket = "dbcg-infrastructure-state"
    key    = "shared.tfstate"
    region = "us-west-2"
  }
}

provider "aws" {
  region  = "${data.terraform_remote_state.shared_state.aws_region}"
  profile = "${var.aws_profile}"
  version = "~> 1.5"
}

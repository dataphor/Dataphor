terraform {
  backend "s3" {
    bucket = "dbcg-infrastructure-state"
    key    = "dataphor.tfstate"
    region = "us-west-2"
  }
}

provider "aws" {
  region  = "us-east-1"
  profile = "${var.aws_profile}"
  version = "~> 1.5"
}

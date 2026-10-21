
variable "cluster_name" {
  description = "Name of the Amazon EKS cluster"
  type        = string
  default     = "voting-app-eks"
}

variable "kubernetes_version" {
  description = "Kubernetes version for the EKS cluster"
  type        = string
  default     = "1.36"
}

variable "instance_type" {
  description = "EC2 instance type for EKS worker nodes"
  type        = string
  default     = "t3.small"
}


variable "allowed_api_cidr" {
  description = "Public IP address allowed to access the Kubernetes API"
  type        = string
  default     = "102.204.76.108/32"
}

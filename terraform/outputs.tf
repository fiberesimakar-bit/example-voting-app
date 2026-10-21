
output "cluster_name" {
  description = "Name of the EKS cluster"
  value       = aws_eks_cluster.voting_app.name
}

output "cluster_endpoint" {
  description = "API endpoint of the EKS cluster"
  value       = aws_eks_cluster.voting_app.endpoint
}

output "vpc_id" {
  description = "ID of the existing default VPC"
  value       = data.aws_vpc.default.id
}

output "subnet_ids" {
  description = "Subnet IDs used by the EKS cluster"
  value       = aws_eks_cluster.voting_app.vpc_config[0].subnet_ids
}

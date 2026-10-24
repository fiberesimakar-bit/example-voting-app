# Example Voting App

A simple distributed application running across multiple Docker containers.

## Getting started

Download [Docker Desktop](https://www.docker.com/products/docker-desktop) for Mac or Windows. [Docker Compose](https://docs.docker.com/compose) will be automatically installed. On Linux, make sure you have the latest version of [Compose](https://docs.docker.com/compose/install/).

This solution uses Python, Node.js, .NET, with Redis for messaging and Postgres for storage.

Run in this directory to build and run the app:

```shell
docker compose up
```

The `vote` app will be running at [http://localhost:8080](http://localhost:8080), and the `results` will be at [http://localhost:8081](http://localhost:8081).

Alternately, if you want to run it on a [Docker Swarm](https://docs.docker.com/engine/swarm/), first make sure you have a swarm. If you don't, run:

```shell
docker swarm init
```

Once you have your swarm, in this directory run:

```shell
docker stack deploy --compose-file docker-stack.yml vote
```

## Run the app in Kubernetes

The folder k8s-specifications contains the YAML specifications of the Voting App's services.

Run the following command to create the deployments and services. Note it will create these resources in your current namespace (`default` if you haven't changed it.)

```shell
kubectl create -f k8s-specifications/
```

The `vote` web app is then available on port 31000 on each host of the cluster, the `result` web app is available on port 31001.

To remove them, run:

```shell
kubectl delete -f k8s-specifications/
```

## Architecture

![Architecture diagram](architecture.excalidraw.png)

* A front-end web app in [Python](/vote) which lets you vote between two options
* A [Redis](https://hub.docker.com/_/redis/) which collects new votes
* A [.NET](/worker/) worker which consumes votes and stores them in…
* A [Postgres](https://hub.docker.com/_/postgres/) database backed by a Docker volume
* A [Node.js](/result) web app which shows the results of the voting in real time

## Notes

The voting application only accepts one vote per client browser. It does not register additional votes if a vote has already been submitted from a client.

This isn't an example of a properly architected, perfectly designed distributed app... it's just a simple
example of the various types of pieces and languages you might see (queues, persistent data, etc), and how to
deal with them in Docker at a basic level.

## Deploy with Helm

The `helm/voting-app` directory contains a Helm chart for deploying the Example Voting App to a Kubernetes cluster.

### Prerequisites

- A running Kubernetes cluster.
- `kubectl` configured to access the cluster.
- Helm installed.
- The application container images published to GitHub Container Registry (GHCR).

### Configure the database password

The chart expects a Kubernetes Secret named `voting-app-db` in the target
namespace. The Secret must contain the key `POSTGRES_PASSWORD`.

For an existing deployment, keep the Secret configured with the database's
current password. Do not recreate or change it just to follow this example.

For a new deployment, create the Secret using a securely entered password.
Do not put real credentials in this README, commit them to Git, or share them
in screenshots or logs.

### Validate the Helm chart

From the repository root, run:

```shell
helm lint helm/voting-app
helm template test helm/voting-app
```

### Install the chart

After configuring `kubectl` for your intended cluster and creating the database Secret, install the chart:

```shell
helm install voting-app helm/voting-app
```

The vote application is configured to use NodePort `31000`, and the results application uses NodePort `31001`. Access them using the appropriate cluster node address and ports, subject to your cluster's networking configuration.

### Upgrade or remove the release

To upgrade the release after changing the chart:

```shell
helm upgrade voting-app helm/voting-app
```

To uninstall the release:

```shell
helm uninstall voting-app
```

Check persistent volume claims and retained data separately before removing or recreating database storage.

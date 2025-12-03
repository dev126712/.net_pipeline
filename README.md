
### Build
```bash
sudo docker build . -t test2:latest
```

### Run
```bash
sudo docker run -d -p 8080:8080 -e ASPNETCORE_ENVIRONMENT="Development" test2:latest
```

Docker CI/CD Pipeline for .NET API

This workflow performs Static Analysis (CodeQL), builds the Docker image,

scans the image for vulnerabilities (Trivy), and pushes the final, secure

image to Docker Hub, tagged with 'latest' and the Git SHA.

name: Docker CI/CD - .NET API
```
on:
push:
branches: [ "main" ]
paths-ignore:
- 'docs/**'
- 'README.md'

concurrency:
group: docker-dotnet-api
cancel-in-progress: true

permissions:
contents: read
security-events: write

env:

The simple local name used for building and local artifact management

IMAGE_NAME: net_image
IMAGE_LATEST_TAG: net_image:latest

jobs:
```
----------------------------------------------------

1. SAST SCAN (CodeQL)

----------------------------------------------------
```
sast_scan:
name: AST Scan

runs-on: ubuntu-latest
steps:
- name: Checkout Repository
uses: actions/checkout@v4
with:
fetch-depth: 2

- name: Initialize CodeQL
  uses: github/codeql-action/init@v3
  with:
    languages: csharp 

- name: Autobuild
  uses: github/codeql-action/autobuild@v3

- name: Perform CodeQL Analysis
  uses: github/codeql-action/analyze@v3
```

----------------------------------------------------

2. BUILD IMAGE & UPLOAD ARTIFACT

----------------------------------------------------
```
build:
name: Build Image
runs-on: ubuntu-latest

steps:
- name: checkout repo
  uses: actions/checkout@v4


- name: Set up QEMU 
  uses: docker/setup-qemu-action@v3  

- name: set up docker
  uses: docker/setup-buildx-action@v3
  
- name: Build the Docker images
  uses: docker/build-push-action@v6
  with:
    context: .
    push: false
    tags: ${{ env.IMAGE_LATEST_TAG }}
    load: true
    outputs: type=docker,dest=/tmp/${{ env.IMAGE_NAME }}.tar

- name: Upload Image Artifact
  uses: actions/upload-artifact@v4
  with:
    name: ${{ env.IMAGE_NAME }}
    path: /tmp/${{ env.IMAGE_NAME }}.tar

```
----------------------------------------------------

3. SECURITY SCAN IMAGE (Trivy)

----------------------------------------------------
```
scan:
name: Security Scan Image
runs-on: ubuntu-latest
needs: [build, sast_scan]

steps:
- name: Download Image artifact
  uses: actions/download-artifact@v4
  with:
    name: ${{ env.IMAGE_NAME }}
    path: /tmp

- name: Load image
  run: |
    docker load --input /tmp/${{ env.IMAGE_NAME }}.tar
    docker image ls -a

- name: Install Trivy
  run: |
    sudo apt-get update
    sudo apt-get install -y curl
    curl -sfL https://raw.githubusercontent.com/aquasecurity/trivy/main/contrib/install.sh | sudo sh -s -- -b /usr/local/bin v0.57.0

- name: Download Trivy vulnerability database
  run: trivy image --download-db-only
  
- name: Run Trivy vulnerability scan
  run: |
    trivy image \
      --exit-code 0 \
      --format table \
      --ignore-unfixed \
      --pkg-types os,library \
      --severity CRITICAL,HIGH,MEDIUM \
      ${{ env.IMAGE_NAME }}:latest

```
----------------------------------------------------

4. PUSH IMAGE (Final Authorization and Push)

----------------------------------------------------
```
push:
name: Push Image
runs-on: ubuntu-latest
needs: [build, scan]

steps:

- name: Download Image artifact
  uses: actions/download-artifact@v4
  with:
    name: ${{ env.IMAGE_NAME }}
    path: /tmp

- name: Load image
  run: |
    docker load --input /tmp/${{ env.IMAGE_NAME }}.tar
    docker image ls -a

- name: Tag Image with Docker Hub Username Prefix
  run: |
    DOCKER_REPO="${{ secrets.DOCKERHUB_USERNAME }}/${{ env.IMAGE_NAME }}"
    
    docker tag ${{ env.IMAGE_NAME }}:latest $DOCKER_REPO:latest
    
    docker tag ${{ env.IMAGE_NAME }}:latest $DOCKER_REPO:${{ github.sha }}

- name: Login to Docker Hub
  uses: docker/login-action@v3
  with:
    username: ${{ secrets.DOCKERHUB_USERNAME }}
    password: ${{ secrets.DOCKERHUB_REPO_TOKEN }}

- name: Push Image to Docker Hub
  run: |
    DOCKER_REPO="${{ secrets.DOCKERHUB_USERNAME }}/${{ env.IMAGE_NAME }}"
    
    docker push $DOCKER_REPO:latest
    docker push $DOCKER_REPO:${{ github.sha }}

- name: Docker - Logout
  run: docker logout
```

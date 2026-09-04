# Stop and remove any existing containers.
docker stop tjpork; docker rm tjpork

# Build a new image from the Dockerfile.
docker build -t tjpork-app -f src/TJPork.Web/Dockerfile .

# Run the new container.
docker run -d -p 8080:80 --name tjpork tjpork-app

# check if the container is running.
docker ps -a
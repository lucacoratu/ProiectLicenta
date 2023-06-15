FROM golang:1.19

# Set destination for COPY
WORKDIR /app

# Download Go modules
#COPY go.mod go.sum ./
#RUN go mod download

# Copy the source code. Note the slash at the end, as explained in
COPY src ./
RUN go mod download

# Build
RUN CGO_ENABLED=0 GOOS=linux go build -o /friendservice

# Optional:
# To bind to a TCP port, runtime parameters must be supplied to the docker command.
# But we can document in the Dockerfile what ports
# the application is going to listen on by default.
EXPOSE 8084

# Run
CMD ["/friendservice"]

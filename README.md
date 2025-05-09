# Sendlix SMTP Relay

## Overview

The Sendlix SMTP Relay is a lightweight application that bridges your applications and the Sendlix email service. It accepts SMTP connections, authenticates clients, and forwards emails to the Sendlix API.

## Installation

### Prerequisites

- .NET 9 or later

### Steps

1. **Download**  
   Obtain the latest version of the Sendlix SMTP Relay.

2. **Extract**  
   Unpack the downloaded file into a directory of your choice.

3. **Configure**  
   Set up the relay using environment variables or a configuration file (refer to the [Configuration](#configuration) section).

## Configuration

The Sendlix SMTP Relay can be configured using environment variables. All settings are optional. Below are the available options:

- `ListenAddress`: The IP address the relay binds to (default: `127.0.0.1`).
- `Port`: The port the relay listens on (default: `587`).
- `ServerCertificatePath`: Path to an SSL certificate in PKCS12 format (optional).
- `Auth:Username`: Username for Sendlix API authentication.
- `Auth:ApiKey`: API key for Sendlix API authentication.
- `Auth:ApiKeyPath`: Path to a file containing the API key (alternative to `Auth:ApiKey`).

The `Auth:*` settings allow you to define default credentials for the relay. This enables clients to send emails without authenticating, as the relay will handle authentication with Sendlix on their behalf.

### Example Configuration

```bash
ListenAddress=0.0.0.0
Port=587
```

## Usage

Run the application to start listening for SMTP connections on the configured address and port. Use any SMTP client to connect to the relay and send emails.

## Docker Support

You can also run the Sendlix SMTP Relay in a Docker container:

```bash
docker run -p 587:587 ghcr.io/sendlix/smtp-relay/sendlix-smtp-relay:latest
```
